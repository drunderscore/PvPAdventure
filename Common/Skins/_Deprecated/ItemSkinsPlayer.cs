//using PvPAdventure.Common.MainMenu.Profile;
//using PvPAdventure.Common.MainMenu.Shop;
//using System;
//using Terraria;
//using Terraria.ID;
//using Terraria.ModLoader;

//namespace PvPAdventure.Common.Skins.Net;

//internal sealed class ItemSkinsPlayer : ModPlayer
//{
//    private int[] slotSig = [];
//    private double _nextLogTime;

//    public override void Initialize()
//    {
//        slotSig = new int[Player.inventory.Length];
//        Array.Fill(slotSig, -1);
//    }

//    public override void PostUpdate()
//    {
//        if (Main.netMode == NetmodeID.Server)
//            return;
//        if (Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI != Main.myPlayer)
//            return;

//        bool canLog = Main.gameTimeCache.TotalGameTime.TotalSeconds >= _nextLogTime;

//        for (int i = 0; i < Player.inventory.Length; i++)
//        {
//            Item item = Player.inventory[i];

//            if (item == null || item.IsAir) { slotSig[i] = -1; continue; }
//            if (!ItemSkins.HasAnySkins(item.type)) continue;

//            byte desired = ItemSkins.NoSkin;
//            if (SkinPreference.TryGetSelected(item.type, out string skinId) &&
//                ProfileStorage.IsUnlocked(skinId) &&
//                ItemSkins.TryGetSkinIndex(item.type, skinId, out byte idx))
//                desired = idx;

//            int sig = (item.type << 8) ^ desired;
//            if (slotSig[i] == sig) continue;
//            slotSig[i] = sig;

//            if (canLog)
//            {
//                Log.Debug($"[SkinsPlayer] slot={i} type={item.type} desired={desired} netMode={Main.netMode}");
//                _nextLogTime = Main.gameTimeCache.TotalGameTime.TotalSeconds + 1.0;
//            }

//            if (Main.netMode == NetmodeID.MultiplayerClient)
//                ItemSkinsNet.SendRequestSetInventorySkin((byte)i, desired, item.type);
//        }
//    }
//}