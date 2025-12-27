using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.IO;
using Terraria;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Core.SpawnAndSpectate;

/// <summary>
/// Draws player bed spawn points on the fullscreen map.
/// Also enables teleportation to teammates' beds by clicking on them.
/// </summary>
internal class BedsOnMap : ModSystem
{
    private Asset<Texture2D> spawnBedTexture;

    public override void Load()
    {
        On_SpawnMapLayer.Draw += OnSpawnMapLayer;
    }

    public override void Unload()
    {
        On_SpawnMapLayer.Draw -= OnSpawnMapLayer;
    }

    public override void PostSetupContent()
    {
        if (!Main.dedServ)
        {
            spawnBedTexture = TextureAssets.SpawnBed;
        }
    }

    private static bool HasValidBedSpawn(Player player)
    {
        if (player.SpawnX < 0 || player.SpawnY < 0)
            return false;

        return Player.CheckSpawn(player.SpawnX, player.SpawnY);
    }

    private void OnSpawnMapLayer(On_SpawnMapLayer.orig_Draw orig, SpawnMapLayer self, ref MapOverlayDrawContext context, ref string text)
    {
        // We skip orig because we’re replacing how beds are drawn
        // orig(self, ref context, ref text);

        Player localPlayer = Main.LocalPlayer;
        var sys = ModContent.GetInstance<SpawnAndSpectateSystem>();
        bool selectorEnabled = sys.ui.CurrentState == sys.spawnSelectorState;

        // Draw world spawn point + handle teleport
        var worldSpawnHover = context.Draw(
                texture: TextureAssets.SpawnPoint.Value,
                position: new Vector2(Main.spawnTileX, Main.spawnTileY),
                color: Color.White,
                frame: new SpriteFrame(1, 1),
                scaleIfNotSelected: 1.0f,
                scaleIfSelected: 1.8f,
                alignment: Alignment.Bottom,
                spriteEffects: SpriteEffects.None
            );

        if (worldSpawnHover.IsMouseOver)
        {
            Main.LocalPlayer.mouseInterface = true;

            text = selectorEnabled
                ? Language.GetTextValue("Mods.PvPAdventure.SpawnAndSpectate.TeleportToWorldSpawn")
                : Language.GetTextValue("UI.SpawnPoint");

            if (selectorEnabled && Main.mouseLeft && Main.mouseLeftRelease)
            {
                if (Main.netMode == NetmodeID.SinglePlayer)
                {
                    Vector2 spawnWorld = new Vector2(Main.spawnTileX, Main.spawnTileY - 3).ToWorldCoordinates();
                    Main.LocalPlayer.Teleport(spawnWorld, TeleportationStyleID.RecallPotion);
                    Main.mapFullscreen = false;
                    return;
                }

                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    ModPacket p = Mod.GetPacket();
                    p.Write((byte)AdventurePacketIdentifier.WorldSpawnTeleport);
                    p.Write((byte)Main.myPlayer);
                    p.Send();

                    Main.mapFullscreen = false;
                    return;
                }
            }
        }

        // Draw local player's bed
        if (HasValidBedSpawn(localPlayer))
        {
            Vector2 localBedPos = new(localPlayer.SpawnX, localPlayer.SpawnY);

            if (context.Draw(TextureAssets.SpawnBed.Value, localBedPos, Alignment.Bottom).IsMouseOver)
            {
                text = Language.GetTextValue("UI.SpawnBed");
            }
        }

        // Draw team beds + handle teleport
        spawnBedTexture ??= TextureAssets.SpawnBed;

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player player = Main.player[i];
            if (player == null || !player.active || player.team != localPlayer.team)
                continue;

            if (!HasValidBedSpawn(player))
                continue;

            Vector2 bedTilePos = new(player.SpawnX, player.SpawnY);

