using PvPAdventure.Common.Spectator.SpectatorMode;
using PvPAdventure.Core.Net;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Spectator.Net;

internal static class SpectatorModeNetHandler
{
    private enum SpectatorPacket : byte
    {
        SetMode, // Client asks server to change one player’s mode
        SyncModes // Server sends the complete player mode list
    }

    public static void Receive(BinaryReader reader, int sender)
    {
        SpectatorPacket packet = (SpectatorPacket)reader.ReadByte();

        if (packet == SpectatorPacket.SetMode)
            ReceiveSetMode(reader, sender);

        if (packet == SpectatorPacket.SyncModes)
            ReceiveSyncModes(reader);
    }

    public static void SendRequestSetMode(int slot, PlayerMode mode)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        ModPacket packet = CreatePacket(SpectatorPacket.SetMode);
        packet.Write(slot);
        packet.Write((byte)mode);
        packet.Send();
    }

    public static void SendSyncModes(int toClient = -1)
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        ModPacket packet = CreatePacket(SpectatorPacket.SyncModes);
        packet.Write(SpectatorModeSystem.Modes.Count);

        foreach ((int slot, PlayerMode mode) in SpectatorModeSystem.Modes)
        {
            packet.Write(slot);
            packet.Write((byte)mode);
        }

        packet.Send(toClient);
    }

    private static void ReceiveSetMode(BinaryReader reader, int sender)
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        int slot = reader.ReadInt32();
        PlayerMode mode = (PlayerMode)reader.ReadByte();

        if (slot < 0 || slot >= Main.maxPlayers || !Main.player[slot].active)
            return;

        if (slot != sender)
            return;

        SpectatorModeSystem.SetModeServer(slot, mode);
    }

    private static void ReceiveSyncModes(BinaryReader reader)
    {
        if (Main.netMode == NetmodeID.Server)
            return;

        SpectatorModeSystem.Modes.Clear();

        int count = reader.ReadInt32();

        for (int i = 0; i < count; i++)
        {
            int slot = reader.ReadInt32();
            PlayerMode mode = (PlayerMode)reader.ReadByte();

            if (slot >= 0 && slot < Main.maxPlayers)
                SpectatorModeSystem.SetModeLocal(slot, mode);
        }
    }

    private static ModPacket CreatePacket(SpectatorPacket packetType)
    {
        ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.Spectator);
        packet.Write((byte)packetType);
        return packet;
    }

}