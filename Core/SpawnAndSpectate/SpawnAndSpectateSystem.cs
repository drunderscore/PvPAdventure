using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.SpawnAndSpectate.SpawnSelectorUI;
using PvPAdventure.Core.SpawnAndSpectate.SpectateUI;
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
    public SpawnSelectorState spawnSelectorState; // allows spawn selection and spectating
    public SpectateState spectateState; // allows spectating only
    public enum SpawnSpectateMode
    {
        None,
        Spectate,
        SpawnSelect
    }
    public static SpawnSpectateMode CurrentMode { get; private set; }
    public static void SetMode(SpawnSpectateMode mode)
    {
        if (CurrentMode == mode)
            return;

        CurrentMode = mode;

        var sys = ModContent.GetInstance<SpawnAndSpectateSystem>();
        sys.ui.SetState(mode switch
        {
            SpawnSpectateMode.None => null,
            SpawnSpectateMode.Spectate => sys.spectateState,
            SpawnSpectateMode.SpawnSelect => sys.spawnSelectorState,
            _ => null
        });
    }

    public static bool IsRespawnFrozen(Player p) => p != null && p.dead && p.respawnTimer == 2;

    public static bool IsValidTeammateTarget(int idx, bool requireAlive)
    {
        if (idx < 0 || idx >= Main.maxPlayers)
            return false;

        Player local = Main.LocalPlayer;
        Player t = Main.player[idx];

        if (local == null || t == null || !t.active)
            return false;

        if (t.whoAmI == local.whoAmI)
            return false;

        if (local.team == 0 || t.team != local.team)
            return false;

        if (requireAlive && t.dead)
            return false;

        return true;
    }

    public static void ToggleSpectate(int idx)
    {
        if (!Main.LocalPlayer.dead)
            return;

        if (!IsValidTeammateTarget(idx, requireAlive: true))
            return;

        SpectatePlayerIndex = (SpectatePlayerIndex == idx) ? null : idx;
    }

    public static void ToggleSpawnSelection(int idx)
    {
        if (!IsValidTeammateTarget(idx, requireAlive: true))
            return;

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
        spectateState = new();

        // Draw on map
        Main.OnPostFullscreenMapDraw += DrawOnFullscreenMap;
    }

    public override void Unload()
    {
        Main.OnPostFullscreenMapDraw -= DrawOnFullscreenMap;
    }

    public static bool MapSpectateOpen { get; private set; }

    private static UIState GetStateForMode(SpawnSpectateMode mode, SpawnAndSpectateSystem sys) => mode switch
    {
        SpawnSpectateMode.None => null,
        SpawnSpectateMode.Spectate => sys.spectateState,
        SpawnSpectateMode.SpawnSelect => sys.spawnSelectorState,
        _ => null
    };

    public override void UpdateUI(GameTime gameTime)
    {
        if (ui == null)
            return;

        UIState desired = GetStateForMode(CurrentMode, this);
        if (ui.CurrentState != desired)
        {
            ui.SetState(desired);
        }

        if (ui.CurrentState != null)
        {
            ui.Update(gameTime);
        }
    }

    #region Spectating
    public static void ClearSpectate()
    {
        SpectatePlayerIndex = null;
    }

    public static void TrySetSpectate(int playerIndex)
    {
        if (!Main.LocalPlayer.dead)
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

        if (local == null || !local.active || !local.dead)
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

        Vector2 zoom = Main.GameViewMatrix.Zoom;
        Vector2 halfView = new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
        halfView /= zoom;

        // Center screen on target player
        Main.screenPosition = target.Center - halfView;
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
