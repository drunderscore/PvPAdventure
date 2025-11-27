//using Ionic.Zlib;
//using Microsoft.Xna.Framework.Input;
//using PvPAdventure.System;
//using Terraria;
//using Terraria.GameInput;
//using Terraria.ID;
//using Terraria.Localization;
//using Terraria.ModLoader;
//using Terraria.UI;

//namespace PvPAdventure.Core.Debug;

//#if DEBUG
//[Autoload(Side = ModSide.Client)]
//public class DebugKeybinds : ModSystem
//{
//    //public ModKeybind OpenControls { get; private set; }

//    public override void Load()
//    {
//        //OpenControls = KeybindLoader.RegisterKeybind(Mod, "[DEBUG] OpenControls", Keys.NumPad0);
//    }
//}

//public class DebugKeybindsPlayer : ModPlayer
//{
//    public override void ProcessTriggers(TriggersSet triggersSet)
//    {
//        var key = ModContent.GetInstance<DebugKeybinds>();

//        // NOTE: DOESN'T WORK BELOW

//        //// Start game via /dbstart
//        //if (key.StartGame.JustPressed)
//        //{
//        //    if (Main.netMode == NetmodeID.MultiplayerClient)
//        //    {
//        //        // Send a chat packet to the server as if we typed "/dbstart"
//        //        NetMessage.SendData(
//        //            MessageID.ChatText,
//        //            number: Player.whoAmI,
//        //            text: NetworkText.FromLiteral("/dbstart")
//        //        );
//        //    }
//        //    else
//        //    {
//        //        // Singleplayer: just call directly
//        //        ModContent.GetInstance<GameManager>().StartGame(60000, 0);
//        //    }
//        //}
//    }
//}

//#endif