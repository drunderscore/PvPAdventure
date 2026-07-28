using Microsoft.Xna.Framework;
using PvPAdventure.Core.Net;
using PvPAdventure.Core.Utilities;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Combat.EJ;

internal static class HellhexNetHandler
{
    private enum HellhexPacketType : byte
    {
        Applied,
        ExplosionRequest
    }

    public static void HandlePacket(BinaryReader reader, int whoAmI)
    {
        HellhexPacketType type = (HellhexPacketType)reader.ReadByte();

        switch (type)
        {
            case HellhexPacketType.Applied:
                ReceiveApplied(reader);
                break;

            case HellhexPacketType.ExplosionRequest:
                ReceiveExplosionRequest(reader, whoAmI);
                break;

            default:
                Log.Warn($"[Hellhex] Unknown packet type={(byte)type}");
                break;
        }
    }

    public static void SendApplied(int playerId, int applierId)
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.Hellhex);
        packet.Write((byte)HellhexPacketType.Applied);
        packet.Write((byte)playerId);
        packet.Write((short)applierId);
        packet.Send();
    }

    private static void ReceiveApplied(BinaryReader reader)
    {
        int playerId = reader.ReadByte();
        int applierId = reader.ReadInt16();

        if (Main.netMode != NetmodeID.MultiplayerClient ||
            !TryGetActivePlayer(playerId, out Player player) ||
            applierId < -1 || applierId >= Main.maxPlayers)
            return;

        player.GetModPlayer<HellhexPlayer>().SetApplierFromNetwork(applierId);
    }

    public static void SendExplosionRequest(int playerId, Vector2 position, int damage, float scale, int owner)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.Hellhex);
        packet.Write((byte)HellhexPacketType.ExplosionRequest);
        packet.Write((byte)playerId);
        packet.Write(position.X);
        packet.Write(position.Y);
        packet.Write(damage);
        packet.Write(scale);
        packet.Write((short)owner);
        packet.Send();
    }

    private static void ReceiveExplosionRequest(BinaryReader reader, int whoAmI)
    {
        int playerId = reader.ReadByte();
        Vector2 position = new(reader.ReadSingle(), reader.ReadSingle());
        int damage = reader.ReadInt32();
        float scale = reader.ReadSingle();
        int owner = reader.ReadInt16();

        if (Main.netMode != NetmodeID.Server ||
            whoAmI < 0 || whoAmI >= Main.maxPlayers ||
            !TryGetActivePlayer(whoAmI, out Player sender) ||
            !TryGetActivePlayer(playerId, out Player target) ||
            playerId == whoAmI ||
            !sender.hostile || !target.hostile ||
            sender.team != 0 && sender.team == target.team ||
            owner < -1 || owner >= Main.maxPlayers ||
            !float.IsFinite(position.X) || !float.IsFinite(position.Y) ||
            !float.IsFinite(scale) || scale <= 0f ||
            damage <= 0 ||
            Vector2.DistanceSquared(position, target.Center) > 96f * 96f)
            return;

        HellhexPlayer hellhex = target.GetModPlayer<HellhexPlayer>();
        if (hellhex.applierIndex != owner)
            return;

        hellhex.SpawnExplosionFromNetwork(
            position,
            Math.Clamp(damage, 1, 100_000),
            Math.Clamp(scale, 0.1f, 10f),
            owner);
    }

    private static bool TryGetActivePlayer(int playerId, out Player player)
    {
        player = null;

        if (playerId < 0 || playerId >= Main.maxPlayers ||
            Main.player[playerId] is not { active: true } foundPlayer)
            return false;

        player = foundPlayer;
        return true;
    }
}
