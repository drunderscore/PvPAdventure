using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Utilities;
using System;
using System.Collections.Generic;
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

    private bool DrawMyPortalIcon(Player local, SpawnPlayer sp, bool selectorOpen, bool instantTeleport, bool recallActive, ref MapOverlayDrawContext context, ref string text)
    {
        if (!PortalSystem.TryGetPortalWorldPos(local, out Vector2 portalWorldPos))
            return false;

        Vector2 pos = portalWorldPos / 16f;
        bool selected = sp.SelectedType == SpawnType.MyPortal;
        bool canUsePortal = SpawnSystem.CanUseStoredPortal(local);

        Texture2D portalIcon = PortalDrawer.GetPortalMinimapAsset(local).Value;

        if (!DrawIcon(portalIcon, pos, selected, instantTeleport, recallActive, ref context, out bool hover))
            return false;

        //DrawPortalMapVfx(portalIcon, pos, selected || hover || recallActive, ref context);
        //DrawPotionOfReturnMapGlow(portalWorldPos, selected || hover || recallActive, ref context);
        //PortalDrawer.SpawnPortalMapDust(portalWorldPos, selected || hover || recallActive);
        //SpawnPortalMapDust(portalWorldPos, selected || hover || recallActive);
        //DrawPotionOfReturnGlowOnly(portalWorldPos, selected || hover || recallActive, ref context);
        //DrawPotionOfReturnMapDust(portalWorldPos, selected || hover || recallActive, ref context);

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

    #region VFX and dust
    private static void DrawPotionOfReturnMapDust(Vector2 worldPos, bool emphasized, ref MapOverlayDrawContext context)
    {
        int attempts = emphasized ? 40 : 20; // Vanilla-ish dust, multiplied hard enough to actually read on the map.
        Vector2 mapCenter = WorldToMapScreen(worldPos, ref context);

        if (context.ClippingRectangle.HasValue && !context.ClippingRectangle.Value.Contains(mapCenter.ToPoint()))
            return;

        for (int i = 0; i < attempts; i++)
        {
            if (Main.rand.Next(3) != 0)
                continue;

            bool blueDust = Main.rand.Next(2) == 0;
            int dustType = blueDust ? Utils.SelectRandom(Main.rand, 86, 88) : 240;

            Vector2 vector = Vector2.UnitY.RotatedByRandom(MathHelper.TwoPi);
            vector *= new Vector2(0.5f, 1f);

            Vector2 dustWorldOffset;
            float scale = 0.5f + Main.rand.NextFloat();

            if (blueDust)
            {
                dustWorldOffset = -vector.SafeNormalize(Vector2.Zero) * Main.rand.Next(10, 21);
                Vector2 velocity = vector.RotatedBy(MathHelper.PiOver2) * 2f;
                dustWorldOffset += velocity * 10f;
            }
            else
            {
                dustWorldOffset = -vector.SafeNormalize(Vector2.Zero) * Main.rand.Next(5, 10);
                Vector2 velocity = vector.RotatedBy(-MathHelper.PiOver2) * 3f;
                dustWorldOffset += velocity * 10f;
            }

            DrawMapDust(dustType, mapCenter, dustWorldOffset, scale, emphasized, ref context);
        }
    }
    private static void DrawMapDust(int dustType, Vector2 mapCenter, Vector2 dustWorldOffset, float dustScale, bool emphasized, ref MapOverlayDrawContext context)
    {
        Texture2D texture = TextureAssets.Dust.Value;
        Rectangle source = new(dustType * 10, Main.rand.Next(3) * 10, 8, 8);

        const float mapDustOffsetScale = 0.35f;

        Vector2 screenOffset = dustWorldOffset * mapDustOffsetScale * context.DrawScale;
        float scale = dustScale * context.DrawScale * (emphasized ? 1.5f : 1.15f);

        Color color = Color.White * (emphasized ? 0.95f : 0.75f);

        Main.spriteBatch.Draw(
            texture,
            mapCenter + screenOffset,
            source,
            color,
            0f,
            source.Size() * 0.5f,
            scale,
            SpriteEffects.None,
            0f
        );
    }

    private static Vector2 WorldToMapScreen(Vector2 worldPos, ref MapOverlayDrawContext context)
    {
        Vector2 tilePos = worldPos / 16f;
        return (tilePos - context.MapPosition) * context.MapScale + context.MapOffset;
    }

    //private static void SpawnPortalMapDust(Vector2 worldPos, bool emphasized)
    //{
    //    int dustCount = emphasized ? 3 : 1;

    //    for (int i = 0; i < dustCount; i++)
    //    {
    //        PotionOfReturnGateHelper gate = new(
    //            PotionOfReturnGateHelper.GateType.EntryPoint,
    //            worldPos,
    //            1f
    //        );

    //        gate.SpawnReturnPortalDust();
    //    }
    //}

    //private static void DrawPotionOfReturnMapGlow(Vector2 worldPos, bool emphasized, ref MapOverlayDrawContext context)
    //{
    //    List<DrawData> drawData = [];

    //    PotionOfReturnGateHelper gate = new(
    //        PotionOfReturnGateHelper.GateType.EntryPoint,
    //        worldPos,
    //        emphasized ? 0.8f : 0.45f
    //    );

    //    gate.DrawToDrawData(drawData, emphasized ? 2 : 0);

    //    for (int i = 0; i < drawData.Count; i++)
    //        DrawGateDataOnMap(drawData[i], worldPos, ref context);
    //}

    //private static void DrawPotionOfReturnGlowOnly(Vector2 worldPos, bool emphasized, ref MapOverlayDrawContext context)
    //{
    //    Texture2D glowTexture = TextureAssets.Extra[242].Value;

    //    int frame = (int)(((float)Main.tileFrameCounter[491] + worldPos.X + worldPos.Y) % 40f) / 5;
    //    Rectangle source = glowTexture.Frame(1, 8, 0, frame);

    //    Vector2 tilePos = worldPos / 16f;
    //    Vector2 screenPos = (tilePos - context.MapPosition) * context.MapScale + context.MapOffset;

    //    if (context.ClippingRectangle.HasValue && !context.ClippingRectangle.Value.Contains(screenPos.ToPoint()))
    //        return;

    //    float pulse = 0.7f + 0.3f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5f);
    //    float opacity = emphasized ? 0.85f : 0.45f;
    //    float scale = context.DrawScale * (emphasized ? 0.75f : 0.55f);

    //    Color color = new Color(127, 50, 127, 0) * opacity * pulse;
    //    Vector2 origin = source.Size() * 0.5f;

    //    Main.spriteBatch.Draw(
    //        glowTexture,
    //        screenPos,
    //        source,
    //        color,
    //        0f,
    //        origin,
    //        scale,
    //        SpriteEffects.None,
    //        0f
    //    );
    //}

    //private static void DrawGateDataOnMap(DrawData data, Vector2 gateWorldPos, ref MapOverlayDrawContext context)
    //{
    //    Vector2 tilePos = gateWorldPos / 16f;
    //    Vector2 screenPos = (tilePos - context.MapPosition) * context.MapScale + context.MapOffset;

    //    if (context.ClippingRectangle.HasValue && !context.ClippingRectangle.Value.Contains(screenPos.ToPoint()))
    //        return;

    //    Vector2 worldDelta = data.position + Main.screenPosition - gateWorldPos;
    //    Vector2 mapDelta = worldDelta / 16f * context.MapScale;
    //    float mapScale = context.DrawScale * context.MapScale / 16f;

    //    Main.spriteBatch.Draw(
    //        data.texture,
    //        screenPos + mapDelta,
    //        data.sourceRect,
    //        data.color,
    //        data.rotation,
    //        data.origin,
    //        data.scale * mapScale,
    //        data.effect,
    //        0f
    //    );
    //}


    private static void DrawPortalMapVfx(Texture2D tex, Vector2 tilePos, bool emphasized, ref MapOverlayDrawContext context)
    {
        float time = Main.GlobalTimeWrappedHourly;

        float pulse = 0.5f + 0.5f * (float)Math.Sin(time * (emphasized ? 5.2f : 3.6f));
        float baseAlpha = emphasized ? 0.38f : 0.18f;
        float baseScale = emphasized ? 1.2f : 1f;

        // Soft center glow.
        DrawPortalMapLayer(tex, tilePos, Color.White * (baseAlpha * 0.45f), baseScale * (1.35f + pulse * 0.2f), ref context);

        // Cyan outer bloom.
        DrawPortalMapLayer(tex, tilePos, Color.Cyan * (baseAlpha * 0.9f), baseScale * (1.15f + pulse * 0.12f), ref context);

        // Secondary blue bloom for more depth.
        DrawPortalMapLayer(tex, tilePos, new Color(90, 170, 255) * (baseAlpha * 0.65f), baseScale * (1.05f + pulse * 0.08f), ref context);

        int orbitCount = emphasized ? 6 : 4;
        float orbitRadius = emphasized ? 0.95f : 0.6f;
        float orbitAlpha = emphasized ? 0.32f : 0.16f;
        float orbitScale = emphasized ? 0.92f : 0.82f;

        // Main rotating wisps.
        for (int i = 0; i < orbitCount; i++)
        {
            float angle = time * (emphasized ? 3.8f : 2.4f) + MathHelper.TwoPi * i / orbitCount;
            Vector2 offset = new((float)Math.Cos(angle) * orbitRadius, (float)Math.Sin(angle) * orbitRadius * 0.7f);

            DrawPortalMapLayer(
                tex,
                tilePos + offset,
                Color.Cyan * orbitAlpha,
                baseScale * orbitScale * (1f + pulse * 0.08f),
                ref context);
        }

        // Counter-rotating inner wisps.
        for (int i = 0; i < orbitCount; i++)
        {
            float angle = -time * (emphasized ? 2.9f : 1.8f) + MathHelper.TwoPi * (i + 0.5f) / orbitCount;
            Vector2 offset = new((float)Math.Cos(angle) * orbitRadius * 0.55f, (float)Math.Sin(angle) * orbitRadius * 0.4f);

            DrawPortalMapLayer(
                tex,
                tilePos + offset,
                new Color(160, 220, 255) * (orbitAlpha * 0.75f),
                baseScale * 0.78f,
                ref context);
        }
    }

    private static void DrawPortalMapLayer(Texture2D tex, Vector2 tilePos, Color color, float scale, ref MapOverlayDrawContext context)
    {
        context.Draw(
            texture: tex,
            position: tilePos,
            color: color,
            frame: new SpriteFrame(1, 1),
            scaleIfNotSelected: scale,
            scaleIfSelected: scale,
            alignment: Alignment.Bottom,
            spriteEffects: SpriteEffects.None
        );
    }
    #endregion
}