            var hoverCheck = context.Draw(
                texture: spawnBedTexture.Value,
                position: bedTilePos,
                color: Color.White,
                frame: new SpriteFrame(1, 1),
                scaleIfNotSelected: 1.0f,
                scaleIfSelected: selectorEnabled ? 1.8f : 1.0f,
                alignment: Alignment.Bottom,
                spriteEffects: SpriteEffects.None
            );

            if (!hoverCheck.IsMouseOver)
                continue;

            // Hovering over this player's bed
            text = Language.GetTextValue("Mods.PvPAdventure.SpawnAndSpectate.PlayersBed", player.name);

            if (!selectorEnabled)
                continue;

            // Selector is enabled, allow teleporting
            text = Language.GetTextValue("Mods.PvPAdventure.SpawnAndSpectate.TeleportToPlayersBed", player.name);

            if (Main.mouseLeft && Main.mouseLeftRelease)
            {
                if (Main.netMode == NetmodeID.SinglePlayer)
                {
                    Vector2 spawn = new(player.SpawnX * 16, player.SpawnY * 16 - 48);
                    Main.LocalPlayer.Teleport(spawn);
                    Main.mapFullscreen = false;
                    return;
                }

                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    ModPacket p = Mod.GetPacket();
                    p.Write((byte)AdventurePacketIdentifier.BedTeleport);
                    p.Write((byte)Main.myPlayer);
                    p.Write((short)player.SpawnX);
                    p.Write((short)player.SpawnY);
                    p.Send();

                    Main.mapFullscreen = false;
                }
            }
        }
    }

    public static void HandleBedTeleportPacket(BinaryReader reader, int whoAmI)
    {
        byte playerId = reader.ReadByte();
        short bedX = reader.ReadInt16();
        short bedY = reader.ReadInt16();

        if (Main.netMode != NetmodeID.Server)
            return;

        if (playerId != whoAmI)
            return;

        Player player = Main.player[playerId];
        if (player is null || !player.active)
            return;

        // Reject invalid spawns
        if (bedX == -1 || bedY == -1 || !Player.CheckSpawn(bedX, bedY))
        {
            return;
        }

        Vector2 spawnWorld = new Vector2(bedX, bedY - 3).ToWorldCoordinates();

        player.Teleport(spawnWorld, TeleportationStyleID.RecallPotion);

        NetMessage.SendData(
            MessageID.TeleportEntity,
            -1, -1, null,
            number: 0,
            number2: player.whoAmI,
            number3: spawnWorld.X,
            number4: spawnWorld.Y,
            number5: TeleportationStyleID.RecallPotion
        );

#if DEBUG
        ChatHelper.BroadcastChatMessage(
            NetworkText.FromLiteral($"[DEBUG/SERVER] Player {player.name} teleported to bed ({bedX}, {bedY})."),
            Color.White
        );
#endif
    }
    public static void HandleWorldSpawnPacket(BinaryReader reader, int whoAmI)
    {
        byte playerId = reader.ReadByte();

        if (Main.netMode != NetmodeID.Server)
            return;

        if (playerId != whoAmI)
            return;

        Player player = Main.player[playerId];
        if (player is null || !player.active)
            return;

        Vector2 spawnWorld = new Vector2(Main.spawnTileX, Main.spawnTileY - 3).ToWorldCoordinates();

        player.Teleport(spawnWorld, TeleportationStyleID.RecallPotion);

        NetMessage.SendData(
            MessageID.TeleportEntity,
            -1, -1, null,
            number: 0,
            number2: player.whoAmI,
            number3: spawnWorld.X,
            number4: spawnWorld.Y,
            number5: TeleportationStyleID.RecallPotion
        );

#if DEBUG
        ChatHelper.BroadcastChatMessage(
            NetworkText.FromLiteral($"[DEBUG/SERVER] Player {player.name} teleported to world spawn ({spawnWorld.X}, {spawnWorld.Y})."),
            Color.White
        );
#endif
    }
}
