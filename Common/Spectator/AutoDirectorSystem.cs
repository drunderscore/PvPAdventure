using Microsoft.Xna.Framework;
using PvPAdventure.Common.Spectator.SpectatorMode;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Spectator;

[Autoload(Side = ModSide.Client)]
internal sealed class AutoDirectorSystem : ModSystem
{
    private const int ChangeCooldownTicks = 4 * 60;
    private const int ForceChangeTicks = 60 * 60;
    private const int IdleChangeTicks = 5 * 60;
    private const float MovingSpeedSq = 0.25f;

    private static readonly Vector2[] lastCenters = new Vector2[Main.maxPlayers];
    private static readonly int[] stillTicks = new int[Main.maxPlayers];

    private static bool enabled;
    private static int cooldownTicks;
    private static int ticksSinceTargetChange;
    private static int pendingPvpDamager = -1;

    public static bool Enabled
    {
        get => enabled;
        set
        {
            if (enabled == value)
                return;

            enabled = value;
            pendingPvpDamager = -1;
            cooldownTicks = 0;
            ticksSinceTargetChange = 0;

            if (enabled)
                SpectatorTargetSystem.ClearTarget(preserveAutoDirector: true, moveCameraToLocal: false);
        }
    }

    public static void ReportPvpDamage(int damagerPlayerIndex)
    {
        if (!enabled || !IsTargetable(damagerPlayerIndex))
            return;

        pendingPvpDamager = damagerPlayerIndex;

        if (cooldownTicks <= 0)
            ChangeTarget(damagerPlayerIndex, "PvP damage");
    }

    public override void PreUpdatePlayers()
    {
        if (!enabled)
            return;

        Player local = Main.LocalPlayer;
        if (local?.active != true || !SpectatorModeSystem.IsInSpectateMode(local))
            return;

        UpdateMovementMemory();

        if (cooldownTicks > 0)
            cooldownTicks--;

        ticksSinceTargetChange++;

        int current = SpectatorTargetSystem.GetLockedPlayerTarget()?.whoAmI ?? -1;

        if (!IsTargetable(current))
        {
            ChangeTarget(ChooseBestTarget(-1), "no current target");
            return;
        }

        if (pendingPvpDamager >= 0 && cooldownTicks <= 0 && IsTargetable(pendingPvpDamager) && pendingPvpDamager != current)
        {
            ChangeTarget(pendingPvpDamager, "PvP damage");
            return;
        }

        if (cooldownTicks > 0)
            return;

        if (stillTicks[current] >= IdleChangeTicks)
        {
            ChangeTarget(ChooseBestTarget(current), "current target idle");
            return;
        }

        if (ticksSinceTargetChange >= ForceChangeTicks)
            ChangeTarget(ChooseBestTarget(current), "one minute refresh");
    }

    private static void UpdateMovementMemory()
    {
        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player player = Main.player[i];
            if (player?.active != true)
            {
                stillTicks[i] = 0;
                lastCenters[i] = Vector2.Zero;
                continue;
            }

            if (lastCenters[i] == Vector2.Zero)
            {
                lastCenters[i] = player.Center;
                continue;
            }

            bool moving = player.velocity.LengthSquared() >= MovingSpeedSq || Vector2.DistanceSquared(lastCenters[i], player.Center) >= 4f;
            stillTicks[i] = moving ? 0 : stillTicks[i] + 1;
            lastCenters[i] = player.Center;
        }
    }

    private static int ChooseBestTarget(int exclude)
    {
        List<int> targets = SpectatorTargetSystem.GetTargets(Main.myPlayer);
        int best = -1;
        float bestScore = float.MinValue;

        foreach (int playerIndex in targets)
        {
            if (playerIndex == exclude && targets.Count > 1)
                continue;

            Player player = Main.player[playerIndex];
            float speedScore = player.velocity.LengthSquared();
            float idlePenalty = stillTicks[playerIndex] >= IdleChangeTicks ? -100f : 0f;
            float score = speedScore + idlePenalty;

            if (score <= bestScore)
                continue;

            bestScore = score;
            best = playerIndex;
        }

        return best;
    }

    private static void ChangeTarget(int playerIndex, string reason)
    {
        if (!IsTargetable(playerIndex))
            return;

        int current = SpectatorTargetSystem.GetLockedPlayerTarget()?.whoAmI ?? -1;
        if (current == playerIndex)
            return;

        SpectatorTargetSystem.SetPlayerTarget(playerIndex, preserveAutoDirector: true);
        cooldownTicks = ChangeCooldownTicks;
        ticksSinceTargetChange = 0;
        pendingPvpDamager = -1;
        DebugLog.Chat($"Auto director target: {Main.player[playerIndex].name} ({reason})");
    }

    private static bool IsTargetable(int playerIndex)
    {
        return playerIndex >= 0 && SpectatorTargetSystem.GetTargets(Main.myPlayer).Contains(playerIndex);
    }
}

internal sealed class AutoDirectorDamagePlayer : ModPlayer
{
    public override void PostHurt(Player.HurtInfo info)
    {
        if (Main.dedServ || !info.PvP)
            return;

        int damager = info.DamageSource.SourcePlayerIndex;
        if (damager < 0 || damager == Player.whoAmI)
            return;

        AutoDirectorSystem.ReportPvpDamage(damager);
    }
}
