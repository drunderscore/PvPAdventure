//using PvPAdventure.Core.Net;
//using System.IO;
//using Terraria;
//using Terraria.ID;
//using Terraria.ModLoader;

//namespace PvPAdventure.Common.Skins.Net;

///// <summary>
///// Network handler for player skin selections.
///// Uses <see cref="AdventurePacketIdentifier.Skins"/>.
/////
///// Flow:
///// 1) Client sends SetSkin(itemType, skinId) to server.
///// 2) Server validates, applies to SkinPlayer, broadcasts SyncSkin to all other clients.
///// 3) Clients receive SyncSkin and apply to the remote player's SkinPlayer.
/////
///// On player join, the server sends the full set of skins for all existing players
///// to the new client via SyncSkin messages (handled in SkinPlayer.OnEnterWorld →
///// the existing players' skins are already stored server-side in their SkinPlayer).
///// </summary>
//internal static class SkinNetHandler
//{
//    private enum Msg : byte
//    {
//        /// <summary>Client → Server: I selected this skin.</summary>
//        SetSkin,

//        /// <summary>Server → Client(s): Player X has this skin.</summary>
//        SyncSkin,

//        /// <summary>Server → Client: Full dump of a player's skins on join.</summary>
//        SyncAllSkins,
//    }

//    /// <summary>
//    /// Client sends a skin selection to the server.
//    /// Pass empty skinId to remove the skin for that item type.
//    /// </summary>
//    public static void SendSetSkin(int itemType, string skinId)
//    {
//        if (Main.netMode != NetmodeID.MultiplayerClient)
//            return;

//        Log.Debug($"[SkinNetHandler] SendSetSkin itemType={itemType} skinId='{skinId}'");

//        ModPacket p = ModContent.GetInstance<PvPAdventure>().GetPacket();
//        p.Write((byte)AdventurePacketIdentifier.Skins);
//        p.Write((byte)Msg.SetSkin);
//        p.Write(itemType);
//        p.Write(skinId ?? "");
//        p.Send();
//    }

//    /// <summary>
//    /// Called from PvPAdventure.HandlePacket when packet id is Skins.
//    /// </summary>
//    public static void HandlePacket(BinaryReader reader, int whoAmI)
//    {
//        Msg msg = (Msg)reader.ReadByte();
//        Log.Debug($"[SkinNetHandler] HandlePacket msg={msg} whoAmI={whoAmI} netMode={Main.netMode}");

//        switch (msg)
//        {
//            case Msg.SetSkin:
//                HandleSetSkin(reader, whoAmI);
//                break;

//            case Msg.SyncSkin:
//                HandleSyncSkin(reader);
//                break;

//            case Msg.SyncAllSkins:
//                HandleSyncAllSkins(reader);
//                break;

//            default:
//                Log.Warn($"[SkinNetHandler] Unknown sub-message: {msg}");
//                break;
//        }
//    }

//    /// <summary>
//    /// Server receives a skin selection from a client.
//    /// Validates, applies server-side, and broadcasts to all other clients.
//    /// </summary>
//    private static void HandleSetSkin(BinaryReader reader, int whoAmI)
//    {
//        int itemType = reader.ReadInt32();
//        string skinId = reader.ReadString();

//        Log.Debug($"[SkinNetHandler] HandleSetSkin from={whoAmI} itemType={itemType} skinId='{skinId}'");

//        if (Main.netMode != NetmodeID.Server)
//        {
//            Log.Debug("[SkinNetHandler] HandleSetSkin ignored — not server");
//            return;
//        }

//        // Validate player
//        if (whoAmI < 0 || whoAmI >= Main.maxPlayers)
//        {
//            Log.Debug($"[SkinNetHandler] HandleSetSkin rejected — invalid whoAmI={whoAmI}");
//            return;
//        }

//        Player player = Main.player[whoAmI];

//        if (player == null || !player.active)
//        {
//            Log.Debug($"[SkinNetHandler] HandleSetSkin rejected — player null or inactive");
//            return;
//        }

//        // Validate skin (empty = removal, which is always allowed)
//        if (!string.IsNullOrEmpty(skinId))
//        {
//            if (!SkinIndex.TryGetById(skinId, out SkinDefinition def) || def.ItemType != itemType)
//            {
//                Log.Debug($"[SkinNetHandler] HandleSetSkin rejected — invalid skin '{skinId}' for itemType={itemType}");
//                return;
//            }
//        }

//        // Apply server-side
//        player.GetModPlayer<SkinPlayer>().ApplyRemoteSkin(itemType, skinId);
//        Log.Debug($"[SkinNetHandler] HandleSetSkin applied server-side for player={whoAmI}");

//        // Broadcast to all other clients
//        BroadcastSyncSkin((byte)whoAmI, itemType, skinId, whoAmI);
//    }

//    /// <summary>
//    /// Client receives a single skin update for a remote player.
//    /// </summary>
//    private static void HandleSyncSkin(BinaryReader reader)
//    {
//        byte playerId = reader.ReadByte();
//        int itemType = reader.ReadInt32();
//        string skinId = reader.ReadString();

//        Log.Debug($"[SkinNetHandler] HandleSyncSkin playerId={playerId} itemType={itemType} skinId='{skinId}'");

