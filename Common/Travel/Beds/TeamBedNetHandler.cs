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
            int bedX = reader.ReadInt32();
            int bedY = reader.ReadInt32();
            Team team = (Team)reader.ReadByte();

            //Log.Chat($"[TeamBed] Received bed packet origin=({bedX},{bedY}) team={team}");

            if (Main.netMode == NetmodeID.MultiplayerClient)
                ModContent.GetInstance<TeamBedSystem>().SetFromNet(new Point(bedX, bedY), team);

            return;
        }

        byte playerId = first;
        int spawnX = reader.ReadInt32();
        int spawnY = reader.ReadInt32();

        if (playerId >= Main.maxPlayers || Main.player[playerId] is not { active: true } player)
        {
            //Log.Chat($"[TeamBed] Invalid spawn packet player={playerId} spawn=({spawnX},{spawnY})");
            return;
        }

        if (Main.netMode == NetmodeID.Server && playerId != whoAmI)
        {
            //Log.Chat($"[TeamBed] Rejected spoofed spawn packet whoAmI={whoAmI} playerId={playerId}");
            return;
        }

        player.SpawnX = spawnX;
        player.SpawnY = spawnY;

        Log.Chat($"Applied bed for {player.name}, spawn=({spawnX},{spawnY}), team={(Terraria.Enums.Team)player.team}");

        if (Main.netMode != NetmodeID.Server)
            return;

        ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.TeamBed);
        packet.Write(playerId);
        packet.Write(spawnX);
        packet.Write(spawnY);
        packet.Send(ignoreClient: whoAmI);

        ModContent.GetInstance<TeamBedSystem>().UpdateFromPlayer(player);
    }
}