using PvPAdventure.Common.Statistics;
using PvPAdventure.Core.Compat;
using System;
using System.IO;
using Terraria;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Game;

public static class GameManagerNetHandler
{
    public enum GameManagerPacketType : byte
    {
        StartGame,
        AdjustGameTime,
        UpdateCountdown,
        EndGame,
        SetPoints,
    }

    public static void HandlePacket(BinaryReader reader, int whoAmI)
    {
        GameManagerPacketType subPacket = (GameManagerPacketType)reader.ReadByte();

        if (Main.netMode != NetmodeID.Server)
            return;

        if (whoAmI is < 0 or >= Main.maxPlayers || !ErkySSCCompat.IsAdmin(whoAmI))
        {
            Log.Warn($"Rejected unauthorized game-manager packet. Sender={whoAmI}, Packet={subPacket}");
            return;
        }

        if (!Enum.IsDefined(subPacket))
        {
            Log.Warn($"Rejected unknown game-manager packet. Sender={whoAmI}, Packet={(byte)subPacket}");
            return;
        }

        switch (subPacket)
        {
            case GameManagerPacketType.StartGame:
            {
                int time = reader.ReadInt32();
                int countdown = reader.ReadInt32();
                if (time is < 0 or > GameManager.MaxGameDurationFrames ||
                    countdown is < 0 or > GameManager.MaxCountdownSeconds)
                {
                    Log.Warn($"Rejected invalid StartGame values. Sender={whoAmI}, Time={time}, Countdown={countdown}");
                    return;
                }

                GameManager gameManager = ModContent.GetInstance<GameManager>();
                if (gameManager.CurrentPhase == GameManager.Phase.Playing || gameManager._startGameCountdown.HasValue)
                    return;

                gameManager.StartGame(time, countdown);
                return;
            }

            case GameManagerPacketType.UpdateCountdown:
            {
                int newSeconds = reader.ReadInt32();
                if (newSeconds is < 0 or > GameManager.MaxCountdownSeconds)
                {
                    Log.Warn($"Rejected invalid countdown. Sender={whoAmI}, Countdown={newSeconds}");
                    return;
                }

                ModContent.GetInstance<GameManager>().SetCountdown(newSeconds);
                return;
            }

            case GameManagerPacketType.AdjustGameTime:
            {
                int deltaFrames = reader.ReadInt32();
                if (deltaFrames is < -GameManager.MaxGameDurationFrames or > GameManager.MaxGameDurationFrames)
                {
                    Log.Warn($"Rejected invalid game-time adjustment. Sender={whoAmI}, DeltaFrames={deltaFrames}");
                    return;
                }

                ModContent.GetInstance<GameManager>().AdjustTimeRemaining(deltaFrames);
                return;
            }

            case GameManagerPacketType.EndGame:
                ModContent.GetInstance<GameManager>().EndGame();
                return;

            case GameManagerPacketType.SetPoints:
            {
                Team team = (Team)reader.ReadByte();
                int value = reader.ReadInt32();
                if (team == Team.None || !Enum.IsDefined(team))
                {
                    Log.Warn($"Rejected invalid SetPoints team. Sender={whoAmI}, Team={(byte)team}");
                    return;
                }

                ModContent.GetInstance<PointsManager>().SetTeamPoints(team, value);
                return;
            }
        }
    }
}
