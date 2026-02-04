//using System.IO;
//using Terraria;
//using Terraria.ModLoader;

//namespace PvPAdventure.Common.MatchHistory.Recording;

//internal sealed class DemoCommand : ModCommand
//{
//    public override string Command => "demo";
//    public override string Description => "Demo tools: /demo play <file>, /demo stop, /demo dir";
//    public override CommandType Type => CommandType.Chat;

//    public override void Action(CommandCaller caller, string input, string[] args)
//    {
//        RecordingSystem demos = ModContent.GetInstance<RecordingSystem>();

//        if (args.Length == 0)
//        {
//            Main.NewText("Usage: /demo play <file> | /demo stop | /demo dir", Microsoft.Xna.Framework.Color.Orange);
//            return;
//        }

//        string sub = args[0].ToLowerInvariant();

//        if (sub == "stop")
//        {
//            demos.StopPlayback();
//            return;
//        }

//        if (sub == "dir")
//        {
//            string dir = Path.Combine(Main.SavePath, "PvPAdventure", "Replays");
//            Main.NewText(dir, Microsoft.Xna.Framework.Color.LightGray);
//            return;
//        }

//        if (sub == "play")
//        {
//            if (args.Length < 2)
//            {
//                Main.NewText("Usage: /demo play <file>", Microsoft.Xna.Framework.Color.Orange);
//                return;
//            }

//            string dir = Path.Combine(Main.SavePath, "PvPAdventure", "Replays");
//            string path = Path.Combine(dir, args[1]);

//            demos.BeginPlayback(path);
//            return;
//        }

//        Main.NewText("Unknown subcommand.", Microsoft.Xna.Framework.Color.OrangeRed);
//    }
//}
