using Microsoft.Xna.Framework;
using PvPAdventure.Core.Debug;
using PvPAdventure.Core.Net;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.SpawnSelector.Net;

internal static class AdventurePortalNetHandler
{
    private enum PortalPacketType : byte
    {
        FullSync,
        Update,
        Clear
    }

    public static void HandlePacket(BinaryReader reader, int whoAmI)
    {
        PortalPacketType packetType = (PortalPacketType)reader.ReadByte();

        if (packetType == PortalPacketType.FullSync)
            ReceiveFullSync(reader);
        else if (packetType == PortalPacketType.Update)
            ReceiveUpdate(reader);
        else if (packetType == PortalPacketType.Clear)
            ReceiveClear(reader);
    }

    public static void SendFullSync(int toClient)
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        List<KeyValuePair<int, Vector2>> snapshot = AdventurePortalSystem.GetPortalSnapshot();
        ModPacket packet = GetPacket(PortalPacketType.FullSync);
        packet.Write(snapshot.Count);

        for (int i = 0; i < snapshot.Count; i++)
        {
            packet.Write(snapshot[i].Key);
            packet.Write(snapshot[i].Value.X);
            packet.Write(snapshot[i].Value.Y);
        }

        packet.Send(toClient);
        Log.Chat($"Adventure portal full sync sent, moreinfo: client={toClient} count={snapshot.Count}");
    }

    public static void SendUpdate(int playerIndex, Vector2 worldPosition)
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        ModPacket packet = GetPacket(PortalPacketType.Update);
        packet.Write(playerIndex);
        packet.Write(worldPosition.X);
        packet.Write(worldPosition.Y);
        packet.Send();

        Point tilePos = worldPosition.ToTileCoordinates();
        Log.Chat($"Adventure portal update sent, moreinfo: player={GetPlayerNameSafe(playerIndex)} tile=({tilePos.X},{tilePos.Y})");
    }

    public static void SendClear(int playerIndex)
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        ModPacket packet = GetPacket(PortalPacketType.Clear);
        packet.Write(playerIndex);
        packet.Send();

        Log.Chat($"Adventure portal clear sent, moreinfo: player={GetPlayerNameSafe(playerIndex)} slot={playerIndex}");
    }

    private static ModPacket GetPacket(PortalPacketType packetType)
    {
        ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.AdventurePortal);
        packet.Write((byte)packetType);
        return packet;
    }

    private static void ReceiveFullSync(BinaryReader reader)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        int count = reader.ReadInt32();
        List<KeyValuePair<int, Vector2>> snapshot = [];

        for (int i = 0; i < count; i++)
        {
            int playerIndex = reader.ReadInt32();
            float x = reader.ReadSingle();
            float y = reader.ReadSingle();
            snapshot.Add(new KeyValuePair<int, Vector2>(playerIndex, new Vector2(x, y)));
        }

        AdventurePortalSystem.ApplyFullSync(snapshot);
        Log.Chat($"Adventure portal full sync received, moreinfo: count={count}");
    }

    private static void ReceiveUpdate(BinaryReader reader)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        int playerIndex = reader.ReadInt32();
        float x = reader.ReadSingle();
        float y = reader.ReadSingle();
        Vector2 worldPosition = new(x, y);

        AdventurePortalSystem.ApplyUpdate(playerIndex, worldPosition);

        Point tilePos = worldPosition.ToTileCoordinates();
        Log.Chat($"Adventure portal update received, moreinfo: player={GetPlayerNameSafe(playerIndex)} tile=({tilePos.X},{tilePos.Y})");
    }

    private static void ReceiveClear(BinaryReader reader)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        int playerIndex = reader.ReadInt32();
        AdventurePortalSystem.ApplyClear(playerIndex);
        Log.Chat($"Adventure portal clear received, moreinfo: player={GetPlayerNameSafe(playerIndex)} slot={playerIndex}");
    }

    private static string GetPlayerNameSafe(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= Main.maxPlayers)
            return $"Player {playerIndex}";

        Player player = Main.player[playerIndex];
        if (player == null || string.IsNullOrWhiteSpace(player.name))
            return $"Player {playerIndex}";

        return player.name;
    }
}
