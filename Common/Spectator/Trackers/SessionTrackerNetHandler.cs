using PvPAdventure.Core.Net;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Spectator.Trackers;

internal static class SessionTrackerNetHandler
{
    private enum SessionTrackerOperation : byte
    {
        RequestFullSync,
        FullSync
    }

    public static void HandlePacket(BinaryReader reader, int sender)
    {
        SessionTrackerOperation operation = (SessionTrackerOperation)reader.ReadByte();

        switch (operation)
        {
            case SessionTrackerOperation.RequestFullSync:
                SendFullSync(sender);
                break;

            case SessionTrackerOperation.FullSync:
                ReceiveFullSync(reader);
                break;
        }
    }

    private static ModPacket GetPacket(SessionTrackerOperation operation)
    {
        ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.SessionTracker);
        packet.Write((byte)operation);
        return packet;
    }

    public static void SendRequestFullSync()
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        GetPacket(SessionTrackerOperation.RequestFullSync).Send();
    }

    public static void SendFullSync(int toClient = -1)
    {
        if (!_TrackerStatus.IsEnabled)
        {
            return;
        }

        if (Main.netMode != NetmodeID.Server)
            return;

        ModPacket packet = GetPacket(SessionTrackerOperation.FullSync);
        packet.Write(SessionTracker.Sessions.Count);

        DateTime now = DateTime.UtcNow;

        foreach ((int playerIndex, DateTime start) in SessionTracker.Sessions)
        {
            packet.Write(playerIndex);
            packet.Write((now - start).Ticks);
        }

        packet.Send(toClient);
    }

    private static void ReceiveFullSync(BinaryReader reader)
    {
        if (!_TrackerStatus.IsEnabled)
        {
            return;
        }

        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        SessionTracker.Sessions.Clear();

        int count = reader.ReadInt32();
        DateTime now = DateTime.UtcNow;

        for (int i = 0; i < count; i++)
        {
            int playerIndex = reader.ReadInt32();
            long elapsedTicks = reader.ReadInt64();
            SessionTracker.Sessions[playerIndex] = now - new TimeSpan(elapsedTicks);
        }
    }
}