//        if (Main.netMode != NetmodeID.MultiplayerClient)
//        {
//            Log.Debug("[SkinNetHandler] HandleSyncSkin ignored — not client");
//            return;
//        }

//        if (playerId >= Main.maxPlayers)
//        {
//            Log.Debug($"[SkinNetHandler] HandleSyncSkin rejected — invalid playerId={playerId}");
//            return;
//        }

//        Player player = Main.player[playerId];

//        if (player == null || !player.active)
//        {
//            Log.Debug($"[SkinNetHandler] HandleSyncSkin rejected — player null or inactive");
//            return;
//        }

//        // Don't overwrite our own local selections
//        if (playerId == Main.myPlayer)
//        {
//            Log.Debug("[SkinNetHandler] HandleSyncSkin skipping — that's us");
//            return;
//        }

//        player.GetModPlayer<SkinPlayer>().ApplyRemoteSkin(itemType, skinId);
//        Log.Debug($"[SkinNetHandler] HandleSyncSkin applied for remote player={playerId}");
//    }

//    /// <summary>
//    /// Client receives a full skin dump for a player.
//    /// Used when a player joins and the server sends existing players' skins.
//    /// </summary>
//    private static void HandleSyncAllSkins(BinaryReader reader)
//    {
//        byte playerId = reader.ReadByte();
//        int count = reader.ReadInt32();

//        Log.Debug($"[SkinNetHandler] HandleSyncAllSkins playerId={playerId} count={count}");

//        if (Main.netMode != NetmodeID.MultiplayerClient)
//        {
//            Log.Debug("[SkinNetHandler] HandleSyncAllSkins ignored — not client");
//            return;
//        }

//        if (playerId >= Main.maxPlayers)
//        {
//            Log.Debug($"[SkinNetHandler] HandleSyncAllSkins rejected — invalid playerId={playerId}");
//            return;
//        }

//        Player player = Main.player[playerId];

//        if (player == null || !player.active)
//        {
//            Log.Debug($"[SkinNetHandler] HandleSyncAllSkins rejected — player null or inactive");

//            // Still need to read the data to avoid corrupting the stream
//            for (int i = 0; i < count; i++)
//            {
//                reader.ReadInt32();
//                reader.ReadString();
//            }

//            return;
//        }

//        // Don't overwrite our own local selections
//        if (playerId == Main.myPlayer)
//        {
//            Log.Debug("[SkinNetHandler] HandleSyncAllSkins skipping — that's us");

//            for (int i = 0; i < count; i++)
//            {
//                reader.ReadInt32();
//                reader.ReadString();
//            }

//            return;
//        }

//        SkinPlayer skinPlayer = player.GetModPlayer<SkinPlayer>();

//        for (int i = 0; i < count; i++)
//        {
//            int itemType = reader.ReadInt32();
//            string skinId = reader.ReadString();
//            skinPlayer.ApplyRemoteSkin(itemType, skinId);
//            Log.Debug($"[SkinNetHandler] HandleSyncAllSkins applying itemType={itemType} skinId='{skinId}' for player={playerId}");
//        }
//    }

//    /// <summary>
//    /// Server broadcasts a single skin change to all clients except the excluded one.
//    /// </summary>
//    private static void BroadcastSyncSkin(byte playerId, int itemType, string skinId, int excludeClient)
//    {
//        Log.Debug($"[SkinNetHandler] BroadcastSyncSkin playerId={playerId} itemType={itemType} skinId='{skinId}' exclude={excludeClient}");

//        ModPacket p = ModContent.GetInstance<PvPAdventure>().GetPacket();
//        p.Write((byte)AdventurePacketIdentifier.Skins);
//        p.Write((byte)Msg.SyncSkin);
//        p.Write(playerId);
//        p.Write(itemType);
//        p.Write(skinId ?? "");
//        p.Send(-1, excludeClient);
//    }

//    /// <summary>
//    /// Server sends all skins of a given player to a specific client.
//    /// Called when a new client joins (to sync existing player skins).
//    /// </summary>
//    public static void SendAllSkins(int sourcePlayerId, int targetClient)
//    {
//        if (Main.netMode != NetmodeID.Server)
//            return;

//        Player source = Main.player[sourcePlayerId];

//        if (source == null || !source.active)
//            return;

//        SkinPlayer skinPlayer = source.GetModPlayer<SkinPlayer>();
//        var allSkins = skinPlayer.GetAllSkins();

//        if (allSkins.Count == 0)
//        {
//            Log.Debug($"[SkinNetHandler] SendAllSkins sourcePlayer={sourcePlayerId} → targetClient={targetClient} — no skins to send");
//            return;
//        }

//        Log.Debug($"[SkinNetHandler] SendAllSkins sourcePlayer={sourcePlayerId} → targetClient={targetClient} count={allSkins.Count}");

//        ModPacket p = ModContent.GetInstance<PvPAdventure>().GetPacket();
//        p.Write((byte)AdventurePacketIdentifier.Skins);
//        p.Write((byte)Msg.SyncAllSkins);
//        p.Write((byte)sourcePlayerId);
//        p.Write(allSkins.Count);

//        foreach (var (itemType, def) in allSkins)
//        {
//            p.Write(itemType);
//            p.Write(def.Id);
//        }

//        p.Send(targetClient);
//    }
//}
