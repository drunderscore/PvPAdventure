using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Utilities;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.SpawnSelector;

/// <summary>
/// Draws world icons: world spawn, own bed, teammates' beds.
/// Allows hovering and clicking to select spawn points and teleport when allowed.
/// </summary>
[Autoload(Side = ModSide.Client)]
public class TeleportOnMap : ModSystem
{
    public override void Load()
    {
        On_SpawnMapLayer.Draw += OnSpawnMapLayer;
    }

    public override void Unload()
    {
        On_SpawnMapLayer.Draw -= OnSpawnMapLayer;
    }

    private void OnSpawnMapLayer(On_SpawnMapLayer.orig_Draw orig, SpawnMapLayer self, ref MapOverlayDrawContext context, ref string text)
    {
        Player local = Main.LocalPlayer;
        if (local == null)
            return;

        SpawnPlayer sp = local.GetModPlayer<SpawnPlayer>();

        bool selectorOpen = SpawnSystem.IsUiOpen;
        bool instantTeleport = SpawnSystem.CanTeleport && !local.dead;

        bool recallActive = selectorOpen && !instantTeleport;

        bool handledHover = false;

        handledHover |= DrawWorldIcon(local, sp, selectorOpen, instantTeleport, recallActive, ref context, ref text);
        handledHover |= DrawMyBedIcon(local, sp, selectorOpen, instantTeleport, recallActive, ref context, ref text);
        handledHover |= DrawTeamBedIcons(local, sp, selectorOpen, instantTeleport, recallActive, ref context, ref text);
        handledHover |= DrawMyPortalIcon(local, sp, selectorOpen, instantTeleport, recallActive, ref context, ref text);
        handledHover |= DrawTeamPortalIcons(local, sp, selectorOpen, instantTeleport, recallActive, ref context, ref text);
    }

    private bool DrawWorldIcon(Player local, SpawnPlayer sp, bool selectorOpen, bool instantTeleport, bool recallActive, ref MapOverlayDrawContext context, ref string text)
    {
        Vector2 pos = new(Main.spawnTileX, Main.spawnTileY);
        bool selected = sp.SelectedType == SpawnType.World;

        if (!DrawIcon(TextureAssets.SpawnPoint.Value, pos, selected, instantTeleport, recallActive, ref context, out bool hover))
            return false;

        if (!hover)
            return false;

        if (!selectorOpen)
        {
            text = Language.GetTextValue("UI.SpawnPoint");
            return false;
        }

        BlockMapInput(local);

        if (instantTeleport)
        {
            text = Language.GetTextValue("Mods.PvPAdventure.Spawn.TeleportToWorldSpawn");
            if (TryConsumeClick(local))
            {
                sp.ToggleSelection(SpawnType.World);
                sp.RequestExecute();
            }

            return true;
        }

        text = selected
            ? Language.GetTextValue("Mods.PvPAdventure.Spawn.CancelWorldSpawn")
            : Language.GetTextValue("Mods.PvPAdventure.Spawn.SelectWorldSpawn");

        if (TryConsumeClick(local))
            sp.ToggleSelection(SpawnType.World);

        return true;
    }

    private bool DrawMyBedIcon(Player local, SpawnPlayer sp, bool selectorOpen, bool instantTeleport, bool recallActive, ref MapOverlayDrawContext context, ref string text)
    {
        if (!HasValidBedSpawn(local))
            return false;

        Vector2 pos = new Vector2(local.SpawnX, local.SpawnY);
        bool selected = sp.SelectedType == SpawnType.MyBed;


        if (!DrawIcon(TextureAssets.SpawnBed.Value, pos, selected, instantTeleport, recallActive, ref context, out bool hover))
            return false;

        if (!hover)
            return false;

        if (!selectorOpen)
        {
            text = Language.GetTextValue("UI.SpawnBed");
            return false;
        }

        BlockMapInput(local);

        if (instantTeleport)
        {
            text = Language.GetTextValue("Mods.PvPAdventure.Spawn.TeleportToMyBed", local.name);
            if (TryConsumeClick(local))
            {
                sp.ToggleSelection(SpawnType.MyBed, local.whoAmI);
                sp.RequestExecute();
            }

            return true;
        }

        text = selected
            ? Language.GetTextValue("Mods.PvPAdventure.Spawn.CancelMyBed", local.name)
            : Language.GetTextValue("Mods.PvPAdventure.Spawn.SelectMyBed", local.name);

        if (TryConsumeClick(local))
            sp.ToggleSelection(SpawnType.MyBed, local.whoAmI);

        return true;
    }

