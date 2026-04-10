//using PvPAdventure.Core.Net;
//using System.IO;
//using Terraria;
//using Terraria.ID;
//using Terraria.ModLoader;

//namespace PvPAdventure.Common.Skins;

///// <summary>
///// Networking for weapon skin selections.
///// <list type="bullet">
/////   <item>Client → Server: "I selected skin X for item type Y"</item>
/////   <item>Server → All other clients: "Player Z has skin X for item type Y"</item>
///// </list>
///// </summary>
//internal static class SkinNetHandler
//{
//    private enum Msg : byte
//    {
//        /// <summary>Client tells the server which skin it selected.</summary>
//        SetSkin,

//        /// <summary>Server broadcasts a player's skin selection to other clients.</summary>
//        SyncSkin,
//    }

//    /// <summary>
//    /// Client sends a skin selection (or removal) to the server.
//    /// An empty <paramref name="skinId"/> means the skin was removed for that item type.
//    /// </summary>
//    public static void SendSkinSelection(int itemType, string skinId)
//    {
//        ModPacket p = ModContent.GetInstance<PvPAdventure>().GetPacket();
//        p.Write((byte)AdventurePacketIdentifier.WeaponSkin);
//        p.Write((byte)Msg.SetSkin);
//        p.Write(itemType);
//        p.Write(skinId ?? "");
//        p.Send();
//    }

//    /// <summary>
//    /// Entry point called by <see cref="PvPAdventure.HandlePacket"/> after
//    /// <see cref="AdventurePacketIdentifier.WeaponSkin"/> has been read.
//    /// </summary>
//    public static void HandlePacket(BinaryReader reader, int whoAmI)
//    {
//        Msg msg = (Msg)reader.ReadByte();

//        switch (msg)
//        {
//            case Msg.SetSkin:
//                HandleSetSkin(reader, whoAmI);
//                break;

//            case Msg.SyncSkin:
//                HandleSyncSkin(reader);
//                break;
//        }
//    }

//    /// <summary>
//    /// Server receives a skin selection from a client, validates it,
//    /// applies it to the server-side <see cref="SkinPlayer"/>,
//    /// and broadcasts to all other clients.
//    /// </summary>
//    private static void HandleSetSkin(BinaryReader reader, int whoAmI)
//    {
//        int itemType = reader.ReadInt32();
//        string skinId = reader.ReadString();

//        if (Main.netMode != NetmodeID.Server)
//            return;

//        if (whoAmI < 0 || whoAmI >= Main.maxPlayers)
//            return;

//        Player player = Main.player[whoAmI];

//        if (player == null || !player.active)
//            return;

//        // Validate the skin exists and maps to the claimed item type.
//        if (!string.IsNullOrEmpty(skinId))
//        {
//            if (!SkinIndex.TryGetById(skinId, out SkinDefinition def) || def.ItemType != itemType)
//            {
//                Log.Debug($"[SkinNet] Rejected invalid skin: player={whoAmI} itemType={itemType} skinId={skinId}");
//                return;
//            }
//        }

//        // Apply on the server's SkinPlayer instance.
//        player.GetModPlayer<SkinPlayer>().ApplyRemoteSkin(itemType, skinId);

//        // Broadcast to every other client.
//        ModPacket p = ModContent.GetInstance<PvPAdventure>().GetPacket();
//        p.Write((byte)AdventurePacketIdentifier.WeaponSkin);
//        p.Write((byte)Msg.SyncSkin);
//        p.Write((byte)whoAmI);
//        p.Write(itemType);
//        p.Write(skinId ?? "");
//        p.Send(-1, whoAmI);
//    }

//    /// <summary>
//    /// Client receives a broadcast that another player changed their skin.
//    /// </summary>
//    private static void HandleSyncSkin(BinaryReader reader)
//    {
//        byte playerId = reader.ReadByte();
//        int itemType = reader.ReadInt32();
//        string skinId = reader.ReadString();

//        if (Main.netMode != NetmodeID.MultiplayerClient)
//            return;

//        if (playerId >= Main.maxPlayers)
//            return;

//        Player player = Main.player[playerId];

//        if (player == null || !player.active)
//            return;

//        player.GetModPlayer<SkinPlayer>().ApplyRemoteSkin(itemType, skinId);
//    }
//}
