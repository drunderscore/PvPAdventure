//using System.IO;
//using PvPAdventure.Common.Statistics;
//using Terraria;
//using Terraria.ID;
//using Terraria.ModLoader;

//namespace PvPAdventure.Core.Net;

//public static class PingPongNetHandler
//{
//    public static void HandlePacket(BinaryReader reader, int whoAmI)
//    {
//        var pingPong = LatencyTrackerPlayer.PingPong.Deserialize(reader);

//        if (Main.netMode == NetmodeID.Server)
//        {
//            var player = Main.player[whoAmI];
//            if (player == null || !player.active)
//            {
//                return;
//            }

//            player.GetModPlayer<LatencyTrackerPlayer>().OnPingPongReceived(pingPong);
//            return;
//        }

//        // Client echoes back
//        ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
//        packet.Write((byte)AdventurePacketIdentifier.PingPong);
//        pingPong.Serialize(packet);
//        packet.Send();
//    }
//}
