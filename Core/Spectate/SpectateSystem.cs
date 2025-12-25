using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Core.Spectate;

/// <summary>
/// Client-side system for managing spectate mode.
/// </summary>
[Autoload(Side = ModSide.Client)]
internal class SpectateSystem : ModSystem
{
    // UI Elements
    public UserInterface ui;
    public UIState spectateState;
    public SpectateElement spectateElement;

    // Track player
    public int? TargetPlayerIndex;
    public bool ShowAllPlayers; // used for admin spectate mode

    // Track state changes
    private bool wasDeadLastTick;
    private bool openUiNextTick;

    public bool IsActive() => ui?.CurrentState == spectateState;

#if DEBUG
    private bool lastUiActiveDebug;
#endif
    public void ApplySpectateUiPosition(Vector2 pos)
    {
        if (Main.dedServ)
            return;

        if (spectateElement == null)
            return;

        spectateElement._hAlign = pos.X;
        spectateElement._vAlign = pos.Y;

        spectateElement.HAlign = pos.X;
        spectateElement.VAlign = pos.Y;

        if (ui?.CurrentState == spectateState)
            spectateElement.Recalculate();
    }

    public override void OnWorldLoad()
    {
        ui = new UserInterface();
        spectateState = new UIState();
        spectateElement = new SpectateElement();
        spectateState.Append(spectateElement);

        TargetPlayerIndex = null;
        wasDeadLastTick = false;
        openUiNextTick = false;
    }

    public override void OnWorldUnload()
    {
        TargetPlayerIndex = null;
        ui?.SetState(null);
        wasDeadLastTick = false;
        openUiNextTick = false;
    }

    public void SetTarget(int? target)
    {
#if DEBUG
        if (TargetPlayerIndex != target)
        {
            Main.NewText($"[DEBUG/SPECTATE]: SetTarget {TargetPlayerIndex?.ToString() ?? "null"} -> {target?.ToString() ?? "null"}");
        }
#endif
        TargetPlayerIndex = target;
    }

    public void EnterSpectateUI()
    {
#if DEBUG
        Main.NewText($"[DEBUG/SPECTATE]: EnterSpectateUI target={TargetPlayerIndex?.ToString() ?? "null"}");
#endif
        spectateElement?.Rebuild();
        ui?.SetState(spectateState);
    }

    public void ExitSpectateUI()
    {
#if DEBUG
        Main.NewText($"[DEBUG/SPECTATE]: ExitSpectateUI target={TargetPlayerIndex?.ToString() ?? "null"}");
#endif
        ui?.SetState(null);
        TargetPlayerIndex = null;
    }

    public override void UpdateUI(GameTime gameTime)
    {
        ui?.Update(gameTime);

        if (Main.gameMenu || Main.dedServ)
        {
            return;
        }

        Player me = Main.LocalPlayer;
        bool deadNow = me.dead;

        if (!wasDeadLastTick && deadNow)
        {
            var cfg = ModContent.GetInstance<AdventureClientConfig>();
            if (cfg != null && cfg.SpectateTeammatesOnDeath)
            {
                openUiNextTick = true;
            }
        }

        if (wasDeadLastTick && !deadNow)
        {
            ExitSpectateUI();
        }

        wasDeadLastTick = deadNow;

        if (openUiNextTick)
        {
            openUiNextTick = false;

            if (TargetPlayerIndex is not null)
            {
                EnterSpectateUI();
            }
        }

        if (deadNow && TargetPlayerIndex is not null && ui?.CurrentState != spectateState)
        {
            EnterSpectateUI();
        }

#if DEBUG
        bool uiActive = ui?.CurrentState == spectateState;

        if (lastUiActiveDebug != uiActive)
        {
            lastUiActiveDebug = uiActive;
            Main.NewText($"[DEBUG/SPECTATE]: UI active changed -> {uiActive}");
        }
#endif

        // Prevent interaction with chat/sign/chest while spectating
        if (!IsActive() || Main.drawingPlayerChat || Main.editSign || Main.editChest)
            return;
    }

    public override void ModifyScreenPosition()
    {
        if (Main.dedServ || Main.gameMenu)
        {
            return;
        }

        if (TargetPlayerIndex is not int idx)
        {
            return;
        }

        if (idx < 0 || idx >= Main.maxPlayers)
        {
            TargetPlayerIndex = null;
            return;
        }

        Player target = Main.player[idx];
        if (target == null || !target.active || target.dead)
        {
#if DEBUG
            string activeStr = target == null ? "null" : target.active.ToString();
            string deadStr = target == null ? "null" : target.dead.ToString();
            string nameStr = target == null ? "null" : target.name;
            Main.NewText($"[DEBUG/SPECTATE]: Target invalid -> clearing (idx={idx} name={nameStr} active={activeStr} dead={deadStr})");
#endif
            TargetPlayerIndex = null;
            return;
        }

        Vector2 zoom = Main.GameViewMatrix.Zoom;
        Vector2 halfView = new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
        halfView /= zoom;

        // Center screen on target player
        Main.screenPosition = target.Center - halfView;
    }

    public List<int> GetTeammateIds(bool includeAllPlayers)
    {
        List<int> ids = [];

#if DEBUG
        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            int me = Main.LocalPlayer.whoAmI;
            ids.Add(me);
            ids.Add(me);
            return ids;
        }
#endif

        Player mePlayer = Main.LocalPlayer;

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player p = Main.player[i];

            if (p == null || !p.active || p.dead || p.whoAmI == mePlayer.whoAmI)
            {
                continue;
            }

            if (!includeAllPlayers)
            {
                if (mePlayer.team != 0 && p.team != mePlayer.team)
                {
                    continue;
                }
            }

            ids.Add(p.whoAmI);
        }

        return ids;
    }
}
