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

        int me = localPlayer.whoAmI;
        bool isSelected = sp.SelectedType == SpawnType.MyBed;

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

        // Debug
        //Main.NewText(sp.SelectedType);

        if (!hover.IsMouseOver)
            return false;

        if (!selectorEnabled)
        {
            text = Language.GetTextValue("UI.SpawnBed");
            return false;
        }

        if (!canTeleport)
        {
            text = isSelected
                ? Language.GetTextValue("Mods.PvPAdventure.Spawn.CancelMyBed", localPlayer.name)
                : Language.GetTextValue("Mods.PvPAdventure.Spawn.SelectMyBed", localPlayer.name);

            if (IsClick())
            {
                sp.ToggleSelection(SpawnType.MyBed, me);
                return true;
            }

            return false;
        }

        text = Language.GetTextValue("Mods.PvPAdventure.Spawn.TeleportToMyBed", localPlayer.name);

        if (IsClick())
        {
            sp.ToggleSelection(SpawnType.MyBed, me);
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

            bool isSelected = sp.SelectedType == SpawnType.TeammateBed && sp.SelectedPlayerIndex == i;

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

            if (!selectorEnabled)
            {
                text = Language.GetTextValue("Mods.PvPAdventure.Spawn.PlayersBed", player.name);
                return false;
            }

            if (!canTeleport)
            {
                text = isSelected
                    ? Language.GetTextValue("Mods.PvPAdventure.Spawn.CancelTeammatesBed", player.name)
                    : Language.GetTextValue("Mods.PvPAdventure.Spawn.SelectTeammatesBed", player.name);

                if (IsClick())
                {
                    sp.ToggleSelection(SpawnType.TeammateBed, i);
                    return true;
                }

                return false;
            }

            text = Language.GetTextValue("Mods.PvPAdventure.Spawn.TeleportToTeammatesBed", player.name);

            if (IsClick())
            {
                sp.ToggleSelection(SpawnType.TeammateBed, i);
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

}
