//using System;
//using System.Diagnostics;
//using System.IO;
//using Terraria;
//using Terraria.ModLoader;

//namespace PvPAdventure.Core.Net;

//internal class LatencyTrackerPlayer : ModPlayer
//{
//    // Intentionally zero-initialize this so we get a ping/pong ASAP.
//    private int _nextPingPongTime;
//    private int _pingPongCanary;
//    private Stopwatch _pingPongStopwatch;
//    public TimeSpan? Latency { get; private set; }

//    private const int TimeBetweenPingPongs = 3 * 60;

//    public sealed class PingPong(int canary) : IPacket<PingPong>
//    {
//        public int Canary { get; set; } = canary;

//        public static PingPong Deserialize(BinaryReader reader)
//        {
//            return new(reader.ReadInt32());
//        }

//        public void Serialize(BinaryWriter writer)
//        {
//            writer.Write(Canary);
//        }
//    }

//    private void SendPingPong()
//    {
//        _pingPongStopwatch = Stopwatch.StartNew();

//        var packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
//        packet.Write((byte)AdventurePacketIdentifier.PingPong);
//        new PingPong(_pingPongCanary).Serialize(packet);
//        packet.Send(Player.whoAmI);
//    }

//    public void OnPingPongReceived(PingPong pingPong)
//    {
//        if (_pingPongStopwatch == null)
//            return;

//        if (pingPong.Canary != _pingPongCanary)
//            return;

//        _pingPongStopwatch.Stop();
//        Latency = _pingPongStopwatch.Elapsed / 2;
//        _pingPongStopwatch = null;
//        _pingPongCanary++;
//    }

//    public override void PreUpdate()
//    {
//        if (Main.dedServ && --_nextPingPongTime <= 0)
//        {
//            _nextPingPongTime = TimeBetweenPingPongs;
//            SendPingPong();
//        }
//    }
//}