    private bool DrawTeamBedIcons(Player local, SpawnPlayer sp, bool selectorOpen, bool instantTeleport, bool recallActive, ref MapOverlayDrawContext context, ref string text)
    {
        if (local.team == 0)
            return false;

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            if (i == local.whoAmI)
                continue;

            Player other = Main.player[i];
            if (other == null || !other.active || other.team != local.team)
                continue;

            if (!HasValidBedSpawn(other))
                continue;

            Vector2 pos = new Vector2(other.SpawnX, other.SpawnY);
            bool selected = sp.SelectedType == SpawnType.TeammateBed && sp.SelectedPlayerIndex == i;

            if (!DrawIcon(TextureAssets.SpawnBed.Value, pos, selected, instantTeleport, recallActive, ref context, out bool hover))
                continue;

            if (!hover)
                continue;

            if (!selectorOpen)
            {
                text = Language.GetTextValue("Mods.PvPAdventure.Spawn.TeammatesBed", other.name);
                return true;
            }

            BlockMapInput(local);

            if (instantTeleport)
            {
                text = Language.GetTextValue("Mods.PvPAdventure.Spawn.TeleportToTeammatesBed", other.name);
                if (TryConsumeClick(local))
                {
                    sp.ToggleSelection(SpawnType.TeammateBed, i);
                    sp.RequestExecute();
                }

                return true;
            }

            text = selected
                ? Language.GetTextValue("Mods.PvPAdventure.Spawn.CancelTeammatesBed", other.name)
                : Language.GetTextValue("Mods.PvPAdventure.Spawn.SelectTeammatesBed", other.name);

            if (TryConsumeClick(local))
                sp.ToggleSelection(SpawnType.TeammateBed, i);

            return true;
        }
        return false;
    }

    private bool DrawMyPortalIcon(Player local, SpawnPlayer sp, bool selectorOpen, bool instantTeleport, bool recallActive, ref MapOverlayDrawContext context, ref string text)
    {
        if (!PortalSystem.TryGetPortalWorldPos(local, out Vector2 portalWorldPos))
            return false;

        Vector2 pos = portalWorldPos / 16f;
        bool selected = sp.SelectedType == SpawnType.MyPortal;
        bool canUsePortal = SpawnSystem.CanUseStoredPortal(local);
        if (!DrawIcon(PortalDrawer.GetPortalMinimapAsset(local).Value, pos, selected, instantTeleport && canUsePortal, recallActive && canUsePortal, ref context, out bool hover))
            return false;

        if (!hover)
            return false;

        if (!selectorOpen)
        {
            text = "My portal";
            return true;
        }

        BlockMapInput(local);

        if (!canUsePortal)
        {
            text = "Can only teleport to your portal from spawn or while dead";
            return true;
        }

        if (instantTeleport)
        {
            text = "Teleport to my portal";
            if (TryConsumeClick(local))
            {
                sp.ToggleSelection(SpawnType.MyPortal);
                sp.RequestExecute();
            }

            return true;
        }

        text = selected ? "Cancel my portal" : "Select my portal";

        if (TryConsumeClick(local))
            sp.ToggleSelection(SpawnType.MyPortal);

        return true;
    }

    private bool DrawTeamPortalIcons(Player local, SpawnPlayer sp, bool selectorOpen, bool instantTeleport, bool recallActive, ref MapOverlayDrawContext context, ref string text)
    {
        if (local.team == 0)
            return false;

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            if (i == local.whoAmI)
                continue;

            Player other = Main.player[i];
            if (other == null || !other.active || other.team != local.team)
                continue;

            if (!SpawnPlayer.TryGetPortalWorldPos(other, out Vector2 portalWorld))
                continue;

            Vector2 pos = portalWorld / 16f;
            bool selected = sp.SelectedType == SpawnType.TeammatePortal && sp.SelectedPlayerIndex == i;

            if (!DrawIcon(PortalDrawer.GetPortalMinimapAsset(other).Value, pos, selected, instantTeleport, recallActive, ref context, out bool hover))
                continue;

            if (!hover)
                continue;

            SpectateSystem.TrySetHover(SpawnType.TeammatePortal, i);

            if (!selectorOpen)
            {
                text = Language.GetTextValue("Mods.PvPAdventure.Spawn.TeammatesPortal", other.name);
                return true;
            }

            BlockMapInput(local);

            if (instantTeleport)
            {
                text = Language.GetTextValue("Mods.PvPAdventure.Spawn.TeleportToTeammatesPortal", other.name);
                if (TryConsumeClick(local))
                {
                    sp.ToggleSelection(SpawnType.TeammatePortal, i);
                    sp.RequestExecute();
                }

                return true;
            }

            text = selected
                ? Language.GetTextValue("Mods.PvPAdventure.Spawn.CancelTeammatesPortal", other.name)
                : Language.GetTextValue("Mods.PvPAdventure.Spawn.SelectTeammatesPortal", other.name);

            if (TryConsumeClick(local))
                sp.ToggleSelection(SpawnType.TeammatePortal, i);

            return true;
        }

        return false;
    }

    private static bool DrawIcon(Texture2D tex, Vector2 tilePos, bool selected, bool instantTeleport, bool recallActive,
    ref MapOverlayDrawContext context, out bool hover)
    {
        Color iconColor = Color.White;
        if (!SpawnSystem.IsLocalPlayerInSpawnRegion)
        {
            iconColor = Color.White * 0.5f;
        }

        bool canHoverZoom = instantTeleport || recallActive;

        // Stick: if selected, always 1.8
        float baseScale = selected ? 1.8f : 1.0f;

        // Hover/highlight zoom only when allowed; otherwise keep same as base
        float hoverScale = canHoverZoom ? 1.8f : baseScale;

        var result = context.Draw(
            texture: tex,
            position: tilePos,
            color: iconColor,
            frame: new SpriteFrame(1, 1),
            scaleIfNotSelected: baseScale,
            scaleIfSelected: hoverScale,
            alignment: Alignment.Bottom,
            spriteEffects: SpriteEffects.None
        );

        hover = result.IsMouseOver;
        return true;
    }

    //private static void DrawPortalMapGlow(Vector2 tilePos, Color color, ref MapOverlayDrawContext context)
    //{
    //    Texture2D pixel = TextureAssets.MagicPixel.Value;
    //    float pulse = 0.55f + (float)System.Math.Sin(Main.GameUpdateCount * 0.08f) * 0.15f;
    //    Color glow = color * pulse;

    //    DrawMapDust(pixel, tilePos + new Vector2(0f, -2.0f), glow, 5f, ref context);
    //    DrawMapDust(pixel, tilePos + new Vector2(-0.55f, -1.55f), glow * 0.8f, 4f, ref context);
    //    DrawMapDust(pixel, tilePos + new Vector2(0.55f, -1.55f), glow * 0.8f, 4f, ref context);
    //}

    private static void DrawMapDust(Texture2D pixel, Vector2 tilePos, Color color, float scale, ref MapOverlayDrawContext context)
    {
        context.Draw(pixel, tilePos, color, new SpriteFrame(1, 1), scale, scale, Alignment.Bottom, SpriteEffects.None);
    }

    private static bool TryConsumeClick(Player local)
    {
        if (!Main.mouseLeft || !Main.mouseLeftRelease)
            return false;

        BlockMapInput(local);
        local.releaseUseItem = false;
        local.controlUseItem = false;
        Main.mouseLeftRelease = false;
        return true;
    }

    private static void BlockMapInput(Player local)
    {
        local.mouseInterface = true;
        local.controlUseItem = false;
    }

    private static bool HasValidBedSpawn(Player player)
    {
        if (player.SpawnX < 0 || player.SpawnY < 0)
            return false;

        return Player.CheckSpawn(player.SpawnX, player.SpawnY);
    }
}
