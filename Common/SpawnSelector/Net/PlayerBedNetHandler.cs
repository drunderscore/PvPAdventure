using Microsoft.Xna.Framework;
using PvPAdventure.Common.Teams;
using PvPAdventure.Core.Net;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Team = Terraria.Enums.Team;

namespace PvPAdventure.Common.SpawnSelector.Net;

internal sealed class PlayerBedNetHandler
{
    public static void HandlePacket(BinaryReader reader, int whoAmI)
    {
        byte first = reader.ReadByte();

        // Server -> clients: bed team update
        if (first == 255)
        {
            int bedX = reader.ReadInt32();
            int bedY = reader.ReadInt32();
            Team team = (Team)reader.ReadByte();

            if (Main.netMode == NetmodeID.MultiplayerClient)
                ModContent.GetInstance<TeamBedSystem>().SetFromNet(new Point(bedX, bedY), team);

            return;
        }

        // Normal spawn update
        byte playerId = first;
        int spawnX = reader.ReadInt32();
        int spawnY = reader.ReadInt32();

        if (playerId >= Main.maxPlayers ||
            Main.netMode == NetmodeID.Server && playerId != whoAmI ||
            Main.player[playerId] is not { } p)
        {
            return;
        }

        p.SpawnX = spawnX;
        p.SpawnY = spawnY;
        SpawnPlayer.InvalidateSpawnRegionCaches();

        if (Main.netMode != NetmodeID.Server)
            return;

        // broadcast spawn to others
        ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.PlayerBed);
        packet.Write(playerId);
        packet.Write(spawnX);
        packet.Write(spawnY);
        packet.Send(-1, whoAmI);

        // server authoritative: assign the bed multitile to current team
        ModContent.GetInstance<TeamBedSystem>().UpdateFromPlayer(p);
    }
}
