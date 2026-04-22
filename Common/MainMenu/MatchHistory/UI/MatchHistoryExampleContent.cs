using System;
using Terraria.Enums;
using Terraria.ID;

namespace PvPAdventure.Common.MainMenu.MatchHistory.UI;

internal static class MatchHistoryExampleContent
{
    public static MatchResult[] Create()
    {
        const ulong localSteamId = 76561198000000001;

        MatchResult[] matches =
        [
            new(
                start: DateTime.Now.AddDays(-1).AddHours(-2),
                end: DateTime.Now.AddDays(-1).AddHours(-1).AddMinutes(-18),
                win: true,
                localSteamId: localSteamId,
                teamPoints:
                [
                    new TeamPoints(Team.Red, 128),
                    new TeamPoints(Team.Blue, 102),
                    new TeamPoints(Team.Green, 87),
                    new TeamPoints(Team.Yellow, 75),
                    new TeamPoints(Team.Pink, 74)
                ],
                players:
                [
                    new PlayerKD(Team.Red, localSteamId, "Erky", 22, 8),
                    new PlayerKD(Team.Red, 76561198000000002, "Volley", 14, 11),
                    new PlayerKD(Team.Blue, 76561198000000003, "BlueMage", 19, 14),
                    new PlayerKD(Team.Green, 76561198000000004, "JungleFox", 12, 16),
                    new PlayerKD(Team.Yellow, 76561198000000005, "GoldSpark", 10, 18)
                ],
                bossScoreboard:
                [
                    new TeamBossCompletion(NPCID.EyeofCthulhu, Team.Red),
                    new TeamBossCompletion(NPCID.SkeletronHead, Team.Blue),
                    new TeamBossCompletion(NPCID.WallofFlesh, Team.Red)
                ]),

            new(
                start: DateTime.Now.AddDays(-3).AddHours(-5),
                end: DateTime.Now.AddDays(-3).AddHours(-4).AddMinutes(-11),
                win: false,
                localSteamId: localSteamId,
                teamPoints:
                [
                    new TeamPoints(Team.Blue, 141),
                    new TeamPoints(Team.Red, 140),
                    new TeamPoints(Team.Green, 96),
                    new TeamPoints(Team.Yellow, 92)
                ],
                players:
                [
                    new PlayerKD(Team.Red, localSteamId, "Erky", 17, 13),
                    new PlayerKD(Team.Red, 76561198000000002, "Volley", 15, 12),
                    new PlayerKD(Team.Blue, 76561198000000003, "BlueMage", 18, 10),
                    new PlayerKD(Team.Blue, 76561198000000006, "MirrorTech", 16, 9),
                    new PlayerKD(Team.Green, 76561198000000007, "Leafline", 11, 15)
                ],
                bossScoreboard:
                [
                    new TeamBossCompletion(NPCID.QueenBee, Team.Green),
                    new TeamBossCompletion(NPCID.Retinazer, Team.Blue),
                    new TeamBossCompletion(NPCID.Spazmatism, Team.Red)
                ]),

            new(
                start: DateTime.Now.AddDays(-6).AddHours(-8),
                end: DateTime.Now.AddDays(-6).AddHours(-7).AddMinutes(-6),
                win: true,
                localSteamId: localSteamId,
                teamPoints:
                [
                    new TeamPoints(Team.Red, 156),
                    new TeamPoints(Team.Blue, 131),
                    new TeamPoints(Team.Green, 115),
                    new TeamPoints(Team.Yellow, 111)
                ],
                players:
                [
                    new PlayerKD(Team.Red, localSteamId, "Erky", 28, 7),
                    new PlayerKD(Team.Red, 76561198000000008, "TrainHorn", 12, 10),
                    new PlayerKD(Team.Blue, 76561198000000003, "BlueMage", 20, 13),
                    new PlayerKD(Team.Green, 76561198000000009, "ArenaEnjoyer", 14, 14),
                    new PlayerKD(Team.Yellow, 76561198000000010, "Sunflare", 9, 17)
                ],
                bossScoreboard:
                [
                    new TeamBossCompletion(NPCID.Plantera, Team.Red),
                    new TeamBossCompletion(NPCID.Golem, Team.Red),
                    new TeamBossCompletion(NPCID.HallowBoss, Team.Blue)
                ])
        ];

        return matches;
    }
}
