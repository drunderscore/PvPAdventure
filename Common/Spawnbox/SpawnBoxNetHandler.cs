using PvPAdventure.Core.Compat;
using PvPAdventure.Core.Net;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Spawnbox;

public static class SpawnBoxNetHandler
{
    private enum PacketType : byte
    {
        Set,
        Sync
    }

    public static void HandlePacket(BinaryReader reader, int whoAmI)
    {
        PacketType type = (PacketType)reader.ReadByte();
        SpawnBoxSettings settings = SpawnBoxSettings.Read(reader);

        if (type == PacketType.Sync)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                ModContent.GetInstance<SpawnBoxSystem>().ReceiveSync(settings);

            return;
        }

        if (Main.netMode != NetmodeID.Server || !ErkySSCCompat.IsAdmin(whoAmI))
            return;

        ModContent.GetInstance<SpawnBoxSystem>().SetFromTool(settings, sync: true);
    }

    public static void SendSet(SpawnBoxSettings settings)
    {
        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            ModContent.GetInstance<SpawnBoxSystem>().SetFromTool(settings);
            return;
        }

        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        Write(PacketType.Set, settings).Send();
    }

    internal static void SendSync(SpawnBoxSettings settings) => Write(PacketType.Sync, settings).Send();

    private static ModPacket Write(PacketType type, SpawnBoxSettings settings)
    {
        ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.SpawnBox);
        packet.Write((byte)type);
        settings.Clamped().Write(packet);
        return packet;
    }
}
