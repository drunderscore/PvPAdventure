//using Terraria;
//using Terraria.ModLoader;

//namespace PvPAdventure.Core.Net;

//public class PingCommand : ModCommand
//{
//    public override void Action(CommandCaller caller, string input, string[] args)
//    {
//        foreach (var player in Main.ActivePlayers)
//        {
//            var ping = player.GetModPlayer<LatencyTrackerPlayer>().Latency;
//            if (ping != null)
//                caller.Reply($"{player.name}: {ping.Value.TotalMilliseconds}ms");
//        }
//    }

//    public override string Command => "ping";
//    public override CommandType Type => CommandType.Console;
//}