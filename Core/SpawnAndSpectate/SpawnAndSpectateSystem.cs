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

    public static bool IsDeadSelectionPhase
    {
        get
        {
            Player p = Main.LocalPlayer;
            return p != null && p.dead && p.respawnTimer > 2;
        }
    }

    public static bool IsDeadReadyPhase
    {
        get
        {
            Player p = Main.LocalPlayer;
            return p != null && p.dead && p.respawnTimer == 2;
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

    public static void ToggleSpawnSelectionOnPlayerIndex(int idx)
    {
        // Must be able to respawn on teammate
        if (!IsValidTeammateIndex(idx))
            return;

        // Toggle spawn selection
        SelectedSpawnPlayerIndex = (SelectedSpawnPlayerIndex == idx) ? null : idx;
    }

    // Hovered/spectated/selected player indices
    public static int? HoveredPlayerIndex = null; // Currently hovered player
    public static int? SpectatePlayerIndex = null; // Currently spectated player
    public static int? SelectedSpawnPlayerIndex = null; // Currently selected player to spawn on

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

    public override void UpdateUI(GameTime gameTime)
    {
        if (ui == null)
            return;

        if (!ShouldShowUI)
        {
            if (ui.CurrentState != null)
                ui.SetState(null);

            return;
        }

        if (ui.CurrentState != spawnSelectorState)
            ui.SetState(spawnSelectorState);

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

    /// <summary>
    /// If a target player exists, moves the screen position to follow the spectated player.
    /// </summary>
    public override void ModifyScreenPosition()
    {
        Player local = Main.LocalPlayer;

        bool canSpectate = local != null && local.active && (local.dead || IsAliveSpawnRegionInstant);

        if (!canSpectate)
        {
            SpectatePlayerIndex = null;
            return;
        }

        if (SpectatePlayerIndex is not int idx || idx < 0 || idx >= Main.maxPlayers)
        {
            SpectatePlayerIndex = null;
            return;
        }

        Player target = Main.player[idx];
        if (target == null || !target.active || target.dead)
        {
            SpectatePlayerIndex = null;
            return;
        }

        // Center camera on player with correct zoom
        //Vector2 zoom = Main.GameViewMatrix.Zoom;
        //Vector2 halfView = new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f / zoom;
        //Vector2 focus = target.MountedCenter;
        //Vector2 viewMatrixTranslation = Main.GameViewMatrix.Translation / zoom;
        //Main.screenPosition = focus - halfView - viewMatrixTranslation;

        // TeamSpectate code
        Main.screenPosition = Main.player[target.whoAmI].position - new Vector2(Main.screenWidth, Main.screenHeight) / 2;
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
