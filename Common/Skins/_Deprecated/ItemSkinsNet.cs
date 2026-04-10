//using PvPAdventure.Core.Net;
//using System.IO;
//using Terraria;
//using Terraria.ModLoader;

//namespace PvPAdventure.Common.Skins.Net;

//internal static class ItemSkinsNet
//{
//    private enum Msg : byte
//    {
//        RequestSetInventorySkin,
//        SyncInventorySkin
//    }

//    public static void SendRequestSetInventorySkin(byte slot, byte skinIndex, int itemType)
//    {
//        ModPacket p = ModContent.GetInstance<PvPAdventure>().GetPacket();
//        p.Write((byte)AdventurePacketIdentifier.WeaponSkin);
//        p.Write((byte)Msg.RequestSetInventorySkin);
//        p.Write(slot);
//        p.Write(skinIndex);
//        p.Write(itemType);
//        p.Send();
//    }

//    public static void Receive(BinaryReader reader, int whoAmI)
//    {
//        Msg msg = (Msg)reader.ReadByte();

//        switch (msg)
//        {
//            case Msg.RequestSetInventorySkin:
//                ReceiveRequestSetInventorySkin(reader, whoAmI);
//                break;

//            case Msg.SyncInventorySkin:
//                ReceiveSyncInventorySkin(reader);
//                break;
//        }
//    }

//    private static void ReceiveRequestSetInventorySkin(BinaryReader reader, int whoAmI)
//    {
//        if (Main.netMode != Terraria.ID.NetmodeID.Server)
//            return;

//        byte slot = reader.ReadByte();
//        byte skinIndex = reader.ReadByte();
//        int itemType = reader.ReadInt32();

//        Log.Debug($"[SkinNet] Server received: whoAmI={whoAmI} slot={slot} skinIndex={skinIndex} itemType={itemType}");

//        if (whoAmI < 0 || whoAmI >= Main.maxPlayers)
//        { Log.Debug($"[SkinNet] REJECTED: invalid whoAmI={whoAmI}"); return; }

//        Player player = Main.player[whoAmI];
//        if (player == null || !player.active)
//        { Log.Debug($"[SkinNet] REJECTED: player null or inactive"); return; }

//        if (slot >= player.inventory.Length)
//        { Log.Debug($"[SkinNet] REJECTED: slot={slot} >= inventory.Length={player.inventory.Length}"); return; }

//        Item item = player.inventory[slot];
//        if (item == null || item.IsAir)
//        { Log.Debug($"[SkinNet] REJECTED: item null or air at slot={slot}"); return; }

//        if (item.type != itemType)
//        { Log.Debug($"[SkinNet] REJECTED: item.type={item.type} != itemType={itemType}"); return; }

//        if (!ItemSkins.HasAnySkins(item.type))
//        { Log.Debug($"[SkinNet] REJECTED: no skins for item.type={item.type}"); return; }

//        var g = item.GetGlobalItem<ItemSkinsGlobalItem>();
//        g.SkinIndex = skinIndex;
//        Log.Debug($"[SkinNet] Applied skinIndex={skinIndex} to slot={slot}");

//        ModPacket p = ModContent.GetInstance<PvPAdventure>().GetPacket();
//        p.Write((byte)AdventurePacketIdentifier.WeaponSkin);
//        p.Write((byte)Msg.SyncInventorySkin);
//        p.Write((byte)whoAmI);
//        p.Write(slot);
//        p.Write(skinIndex);
//        p.Send(-1, -1);
//        Log.Debug($"[SkinNet] Broadcast sync to all clients");
//    }

//    private static void ReceiveSyncInventorySkin(BinaryReader reader)
//    {
//        if (Main.netMode != Terraria.ID.NetmodeID.MultiplayerClient)
//            return;

//        byte playerId = reader.ReadByte();
//        byte slot = reader.ReadByte();
//        byte skinIndex = reader.ReadByte();

//        if (playerId >= Main.maxPlayers)
//            return;

//        Player player = Main.player[playerId];
//        if (player == null || !player.active)
//            return;

//        if (slot >= player.inventory.Length)
//            return;

//        Item item = player.inventory[slot];
//        if (item == null || item.IsAir)
//            return;

//        if (!ItemSkins.HasAnySkins(item.type))
//            return;

//        item.GetGlobalItem<ItemSkinsGlobalItem>().SkinIndex = skinIndex;
//    }
//}