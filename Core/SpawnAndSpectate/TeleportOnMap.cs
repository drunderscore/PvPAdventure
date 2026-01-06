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

    #region Load Hooks
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
    #endregion

    private void OnSpawnMapLayer(On_SpawnMapLayer.orig_Draw orig,SpawnMapLayer self,ref MapOverlayDrawContext context, ref string text)
    {
        // Don't call original to prevent default spawn point drawing.
        // orig(self, ref context, ref text);

        Player localPlayer = Main.LocalPlayer;
        if (localPlayer == null)
            return;

        // Get system and flags
        var sys = ModContent.GetInstance<SpawnSystem>();
        bool selectorEnabled = sys.ui?.CurrentState == sys.spawnState;
        bool canTeleport = SpawnSystem.CanTeleport;

        SpawnPlayer sp = localPlayer.GetModPlayer<SpawnPlayer>();

        if (DrawWorldSpawn(localPlayer, sp, selectorEnabled, canTeleport, ref context, ref text))
            return;

        if (DrawLocalBed(localPlayer, sp, selectorEnabled, canTeleport, ref context, ref text))
            return;

        if (DrawTeamBeds(localPlayer, sp, selectorEnabled, canTeleport, ref context, ref text))
            return;
    }

    private bool DrawWorldSpawn(Player localPlayer,SpawnPlayer sp,bool selectorEnabled,bool canTeleport,ref MapOverlayDrawContext context, ref string text)
    {
        bool isSelected = sp.SelectedType == SpawnType.World;

        var hover = context.Draw(
            texture: TextureAssets.SpawnPoint.Value,
            position: new Vector2(Main.spawnTileX, Main.spawnTileY),
            color: Color.White,
            frame: new SpriteFrame(1, 1),
            scaleIfNotSelected: isSelected ? 1.8f : 1.0f,
            //scaleIfNotSelected: 1.0f,
            scaleIfSelected: 1.8f,
            alignment: Alignment.Bottom,
            spriteEffects: SpriteEffects.None
        );

        if (!hover.IsMouseOver)
            return false;

        if (!selectorEnabled)
        {
            text = Language.GetTextValue("UI.SpawnPoint");
            return false;
        }

        if (!canTeleport)
        {
            text = isSelected
                ? Language.GetTextValue("Mods.PvPAdventure.Spawn.CancelWorldSpawn")
                : Language.GetTextValue("Mods.PvPAdventure.Spawn.SelectWorldSpawn");

            if (IsClick())
            {
                sp.ToggleSelection(SpawnType.World);
                return true;
            }

            return false;
        }

        // Teleport mode
        text = Language.GetTextValue("Mods.PvPAdventure.Spawn.TeleportToWorldSpawn");

        if (IsClick())
        {
            sp.ToggleSelection(SpawnType.World);
            return true;
        }

        return false;
    }

    private bool DrawLocalBed(Player localPlayer,SpawnPlayer sp,bool selectorEnabled, bool canTeleport, ref MapOverlayDrawContext context, ref string text)
    {
        if (!HasValidBedSpawn(localPlayer))
            return false;

        int idx = localPlayer.whoAmI;
        bool isSelected = sp.SelectedType == SpawnType.Bed && sp.SelectedPlayerIndex == idx;

        Vector2 bedPos = new(localPlayer.SpawnX, localPlayer.SpawnY);

        var hover = context.Draw(
            texture: TextureAssets.SpawnBed.Value,
            position: bedPos,
            color: Color.White,
            frame: new SpriteFrame(1, 1),
            //scaleIfNotSelected: 1.0f,
            scaleIfNotSelected: isSelected ? 1.8f : 1.0f,
            scaleIfSelected: selectorEnabled ? 1.8f : 1.0f,
            alignment: Alignment.Bottom,
            spriteEffects: SpriteEffects.None
        );

        if (!hover.IsMouseOver)
            return false;

        localPlayer.mouseInterface = true;

        if (!selectorEnabled)
        {
            text = Language.GetTextValue("UI.SpawnBed");
            return false;
        }

        if (!canTeleport)
        {
            text = isSelected
                ? Language.GetTextValue("Mods.PvPAdventure.Spawn.CancelBedSpawn", localPlayer.name)
                : Language.GetTextValue("Mods.PvPAdventure.Spawn.SelectBedSpawn", localPlayer.name);

            if (IsClick())
            {
                sp.ToggleSelection(SpawnType.Bed, idx);
                return true;
            }

            return false;
        }

        text = Language.GetTextValue("Mods.PvPAdventure.Spawn.TeleportToPlayersBed", localPlayer.name);

        if (IsClick())
        {
            sp.ToggleSelection(SpawnType.Bed, idx);
            return true;
        }

        return false;
    }

    private bool DrawTeamBeds(Player localPlayer,SpawnPlayer sp,bool selectorEnabled, bool canTeleport, ref MapOverlayDrawContext context,ref string text)
    {
        if (localPlayer.team == 0)
            return false;

        spawnBedTexture ??= TextureAssets.SpawnBed;

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            if (i == localPlayer.whoAmI)
                continue;

            Player player = Main.player[i];
            if (player == null || !player.active || player.team != localPlayer.team)
                continue;

            if (!HasValidBedSpawn(player))
                continue;

            bool isSelected = sp.SelectedType == SpawnType.Bed && sp.SelectedPlayerIndex == i;

            Vector2 bedTilePos = new(player.SpawnX, player.SpawnY);

            var hover = context.Draw(
                texture: spawnBedTexture.Value,
                position: bedTilePos,
                color: Color.White,
                frame: new SpriteFrame(1, 1),
                //scaleIfNotSelected: 1.0f,
                scaleIfNotSelected: isSelected ? 1.8f : 1.0f,
                scaleIfSelected: selectorEnabled ? 1.8f : 1.0f,
                alignment: Alignment.Bottom,
                spriteEffects: SpriteEffects.None
            );

            if (!hover.IsMouseOver)
                continue;

            localPlayer.mouseInterface = true;

            if (!selectorEnabled)
            {
                text = Language.GetTextValue("Mods.PvPAdventure.Spawn.PlayersBed", player.name);
                return false;
            }

            if (!canTeleport)
            {
                text = isSelected
                    ? Language.GetTextValue("Mods.PvPAdventure.Spawn.CancelPlayersBed", player.name)
                    : Language.GetTextValue("Mods.PvPAdventure.Spawn.SelectPlayersBed", player.name);

                if (IsClick())
                {
                    sp.ToggleSelection(SpawnType.Bed, i);
                    return true;
                }

                return false;
            }

            text = Language.GetTextValue("Mods.PvPAdventure.Spawn.TeleportToPlayersBed", player.name);

            if (IsClick())
            {
                sp.ToggleSelection(SpawnType.Bed, i);
                return true;
            }

            return false;
        }

        return false;
    }

    private static bool IsClick() => Main.mouseLeft && Main.mouseLeftRelease;

    private static bool HasValidBedSpawn(Player player)
    {
        if (player.SpawnX < 0 || player.SpawnY < 0)
            return false;

        return Player.CheckSpawn(player.SpawnX, player.SpawnY);
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
