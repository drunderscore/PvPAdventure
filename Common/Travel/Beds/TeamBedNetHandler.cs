using Microsoft.Xna.Framework;
using PvPAdventure.Core.Net;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Team = Terraria.Enums.Team;

namespace PvPAdventure.Common.Travel.Beds;

public static class TeamBedNetHandler
{
    public static void HandlePacket(BinaryReader reader, int whoAmI)
    {
        byte first = reader.ReadByte();

        if (first == 255)
        {
            Point origin = new(reader.ReadInt32(), reader.ReadInt32());
            Team team = (Team)reader.ReadByte();

            if (Main.netMode == NetmodeID.MultiplayerClient)
                ModContent.GetInstance<TeamBedSystem>().SetFromNet(origin, team);

            return;
        }

        int playerId = first;
        int spawnX = reader.ReadInt32();
        int spawnY = reader.ReadInt32();

        if (playerId < 0 || playerId >= Main.maxPlayers || Main.player[playerId] is not { active: true } player)
            return;

        if (Main.netMode == NetmodeID.Server && playerId != whoAmI)
            return;

        player.SpawnX = spawnX;
        player.SpawnY = spawnY;

        if (Main.netMode != NetmodeID.Server)
            return;

        TeamBedSystem.SendPlayerSpawn(playerId, spawnX, spawnY, ignoreClient: whoAmI);
        ModContent.GetInstance<TeamBedSystem>().UpdateFromPlayer(player);
    }
}