using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.SpawnAndSpectate.UI;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Core.SpawnAndSpectate;

/// <summary>
/// The main system managing the spawn selection and teammate spectate UI for the client, 
/// allowing players to choose their respawn location or spectate other players after death.
/// </summary>
/// <remarks></remarks>
[Autoload(Side = ModSide.Client)]
public class SpawnAndSpectateSystem : ModSystem
{
    // UI elements and states
    public UserInterface ui;
    public SpawnAndSpectateState spawnSelectorState; // allows spawn selection and spectating

    // Track whether the spawn selector is enabled
    private static bool Enabled;
    public static void SetEnabled(bool newValue) => Enabled = newValue;
    public static bool GetEnabled() => Enabled;

    // Client-side gate set by RespawnPlayer.UpdateDead when the local timer freezes at 2.
    private static bool _canRespawn;
    public static bool CanRespawn => _canRespawn;
    public static void SetCanRespawn(bool value) => _canRespawn = value;

    // Hovered/spectated/selected player indices
    public static int? HoveredPlayerIndex = null; // Currently hovered player
    public static int? SpectatePlayerIndex = null; // Currently spectated player
    public static int? SelectedSpawnPlayerIndex = null; // Currently selected player to spawn on
    public static int? HoverSpectatePlayerIndex = null; // Temporary spectate while hovering a row

    public static bool HoveringWorldSpawn;

    public static bool ShouldShowUI
    {
        get
        {
            Player p = Main.LocalPlayer;
            if (p == null)
                return false;

            // Always visible when dead.
            if (p.dead)
                return true;

            // Visible when alive only inside spawn region.
            return Enabled;
        }
    }

    public static bool IsAliveSpawnRegionInstant
    {
        get
        {
            Player p = Main.LocalPlayer;
            return p != null && !p.dead && Enabled;
        }
    }

    public static bool IsValidTeammateIndex(int idx)
    {
        // Must be valid index
        if (idx < 0 || idx >= Main.maxPlayers)
            return false;

        // Get my player and target player
        Player local = Main.LocalPlayer;
        Player t = Main.player[idx];

        // Must be valid player
        if (local == null || t == null || !t.active)
            return false;

        // Must not be self
        if (t.whoAmI == local.whoAmI)
            return false;

        // Must be on same team
        if (local.team == 0 || t.team != local.team)
            return false;

        // Must be alive
        if (t.dead)
            return false;

        return true;
    }

    public static void ToggleSpectateOnPlayerIndex(int idx)
    {
        bool canSpectate = Main.LocalPlayer.dead || IsAliveSpawnRegionInstant;
        if (!canSpectate)
            return;

        if (!IsValidTeammateIndex(idx))
            return;

        SpectatePlayerIndex = (SpectatePlayerIndex == idx) ? null : idx;
    }

    public override void OnWorldLoad()
    {
        // Initialize the UI states
        ui = new();
        spawnSelectorState = new();

        SetCanRespawn(false);

        // Draw on map
        Main.OnPostFullscreenMapDraw += DrawOnFullscreenMap;
    }

    public override void Unload()
    {
        SetCanRespawn(false);
        Main.OnPostFullscreenMapDraw -= DrawOnFullscreenMap;
    }

    private bool _wasShowingUI;
    public override void UpdateUI(GameTime gameTime)
    {
        if (ui == null)
            return;

        bool show = ShouldShowUI;

        if (!show)
        {
            _wasShowingUI = false;

            HoveringWorldSpawn = false;
            if (ui.CurrentState != null)
                ui.SetState(null);

            return;
        }

        // entering UI this frame
        if (!_wasShowingUI || ui.CurrentState != spawnSelectorState)
        {
            spawnSelectorState.Rebuild();
            ui.SetState(spawnSelectorState);
            _wasShowingUI = true;
        }

        ui.Update(gameTime);
    }

    #region Spectating
    public static void ClearSpectate()
    {
        SpectatePlayerIndex = null;
    }

    public static void TrySetSpectate(int playerIndex)
    {
        bool canSpectate = Main.LocalPlayer.dead || IsAliveSpawnRegionInstant;
        if (!canSpectate)
            return;

        if (playerIndex < 0 || playerIndex >= Main.maxPlayers)
            return;

        Player p = Main.player[playerIndex];
        if (p == null || !p.active || p.dead)
            return;

        SpectatePlayerIndex = playerIndex;
    }

    public static void TrySetHoverSpectate(int idx)
    {
        bool canSpectate = Main.LocalPlayer.dead || IsAliveSpawnRegionInstant;
        if (!canSpectate)
        {
            HoverSpectatePlayerIndex = null;
            return;
        }

        if (!IsValidTeammateIndex(idx))
        {
            HoverSpectatePlayerIndex = null;
            return;
        }

        HoverSpectatePlayerIndex = idx;
    }

    public static void ClearHoverSpectateIfMatch(int idx)
    {
        if (HoverSpectatePlayerIndex == idx)
            HoverSpectatePlayerIndex = null;
    }

    /// <summary>
    /// Adjusts the screen position to follow the current spectated player or the world spawn point during spectate mode.
    /// </summary>
    public override void ModifyScreenPosition()
    {
        Player local = Main.LocalPlayer;

        bool canSpectate = local != null && local.active && (local.dead || IsAliveSpawnRegionInstant);
        if (!canSpectate)
        {
            HoverSpectatePlayerIndex = null;
            SpectatePlayerIndex = null;
            HoveringWorldSpawn = false;
            return;
        }

        // World spawn preview takes priority over player hover/lock.
        if (HoveringWorldSpawn)
        {
            Vector2 spawnWorld = new Vector2(Main.spawnTileX, Main.spawnTileY - 3).ToWorldCoordinates();
            Vector2 targetTopLeft = spawnWorld - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;

            // Snap:
            Main.screenPosition = targetTopLeft;

            // Smooth:
            //Main.screenPosition = Vector2.Lerp(Main.screenPosition, targetTopLeft, 0.18f);

            return;
        }

        int? targetIndex = HoverSpectatePlayerIndex;
        if (targetIndex == null)
            targetIndex = SpectatePlayerIndex;

        if (targetIndex is not int idx || idx < 0 || idx >= Main.maxPlayers)
        {
            HoverSpectatePlayerIndex = null;
            return;
        }

        Player target = Main.player[idx];
        if (target == null || !target.active || target.dead)
        {
            if (HoverSpectatePlayerIndex == idx)
                HoverSpectatePlayerIndex = null;

            if (SpectatePlayerIndex == idx)
                SpectatePlayerIndex = null;

            return;
        }

        Main.screenPosition = target.position - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
    }

    #endregion

    private void DrawOnFullscreenMap(Vector2 mapPos, float mapScale)
    {
        if (!Main.mapFullscreen || ui?.CurrentState == null)
            return;

        var sb = Main.spriteBatch;
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);
        ui.Draw(sb, Main._drawInterfaceGameTime);
        sb.End();
    }

    // Draw on screen
    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int mouseTextIndex = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
        if (mouseTextIndex == -1)
            return;

        layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
            "PvPAdventure: SpawnAndSpectate",
            delegate
            {
                if (Main.mapFullscreen || ui?.CurrentState == null)
                    return true;

                ui.Draw(Main.spriteBatch, Main._drawInterfaceGameTime);
                return true;
            },
            InterfaceScaleType.UI
        ));
    }
}
