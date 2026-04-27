using Microsoft.Xna.Framework;
using PvPAdventure.Common.Chat;
using PvPAdventure.Common.Spectator.UI.Tabs.Players;
using PvPAdventure.Core.Net;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.SpawnSelector.Net;

public static class PlayerPortalNetHandler
{
    public static void Send(int playerId, bool hasPortal, Vector2 worldPos, int health, int createTicks, int maxHealth, int toWho = -1, int ignoreClient = -1)
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
        packet.Write(maxHealth);
        packet.Send(toWho, ignoreClient);
    }

    public static void HandlePacket(BinaryReader reader, int whoAmI)
    {
        byte playerId = reader.ReadByte();
        bool hasPortal = reader.ReadBoolean();
        Vector2 worldPos = new(reader.ReadSingle(), reader.ReadSingle());
        int health = reader.ReadInt32();
        int createTicks = reader.ReadInt32();
        int maxHealth = reader.ReadInt32();

        if (playerId >= Main.maxPlayers ||
            Main.netMode == NetmodeID.Server && playerId != whoAmI ||
            Main.player[playerId] is not { active: true } player)
        {
            return;
        }

        bool hadPortal = SpawnPlayer.TryGetPortalWorldPos(player, out Vector2 oldWorldPos);
        bool createdOrMovedPortalOnServer = Main.netMode == NetmodeID.Server && hasPortal && (!hadPortal || oldWorldPos != worldPos);

        if (Main.netMode == NetmodeID.Server && hasPortal)
        {
            maxHealth = PortalSystem.PortalMaxHealth;
            health = Math.Clamp(health, 1, maxHealth);
            createTicks = Math.Clamp(createTicks, 0, PortalSystem.PortalCreateAnimationTicks);
        }

        SpawnPlayer spawnPlayer = player.GetModPlayer<SpawnPlayer>();
        spawnPlayer.ApplyPortalFromNet(hasPortal, worldPos, health, createTicks, maxHealth);

        if (Main.netMode == NetmodeID.Server)
        {
            if (createdOrMovedPortalOnServer)
                SendPortalCreatedMessage(player, worldPos);

            Send(playerId, hasPortal, worldPos, health, createTicks, maxHealth, toWho: -1, ignoreClient: whoAmI);
        }
    }

    private static void SendPortalCreatedMessage(Player player, Vector2 worldPos)
    {
        string biome = PlayerStats.GetBiomeText(player);
        int distance = (int)(Vector2.Distance(player.Center, worldPos) / 16f);

        TeleportChat.SendSystemTeamMessage(
            player,
            PortalSystem.GetPortalMessage(player, biome, distance),
            Main.teamColor[Math.Clamp(player.team, 0, Main.teamColor.Length - 1)],
            PortalSystem.GetOwnPortalMessage(player, biome, distance));
    }
}
