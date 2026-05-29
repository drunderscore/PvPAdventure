using PvPAdventure.Common.Statistics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Enums;

namespace PvPAdventure.Common.GameTimer;

internal readonly record struct MatchRewardContext(
    Team Team,
    int TeamPoints,
    int Kills,
    int Deaths
);

/// <summary>
/// Calculates the match reward for a player based on their performance and their team's performance in the match.
/// </summary>
internal static class MatchRewardCalculator
{
    public static uint Calculate(MatchRewardContext context)
    {
        int reward = context.TeamPoints + CalculateKillDeathReward(context.Kills, context.Deaths);
        return reward <= 0 ? 0u : (uint)reward;
    }

    public static MatchRewardContext CreateContext(Player player, PointsManager pointsManager)
    {
        StatisticsPlayer statsPlayer = player.GetModPlayer<StatisticsPlayer>();
        Team team = (Team)player.team;

        return new MatchRewardContext(
            Team: team,
            TeamPoints: GetTeamPoints(pointsManager, team),
            Kills: statsPlayer.Kills,
            Deaths: statsPlayer.Deaths);
    }

    private static int GetTeamPoints(PointsManager pointsManager, Team team)
    {
        if (team == Team.None)
            return 0;

        return pointsManager.Points.TryGetValue(team, out int points) ? points : 0;
    }

    /// <summary>
    /// Formula: 
    /// K/D * 10.
    /// K/D multiplier. 
    /// Minimum 0 points.
    /// 
    /// </summary>
    // private static int CalculateKdReward(int kills, int deaths)
    // {
    //     if (kills <= 0)
    //         return 0;

    //     int safeDeaths = Math.Max(deaths, 1);
    //     float kd = kills / (float)safeDeaths;

    //     return (int)Math.Round(kd * 10f);
    // }

    /// <summary>
    /// Formula: 
    /// Kills minus deaths.
    /// Minimum 0 points.
    /// </summary>
    private static int CalculateKillDeathReward(int kills, int deaths)
    {
        return Math.Max(kills - deaths, 0);
    }

    /// <summary>
    /// EJ's silly formula.
    /// Punishes deaths more harshly and rewards kills more generously, with a soft exponential curve to prevent extreme values.
    /// </summary>
    // private static int CalculateKillDeathReward(int kills, int deaths)
    // {
    //     int delta = kills - deaths;
    //     if (delta == 0)
    //         return 0;

    //     double phi = (1d + Math.Sqrt(5d)) / 2d;
    //     double k = Math.Max(kills, 0);
    //     double d = Math.Abs(delta);

    //     double reward = k * Math.Pow(d, 1d / phi + 1d / 30d) *
    //         (1d + Math.Sin(phi * Math.Log(d)) / 10d) *
    //         (phi / (phi + 1d / (Math.Sqrt(d) + 1d))) +
    //         Math.Log(d + 1d) / 50d;

    //     double signedReward = Math.Sign(delta) * reward;

    //     if (signedReward > int.MaxValue)
    //         return int.MaxValue;

    //     if (signedReward < int.MinValue)
    //         return int.MinValue;

    //     return (int)Math.Round(signedReward, MidpointRounding.AwayFromZero);
    // }

    /// <summary>
    /// Formula: 
    /// Kills * 2.25 + sqrt(Kills) * 5 - Deaths^0.85 * 0.65.
    /// Soft cap of 90 points and a max of 75 points.
    /// </summary>
    // private static int CalculateKillDeathReward(int kills, int deaths)
    // {
    //     if (kills <= 0)
    //         return 0;

    //     float killValue = kills * 2.25f + MathF.Sqrt(kills) * 5f;
    //     float deathPenalty = MathF.Pow(deaths, 0.85f) * 0.65f;
    //     float softCap = 90f * (1f - MathF.Exp(-killValue / 90f));

    //     int reward = (int)MathF.Round(softCap - deathPenalty);
    //     return Math.Clamp(reward, 0, 75);
    // }
}
