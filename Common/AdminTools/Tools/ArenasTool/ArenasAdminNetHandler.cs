using PvPAdventure.Common.Arenas;
using PvPAdventure.Core.Net;
using SubworldLibrary;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.AdminTools.Tools.ArenasTool;

internal static class ArenasAdminNetHandler
{
    public enum ArenasAdminPacketType : byte
    {
        SendAllToMainWorld = 0,
        SendAllToArenas = 1
    }

    public static void Request(ArenasAdminPacketType type)
    {
        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            Execute(type);
            return;
        }

        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        Log.Debug("Request: " + type);

        var packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.ArenasAdmin);
        packet.Write((byte)type);
        packet.Send();
    }

    public static void HandlePacket(BinaryReader reader, int fromWho)
    {
        var type = (ArenasAdminPacketType)reader.ReadByte();

        if (Main.netMode == NetmodeID.Server)
        {
            // TODO: add your admin permission check here (steamId list, operator flag, etc.)
            Log.Debug("Server received: " + type + " from " + fromWho);

            var packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
            packet.Write((byte)AdventurePacketIdentifier.ArenasAdmin);
            packet.Write((byte)type);
            packet.Send(toClient: -1, ignoreClient: -1);

            return;
        }

        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            Log.Debug("Client executing: " + type);
            Execute(type);
        }
    }

    private static void Execute(ArenasAdminPacketType type)
    {
        Main.QueueMainThreadAction(() =>
        {
            if (type == ArenasAdminPacketType.SendAllToArenas)
            {
                if (!SubworldSystem.AnyActive())
                    SubworldSystem.Enter<ArenasSubworld>();
                return;
            }

            if (type == ArenasAdminPacketType.SendAllToMainWorld)
            {
                if (SubworldSystem.AnyActive())
                    SubworldSystem.Exit();
                return;
            }
        });
    }
}
