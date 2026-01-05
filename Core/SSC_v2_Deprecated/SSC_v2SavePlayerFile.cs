//using Terraria;
//using Terraria.ID;
//using Terraria.IO;
//using Terraria.ModLoader;

//namespace PvPAdventure.Core.SSC_v2;

//public class SSC_v2SavePlayerFile: ModSystem
//{
//    public override void Load()
//    {
//        // Client-side: intercept local player save, upload to server instead.
//        On_Player.InternalSavePlayerFile += OnInternalSavePlayerFile;
//    }

//    public override void Unload()
//    {
//        On_Player.InternalSavePlayerFile -= OnInternalSavePlayerFile;
//    }
//    private static void OnInternalSavePlayerFile(On_Player.orig_InternalSavePlayerFile orig, PlayerFileData playerFile)
//    {
//        // Only hijack saves on multiplayer clients (local singleplayer should work normally).
//        if (Main.netMode == NetmodeID.MultiplayerClient && Main.gameMenu == false)
//        {
//            // Log
//            Log.Chat("Intercepted local player save for upload to server: " + playerFile.Name);

//            // Upload to server and skip local disk save.
//            // (If you want BOTH local+server, call orig(playerFile) after sending.)
//            //SSC_v2.SendSaveToServer(first: false);
//            //return;
//        }

//        orig(playerFile);
//    }

//    public override void PreSaveAndQuit()
//    {
//        Player.SavePlayer(Main.ActivePlayerFileData);
//        //WorldGen.SaveAndQuit();
//        base.PreSaveAndQuit();
//    }
//}
