using Microsoft.Xna.Framework;
using PvPAdventure.Core.Net;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.SpawnSelector.Net;

public static class PlayerPortalNetHandler
{
    public static void Send(int playerId, bool hasPortal, Vector2 worldPos, int health, int createTicks, int toWho = -1, int ignoreClient = -1)
    {
        if (Main.netMode == NetmodeID.SinglePlayer)
            return;

        ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.PlayerPortal);
        packet.Write((byte)playerId);
        packet.Write(hasPortal);
        packet.Write(worldPos.X);
        packet.Write(worldPos.Y);
        packet.Write(health);
        packet.Write(createTicks);
        packet.Send(toWho, ignoreClient);
    }

    public static void HandlePacket(BinaryReader reader, int whoAmI)
    {
        byte playerId = reader.ReadByte();
        bool hasPortal = reader.ReadBoolean();
        Vector2 worldPos = new(reader.ReadSingle(), reader.ReadSingle());
        int health = reader.ReadInt32();
        int createTicks = reader.ReadInt32();

        if (playerId >= Main.maxPlayers)
            return;

        if (Main.netMode == NetmodeID.Server && playerId != whoAmI)
            return;

        Player player = Main.player[playerId];
        if (player == null || !player.active)
            return;

        player.GetModPlayer<SpawnPlayer>().ApplyPortalFromNet(hasPortal, worldPos, health, createTicks);

        if (Main.netMode == NetmodeID.Server)
            Send(playerId, hasPortal, worldPos, health, createTicks, toWho: -1, ignoreClient: whoAmI);
    }
}
