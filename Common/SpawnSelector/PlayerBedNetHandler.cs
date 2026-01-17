using PvPAdventure.Core.Net;
using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.SpawnSelector;

internal class PlayerBedNetHandler
{
    public static void HandlePacket(BinaryReader reader, int whoAmI)
    {
        byte playerId = reader.ReadByte();
        int spawnX = reader.ReadInt32();
        int spawnY = reader.ReadInt32();

        Player p = Main.player[playerId];
        p.SpawnX = spawnX;
        p.SpawnY = spawnY;

        if (Main.dedServ)
        {
            ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
            packet.Write((byte)AdventurePacketIdentifier.PlayerBed);
            packet.Write(playerId);
            packet.Write(spawnX);
            packet.Write(spawnY);
            packet.Send(-1, whoAmI);
        }
    }
}
