using PvPAdventure.Common.Statistics;
using System.IO;
using Terraria;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.GameTimer;

public static class GameTimerNetHandler
{
    public enum GameTimerPacketType : byte
    {
        PauseGame,
        StartGame,
        AdjustGameTime,
        EndGame,
        SetPoints
    }

    public static void HandlePacket(BinaryReader reader, int whoAmI)
    {
        var subPacket = (GameTimerPacketType)reader.ReadByte();

        switch (subPacket)
        {
            case GameTimerPacketType.PauseGame:
                {
                    if (Main.netMode != NetmodeID.Server)
                    {
                        return;
                    }

                    var pm = ModContent.GetInstance<PauseManager>();
                    pm.TogglePause();

                    return;
                }

            case GameTimerPacketType.StartGame:
                {
                    int time = reader.ReadInt32();
                    int countdown = reader.ReadInt32();

                    if (Main.netMode != NetmodeID.Server)
                    {
                        return;
                    }

                    var gm = ModContent.GetInstance<GameManager>();

                    if (gm.CurrentPhase == GameManager.Phase.Playing)
                    {
                        return;
                    }

                    if (gm._startGameCountdown.HasValue)
                    {
                        return;
                    }

                    gm.StartGame(time, countdown);
                    return;
                }

            case GameTimerPacketType.AdjustGameTime:
                {
                    int deltaFrames = reader.ReadInt32();

                    if (Main.netMode != NetmodeID.Server)
                    {
                        return;
                    }

                    var gm = ModContent.GetInstance<GameManager>();
                    gm.AdjustTimeRemaining(deltaFrames);
                    return;
                }

            case GameTimerPacketType.EndGame:
                {
                    if (Main.netMode != NetmodeID.Server)
                    {
                        return;
                    }

                    var gm = ModContent.GetInstance<GameManager>();
                    gm.EndGame();
                    return;
                }

            case GameTimerPacketType.SetPoints:
                {
                    var team = (Team)reader.ReadByte();
                    int value = reader.ReadInt32();

                    var pointsManager = ModContent.GetInstance<PointsManager>();
                    pointsManager._points[team] = value;

                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.WorldData);
                        return;
                    }

                    if (!Main.dedServ)
                    {
                        pointsManager.UiScoreboard?.Invalidate();
                    }

                    return;
                }
        }
    }
}
