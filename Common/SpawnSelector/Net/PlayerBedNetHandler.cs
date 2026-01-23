using PvPAdventure.Core.Net;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.SpawnSelector.Net;

internal class PlayerBedNetHandler
{
    public static void HandlePacket(BinaryReader reader, int whoAmI)
    {
        byte playerId = reader.ReadByte();
        int spawnX = reader.ReadInt32();
        int spawnY = reader.ReadInt32();

        if (playerId < 0 || playerId >= Main.maxPlayers)
            return;

        if (Main.netMode == NetmodeID.Server && playerId != whoAmI)
            return;

        Player p = Main.player[playerId];
        if (p == null)
            return;

        p.SpawnX = spawnX;
        p.SpawnY = spawnY;

        if (Main.netMode == NetmodeID.Server)
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
