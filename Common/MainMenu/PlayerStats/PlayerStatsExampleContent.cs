using PvPAdventure.Common.MainMenu.MatchHistory;
using System.Collections.Generic;
using Terraria.Enums;

namespace PvPAdventure.Common.MainMenu.PlayerStats;

public static class PlayerStatsExampleContent
{
    public static ulong ExampleSteamUserId => 1;

    public static IReadOnlyList<MatchResult> CreateMatches()
    {
        List<MatchResult> matches =
        [
            new MatchResult
            {
                Win = true,
                LocalSteamId = ExampleSteamUserId,
                Players =
                [
                    new PlayerKD
                    {
                        SteamId = ExampleSteamUserId,
                        Kills = 12,
                        Deaths = 4,
                        Team = Team.Red
                    },
                    new PlayerKD
                    {
                        SteamId = 2,
                        Kills = 7,
                        Deaths = 9,
                        Team = Team.Blue
                    }
                ],
                TeamPoints =
                [
                    new TeamPoints
                    {
                        Team = Team.Red,
                        Points = 300
                    },
                    new TeamPoints
                    {
                        Team = Team.Blue,
                        Points = 180
                    }
                ]
            },
            new MatchResult
            {
                Win = false,
                LocalSteamId = ExampleSteamUserId,
                Players =
                [
                    new PlayerKD
                    {
                        SteamId = ExampleSteamUserId,
                        Kills = 8,
                        Deaths = 10,
                        Team = Team.Blue
                    },
                    new PlayerKD
                    {
                        SteamId = 3,
                        Kills = 11,
                        Deaths = 6,
                        Team = Team.Red
                    }
                ],
                TeamPoints =
                [
                    new TeamPoints
                    {
                        Team = Team.Blue,
                        Points = 220
                    },
                    new TeamPoints
                    {
                        Team = Team.Red,
                        Points = 290
                    }
                ]
            },
            new MatchResult
            {
                Win = true,
                LocalSteamId = ExampleSteamUserId,
                Players =
                [
                    new PlayerKD
                    {
                        SteamId = ExampleSteamUserId,
                        Kills = 15,
                        Deaths = 5,
                        Team = Team.Green
                    },
                    new PlayerKD
                    {
                        SteamId = 4,
                        Kills = 6,
                        Deaths = 12,
                        Team = Team.Yellow
                    }
                ],
                TeamPoints =
                [
                    new TeamPoints
                    {
                        Team = Team.Green,
                        Points = 340
                    },
                    new TeamPoints
                    {
                        Team = Team.Yellow,
                        Points = 200
                    }
                ]
            }
        ];

        return matches;
    }
}