using Microsoft.Xna.Framework;
using PvPAdventure.Common.Spectator.SpectatorMode;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Spectator;

[Autoload(Side = ModSide.Client)]
public class SpectatorTargetSystem : ModSystem
{
    private static int target = -1;

    #region Targeting
    private static bool CanTarget(int playerId)
    {
        return playerId >= 0 &&
            playerId < Main.maxPlayers &&
            playerId != Main.myPlayer &&
            Main.player[playerId].active &&
            (SpectatorModeSystem.IsInPlayerMode(Main.player[playerId]) || SpectatorModeSystem.IsInSpectateMode(Main.player[playerId]) || Main.player[playerId].ghost);
    }

    public static void SetPlayerTarget(int slot)
    {
        int next = CanTarget(slot) ? slot : -1;

        if (target != next)
            Log.Chat($"target {target}->{next}");

        target = next;
    }

    public static List<int> GetTargets(int exclude = -1)
    {
        List<int> targets = [];

        for (int i = 0; i < Main.maxPlayers; i++)
            if (CanTarget(i) && i != exclude)
                targets.Add(i);

        return targets;
    }

    public static void ClearTarget()
    {
        if (target == -1)
            return;

        Log.Chat($"clear {target}");

        Player local = Main.LocalPlayer;
        if (local?.active == true)
        {
            Vector2 screenPosition = local.Center - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
            SpectateCameraFade.SetScreenPosition(screenPosition);
        }

        target = -1;
    }

    public static bool IsTargeting(Player player) => player?.active == true && GetPlayerTarget()?.whoAmI == player.whoAmI;
    public static Player GetPlayerTarget()
    {
        if (!SpectatorModeSystem.IsInSpectateMode(Main.LocalPlayer) || !CanTarget(target))
            return null;

        return Main.player[target];
    }
    #endregion

    #region Hooks
    public override void ModifyScreenPosition()
    {
        if (GetPlayerTarget() is Player player)
        {
            Vector2 screenPosition = player.Center - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
            SpectateCameraFade.SetScreenPosition(screenPosition);
        }
    }
    #endregion


    #region Cycle targets
    private static void CycleTarget(bool forward)
    {
        if (!SpectatorModeSystem.IsInSpectateMode(Main.LocalPlayer))
            return;

        List<int> targets = GetTargets(Main.myPlayer);
        if (targets.Count == 0)
        {
            ClearTarget();
            return;
        }

        int index = targets.IndexOf(target);
        index = index < 0 ? (forward ? 0 : targets.Count - 1) : forward ? (index + 1) % targets.Count : (index - 1 + targets.Count) % targets.Count;
        target = targets[index];
    }
    public static void NextPlayerTarget() => CycleTarget(forward: true);

    public static void PreviousPlayerTarget() => CycleTarget(forward: false);

    #endregion
}
