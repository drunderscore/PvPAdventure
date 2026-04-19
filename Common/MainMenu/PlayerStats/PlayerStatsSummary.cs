using PvPAdventure.Common.MainMenu.API;
using PvPAdventure.Common.MainMenu.MatchHistory;
using System.Collections.Generic;
using System.Globalization;
using Terraria.Enums;

namespace PvPAdventure.Common.MainMenu.PlayerStats;

public readonly struct PlayerStatsSummary
{
    public int Kills { get; init; }
    public int Deaths { get; init; }
    public int Wins { get; init; }
    public int Losses { get; init; }
    public int TeamPointsTotal { get; init; }

    public string KillDeathRatio => FormatRatio(Kills, Deaths);
    public string WinLossRatio => FormatRatio(Wins, Losses);

    private static string FormatRatio(int a, int b)
    {
        if (b <= 0)
        {
            if (a <= 0)
                return "0";

            return "INF";
        }

        return ((float)a / b).ToString("0.00", CultureInfo.InvariantCulture);
    }
}
