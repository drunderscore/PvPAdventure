//using Terraria;
//using Terraria.ID;
//using Terraria.ModLoader;

//namespace PvPAdventure.Core.SSC_v2;

//public sealed class SSC_v2Player : ModPlayer
//{
//    private bool _sentJoin;

//    public override void OnEnterWorld()
//    {
//        if (Main.netMode != NetmodeID.MultiplayerClient)
//            return;

//        Log.Chat("Player entered world at time: " + Main.GameUpdateCount);

//        //SSC_v3.SendJoinRequestOnce();
//    }
//}