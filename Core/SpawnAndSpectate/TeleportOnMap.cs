using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.UI;
using static PvPAdventure.Core.SpawnAndSpectate.SpawnSystem;

namespace PvPAdventure.Core.SpawnAndSpectate;

/// <summary>
/// Draws player bed spawn points on the fullscreen map and allows teleportation to them.
/// Also draws world spawn point on the fullscreen map and allows teleportation to it.
/// </summary>
[Autoload(Side =ModSide.Both)]
public class TeleportOnMap : ModSystem
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
        // Don't call original to prevent default spawn point drawing
        // Keep for reference:
        // orig(self, ref context, ref text); 

        Player localPlayer = Main.LocalPlayer;

        var sys = ModContent.GetInstance<SpawnSystem>();
        bool selectorEnabled = sys.ui?.CurrentState == sys.spawnState;

        var sp = localPlayer.GetModPlayer<SpawnPlayer>();
        bool isHoveringWorldSpawn = sp.SelectedType == SpawnType.World;

        // Draw world spawn point
        var worldSpawnHover = context.Draw(
            texture: TextureAssets.SpawnPoint.Value,
            position: new Vector2(Main.spawnTileX, Main.spawnTileY),
            color: Color.White,
            frame: new SpriteFrame(1, 1),
            scaleIfNotSelected: isHoveringWorldSpawn ? 1.8f : 1.0f,
            scaleIfSelected: 1.8f,
            alignment: Alignment.Bottom,
            spriteEffects: SpriteEffects.None
        );

        bool iconHoverWorldSpawn = worldSpawnHover.IsMouseOver;
        bool hoverWorldSpawn = iconHoverWorldSpawn || isHoveringWorldSpawn;

        if (hoverWorldSpawn)
        {
            localPlayer.mouseInterface = true;

            text = selectorEnabled
                ? Language.GetTextValue("Mods.PvPAdventure.Spawn.TeleportToWorldSpawn")
                : Language.GetTextValue("UI.SpawnPoint");
        }

        // Only allow teleporting if selector is enabled
        if (selectorEnabled && iconHoverWorldSpawn && Main.mouseLeft && Main.mouseLeftRelease)
        {
            sp.ToggleSelection(SpawnType.World);
            return;
        }

        // Draw local player's bed
        if (HasValidBedSpawn(localPlayer))
        {
            Vector2 localBedPos = new(localPlayer.SpawnX, localPlayer.SpawnY);

            var hoverLocalBed = context.Draw(
                texture: TextureAssets.SpawnBed.Value,
                position: localBedPos,
                color: Color.White,
                frame: new SpriteFrame(1, 1),
                scaleIfNotSelected: 1.0f,
                scaleIfSelected: selectorEnabled ? 1.8f : 1.0f,
                alignment: Alignment.Bottom,
                spriteEffects: SpriteEffects.None
            );

            if (hoverLocalBed.IsMouseOver)
            {
                localPlayer.mouseInterface = true;

                text = selectorEnabled
                    ? Language.GetTextValue("Mods.PvPAdventure.Spawn.TeleportToPlayersBed", localPlayer.name)
                    : Language.GetTextValue("UI.SpawnBed");

                if (selectorEnabled && Main.mouseLeft && Main.mouseLeftRelease)
                {
                    sp.ToggleSelection(SpawnType.Bed, localPlayer.whoAmI);
                    return;
                }
            }
        }

        // Draw team beds + handle teleport
        spawnBedTexture ??= TextureAssets.SpawnBed;

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            // Skip drawing self bed twice
            if (i == localPlayer.whoAmI)
                continue;

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

            text = Language.GetTextValue("Mods.PvPAdventure.Spawn.PlayersBed", player.name);

            if (!selectorEnabled)
                continue;

            text = Language.GetTextValue("Mods.PvPAdventure.Spawn.TeleportToPlayersBed", player.name);

            if (Main.mouseLeft && Main.mouseLeftRelease)
            {
                sp.ToggleSelection(SpawnType.Bed, i);
                return;
            }

        }
    }

    public static void HandlePacket(BinaryReader reader, int whoAmI)
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        byte requesterId = reader.ReadByte();
        SpawnType type = (SpawnType)reader.ReadByte();

        if (requesterId != whoAmI)
            return;

        Player requester = Main.player[requesterId];
        if (requester == null || !requester.active)
            return;

        Vector2 teleportPos;

        switch (type)
        {
            case SpawnType.World:
                teleportPos = new Vector2(Main.spawnTileX, Main.spawnTileY - 3).ToWorldCoordinates();
                break;

            case SpawnType.Player:
                {
                    short idx = reader.ReadInt16();
                    if (!SpawnSystem.IsValidTeammateIndex(requester, idx))
                        return;

                    Player target = Main.player[idx];
                    teleportPos = target.position;
                    break;
                }

            case SpawnType.Bed:
                {
                    short idx = reader.ReadInt16();
                    if (idx < 0 || idx >= Main.maxPlayers)
                        return;

                    Player bedOwner = Main.player[idx];
                    if (bedOwner == null || !bedOwner.active)
                        return;

                    if (idx != requester.whoAmI)
                    {
                        if (requester.team == 0 || bedOwner.team != requester.team)
                            return;
                    }

                    if (bedOwner.SpawnX < 0 || bedOwner.SpawnY < 0 || !Player.CheckSpawn(bedOwner.SpawnX, bedOwner.SpawnY))
                        return;

                    teleportPos = new Vector2(bedOwner.SpawnX, bedOwner.SpawnY - 6).ToWorldCoordinates();
                    break;
                }

            default:
                return;
        }

        requester.Teleport(teleportPos, TeleportationStyleID.RecallPotion);

        NetMessage.SendData(
            MessageID.TeleportEntity,
            -1, -1, null,
            number: 0,
            number2: requester.whoAmI,
            number3: teleportPos.X,
            number4: teleportPos.Y,
            number5: TeleportationStyleID.RecallPotion
        );
    }

}
