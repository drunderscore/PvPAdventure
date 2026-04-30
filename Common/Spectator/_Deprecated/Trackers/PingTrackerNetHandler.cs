//using PvPAdventure.Core.Net;
//using System.IO;
//using Terraria;
//using Terraria.ID;
//using Terraria.ModLoader;

//namespace PvPAdventure.Common.Spectator.Trackers;

//internal static class PingTrackerNetHandler
//{
//    private enum PingTrackerOperation : byte
//    {
//        PingRequest,
//        PingResponse,
//        PingValue,
//        FullSync
//    }

//    public static void HandlePacket(BinaryReader reader, int sender)
//    {
//        PingTrackerOperation operation = (PingTrackerOperation)reader.ReadByte();

//        switch (operation)
//        {
//            case PingTrackerOperation.PingRequest:
//                ReceivePingRequest(reader, sender);
//                break;

//            case PingTrackerOperation.PingResponse:
//                ReceivePingResponse(reader);
//                break;

//            case PingTrackerOperation.PingValue:
//                ReceivePingValue(reader, sender);
//                break;

//            case PingTrackerOperation.FullSync:
//                ReceiveFullSync(reader);
//                break;
//        }
//    }

//    private static ModPacket GetPacket(PingTrackerOperation operation)
//    {
//        ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
//        packet.Write((byte)AdventurePacketIdentifier.PingPong);
//        packet.Write((byte)operation);
//        return packet;
//    }

//    public static void SendPingRequest(long pingId, long sentTicks)
//    {
//        if (Main.netMode != NetmodeID.MultiplayerClient)
//            return;

//        ModPacket packet = GetPacket(PingTrackerOperation.PingRequest);
//        packet.Write(pingId);
//        packet.Write(sentTicks);
//        packet.Send();
//    }

//    private static void ReceivePingRequest(BinaryReader reader, int sender)
//    {
//        if (!_TrackerStatus.IsEnabled)
//            return;

//        if (Main.netMode != NetmodeID.Server)
//            return;

//        long pingId = reader.ReadInt64();
//        long sentTicks = reader.ReadInt64();

//        ModPacket packet = GetPacket(PingTrackerOperation.PingResponse);
//        packet.Write(pingId);
//        packet.Write(sentTicks);
//        packet.Send(toClient: sender);
//    }

//    private static void ReceivePingResponse(BinaryReader reader)
//    {
//        if (!_TrackerStatus.IsEnabled)
//            return;

//        if (Main.netMode != NetmodeID.MultiplayerClient)
//            return;

//        long pingId = reader.ReadInt64();
//        long sentTicks = reader.ReadInt64();
//        PingTracker.ReceivePingResponse(pingId, sentTicks);
//    }

//    public static void SendPingValue(int playerIndex, int pingMs)
//    {
//        if (!_TrackerStatus.IsEnabled)
//            return;

//        if (Main.netMode != NetmodeID.MultiplayerClient)
//            return;

//        ModPacket packet = GetPacket(PingTrackerOperation.PingValue);
//        packet.Write(playerIndex);
//        packet.Write(pingMs);
//        packet.Send();
//    }

//    private static void ReceivePingValue(BinaryReader reader, int sender)
//    {
//        if (!_TrackerStatus.IsEnabled)
//            return;

//        if (Main.netMode != NetmodeID.Server)
//            return;

//        int playerIndex = reader.ReadInt32();
//        int pingMs = reader.ReadInt32();

//        PingTracker.SetPing(playerIndex, pingMs);
//        SendFullSync();
//    }

//    public static void SendFullSync(int toClient = -1)
//    {
//        if (!_TrackerStatus.IsEnabled)
//            return;

//        if (Main.netMode != NetmodeID.Server)
//            return;

//        ModPacket packet = GetPacket(PingTrackerOperation.FullSync);
//        packet.Write(PingTracker.Pings.Count);

//        foreach ((int playerIndex, int pingMs) in PingTracker.Pings)
//        {
//            packet.Write(playerIndex);
//            packet.Write(pingMs);
//        }

//        packet.Send(toClient);
//    }

//    private static void ReceiveFullSync(BinaryReader reader)
//    {
//        if (!_TrackerStatus.IsEnabled)
//            return;

//        if (Main.netMode != NetmodeID.MultiplayerClient)
//            return;

//        PingTracker.Pings.Clear();

//        int count = reader.ReadInt32();
//        for (int i = 0; i < count; i++)
//        {
//            int playerIndex = reader.ReadInt32();
//            int pingMs = reader.ReadInt32();
//            PingTracker.Pings[playerIndex] = pingMs;
//        }
//    }
//}