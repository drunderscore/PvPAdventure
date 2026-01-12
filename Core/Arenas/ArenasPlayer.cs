using PvPAdventure.Core.Arenas.UI.JoinUI;
using PvPAdventure.Core.Arenas.UI.LoadoutUI;
using PvPAdventure.System;
using PvPAdventure.System.Client;
using SubworldLibrary;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Arenas;

internal class ArenasPlayer : ModPlayer
{
    #region Keybind handling
    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        var keybinds = ModContent.GetInstance<Keybinds>();
        var gm = ModContent.GetInstance<GameManager>();

        // Toggle UI
        if (keybinds.Loadout.JustPressed && gm.CurrentPhase == GameManager.Phase.Waiting)
        {
            ArenasLoadoutUISystem.Toggle();
        }

        if (keybinds.Arenas.JustPressed && gm.CurrentPhase == GameManager.Phase.Waiting)
        {
            ArenasJoinUISystem.Toggle();
        }
    }
    #endregion

    private const int DamageLockDuration = 240; // 4 seconds

    private int damageLockTicks;

    public bool DamageLocked => damageLockTicks > 0;
    public bool IsMoving { get; private set; }

    public override void ResetEffects()
    {
        if (SubworldSystem.AnyActive())
        {
            // Ensure Clamp life to 400 while in arenas
            Player.statLifeMax2 = 400;
            if (Player.statLife > 400)
                Player.statLife = 400;
        }
    }

    public override void OnHurt(Player.HurtInfo info)
    {
        if (!SubworldSystem.AnyActive())
            return;

        damageLockTicks = DamageLockDuration;
    }

    public override void PostUpdate()
    {
        if (!SubworldSystem.AnyActive())
            return;

        if (damageLockTicks > 0)
            damageLockTicks--;

        IsMoving =
            Player.velocity.LengthSquared() > 0.01f ||
            Player.controlLeft ||
            Player.controlRight ||
            Player.controlUp ||
            Player.controlDown;
    }

    public bool CanSelectLoadout(out string reason)
    {
        if (DamageLocked)
        {
            reason = "recently damaged";
            return false;
        }

        if (IsMoving)
        {
            reason = "must stand still";
            return false;
        }

        reason = null;
        return true;
    }
}
