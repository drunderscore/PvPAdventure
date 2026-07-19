using PvPAdventure.Common.Statistics;
using PvPAdventure.Core.Compat;
using PvPAdventure.Core.Net;
using PvPFramework.Common;
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

        switch (subPacket)
        {
            case GameManagerPacketType.StartGame:
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

            case GameManagerPacketType.UpdateCountdown:
                {
                    int newSeconds = reader.ReadInt32();
                    var gm = ModContent.GetInstance<GameManager>();
                    gm.SetCountdown(newSeconds);
                    break;
                }

            case GameManagerPacketType.AdjustGameTime:
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

            case GameManagerPacketType.EndGame:
                {
                    if (Main.netMode != NetmodeID.Server)
                    {
                        return;
                    }

                    var gm = ModContent.GetInstance<GameManager>();
                    gm.EndGame();
                    return;
                }

            case GameManagerPacketType.SetPoints:
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

                    return;
                }
        }
    }
}
