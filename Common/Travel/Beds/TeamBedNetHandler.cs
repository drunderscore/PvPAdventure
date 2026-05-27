using Microsoft.Xna.Framework;
using PvPAdventure.Content.Portals;
using PvPAdventure.Core.Net;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Team = Terraria.Enums.Team;

namespace PvPAdventure.Common.Travel.Beds;

internal enum TeamBedPacketType : byte
{
    BedUpdate,
    PlayerSpawn,
    DestroyAttempt,
    BedDestroyFx,
}

public static class TeamBedNetHandler
{
    public static void HandlePacket(BinaryReader reader, int whoAmI)
    {
        TeamBedPacketType type = (TeamBedPacketType)reader.ReadByte();

        switch (type)
        {
            case TeamBedPacketType.BedUpdate:
                ReceiveBedUpdate(reader);
                break;

            case TeamBedPacketType.PlayerSpawn:
                ReceivePlayerSpawn(reader, whoAmI);
                break;

            case TeamBedPacketType.DestroyAttempt:
                ReceiveDestroyAttempt(reader, whoAmI);
                break;

            case TeamBedPacketType.BedDestroyFx:
                ReceiveBedDestructionFx(reader);
                break;
        }
    }

    public static void SendBedDestructionFx(float worldX, float worldY, bool killed)
    {
        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            PortalNPC.PlayPortalFx(new(worldX, worldY), killed);
            return;
        }

        if (Main.netMode != NetmodeID.Server)
            return;

        ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.TeamBed);
        packet.Write((byte)TeamBedPacketType.BedDestroyFx);
        packet.Write(worldX);
        packet.Write(worldY);
        packet.Write(killed);
        packet.Send();
    }

    private static void ReceiveBedDestructionFx(BinaryReader reader)
    {
        float worldX = reader.ReadSingle();
        float worldY = reader.ReadSingle();
        bool killed = reader.ReadBoolean();

        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        PortalNPC.PlayPortalFx(new(worldX, worldY), killed);
    }

    private static void ReceiveBedUpdate(BinaryReader reader)
    {
        Point origin = new(reader.ReadInt32(), reader.ReadInt32());
        Team team = (Team)reader.ReadByte();

        if (Main.netMode == NetmodeID.MultiplayerClient)
            ModContent.GetInstance<TeamBedSystem>().SetFromNet(origin, team);
    }

    private static void ReceivePlayerSpawn(BinaryReader reader, int whoAmI)
    {
        int playerId = reader.ReadByte();
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

    public static void SendDestroyAttempt(Point origin)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.TeamBed);
        packet.Write((byte)TeamBedPacketType.DestroyAttempt);
        packet.Write(origin.X);
        packet.Write(origin.Y);
        packet.Send();
    }

    private static void ReceiveDestroyAttempt(BinaryReader reader, int whoAmI)
    {
        Point origin = new(reader.ReadInt32(), reader.ReadInt32());

        if (Main.netMode != NetmodeID.Server)
            return;

        ModContent.GetInstance<TeamBedSystem>().SetCurrentBedTarget(whoAmI, origin);
    }
}
