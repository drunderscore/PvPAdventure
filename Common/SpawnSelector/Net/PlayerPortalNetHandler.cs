using Microsoft.Xna.Framework;
using PvPAdventure.Core.Net;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.SpawnSelector.Net;

public static class PlayerPortalNetHandler
{
    private enum PortalAction : byte
    {
        FinishMirrorUse = 20,
        CreateResult = 21
    }

    public static void SendFinishMirrorUse(Player player, Vector2 worldPos, int elapsedTicks, int ticksLeft)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient || player?.active != true || player.whoAmI != Main.myPlayer)
            return;

        ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.PlayerPortal);
        packet.Write((byte)PortalAction.FinishMirrorUse);
        packet.Write((byte)player.whoAmI);
        packet.Write(worldPos.X);
        packet.Write(worldPos.Y);
        packet.Write(elapsedTicks);
        packet.Write(ticksLeft);
        packet.Send();
    }

    public static void HandlePacket(BinaryReader reader, int whoAmI)
    {
        long start = reader.BaseStream.Position;

        try
        {
            PortalAction action = (PortalAction)reader.ReadByte();

            switch (action)
            {
                case PortalAction.FinishMirrorUse:
                    ReceiveFinishMirrorUse(reader, whoAmI);
                    break;

                case PortalAction.CreateResult:
                    ReceiveCreateResult(reader);
                    break;

                default:
                    Log.Warn($"[Portal] Unknown portal packet action={(byte)action}, bytesLeft={BytesLeft(reader)}");
                    break;
            }
        }
        catch (Exception e)
        {
            Log.Warn($"[Portal] Failed reading portal packet: {e.Message}, read={reader.BaseStream.Position - start}, bytesLeft={BytesLeft(reader)}");
        }
        finally
        {
            Drain(reader);
        }
    }

    private static void ReceiveFinishMirrorUse(BinaryReader reader, int whoAmI)
    {
        const int requiredBytes = 1 + 4 + 4 + 4 + 4;

        if (BytesLeft(reader) < requiredBytes)
        {
            Log.Warn($"[Portal] Ignored malformed/old FinishMirrorUse packet: bytesLeft={BytesLeft(reader)}, required={requiredBytes}");
            return;
        }

        byte playerId = reader.ReadByte();
        float x = reader.ReadSingle();
        float y = reader.ReadSingle();
        int elapsedTicks = reader.ReadInt32();
        int ticksLeft = reader.ReadInt32();

        if (Main.netMode != NetmodeID.Server)
            return;

        if (playerId != whoAmI || playerId >= Main.maxPlayers || Main.player[playerId] is not { active: true } player)
            return;

        Vector2 requestedWorldPos = new(x, y);
        Vector2 serverWorldPos = player.Bottom;
        float driftTiles = Vector2.Distance(requestedWorldPos, serverWorldPos) / 16f;

        Log.Chat($"[Mirror] Finish request received: player={player.name}, elapsedTicks={elapsedTicks}, ticksLeft={ticksLeft}, requested={requestedWorldPos}, server={serverWorldPos}, drift={driftTiles:0.0}");

        bool success = PortalSystem.TryCreatePortalAtPosition(player, serverWorldPos, out string reason);
        SendCreateResult(playerId, success, $"player={player.name}, elapsedTicks={elapsedTicks}, ticksLeft={ticksLeft}, reason={reason}");
    }

    private static void ReceiveCreateResult(BinaryReader reader)
    {
        if (Main.netMode == NetmodeID.Server)
            return;

        bool resultSuccess = reader.ReadBoolean();
        string message = reader.ReadString();

        Log.Chat(resultSuccess ? $"Successfully created portal: {message}" : $"Failed to create portal: {message}");
    }

    private static void SendCreateResult(int playerId, bool success, string message)
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.PlayerPortal);
        packet.Write((byte)PortalAction.CreateResult);
        packet.Write(success);
        packet.Write(message ?? string.Empty);
        packet.Send(playerId);
    }

    private static long BytesLeft(BinaryReader reader)
    {
        return reader.BaseStream.Length - reader.BaseStream.Position;
    }

    private static void Drain(BinaryReader reader)
    {
        if (reader.BaseStream.Position < reader.BaseStream.Length)
            reader.BaseStream.Position = reader.BaseStream.Length;
    }
}