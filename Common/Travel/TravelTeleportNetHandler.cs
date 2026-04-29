using Microsoft.Xna.Framework;
using PvPAdventure.Common.Chat;
using PvPAdventure.Common.Travel.Portals;
using PvPAdventure.Content.Portals;
using PvPAdventure.Core.Config;
using PvPAdventure.Core.Net;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Travel;

public static class TravelTeleportNetHandler
{
    private enum TravelTeleportPacketType : byte
    {
        Teleport,
        TeleportSound
    }

    public static void HandlePacket(BinaryReader reader, int whoAmI)
    {
        TravelTeleportPacketType type = (TravelTeleportPacketType)reader.ReadByte();

        switch (type)
        {
            case TravelTeleportPacketType.Teleport:
                ReceiveTeleport(reader, whoAmI);
                break;

            case TravelTeleportPacketType.TeleportSound:
                ReceiveTeleportSound(reader);
                break;

            default:
                Log.Warn($"[TravelTeleport] Unknown packet type={(byte)type}");
                break;
        }
    }

    public static void SendTeleportRequest(TravelTarget target)
    {
        //Log.Chat($"[TravelTeleport] Send request type={target.Type} targetPlayer={target.PlayerIndex} pos={target.WorldPosition} available={target.Available}");

        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            if (TravelTeleportSystem.TryTeleport(Main.LocalPlayer, target, out string reason))
            {
                PlayTeleportSound(Main.LocalPlayer.Center);
                TeleportChat.Announce(Main.LocalPlayer, target.Type, target.PlayerIndex);
            }
            else if (!string.IsNullOrWhiteSpace(reason))
            {
                Main.NewText(reason);
            }

            return;
        }

        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.TravelTeleport);
        packet.Write((byte)TravelTeleportPacketType.Teleport);
        packet.Write((byte)target.Type);
        packet.Write((short)target.PlayerIndex);
        packet.Send();
    }

    private static void ReceiveTeleport(BinaryReader reader, int whoAmI)
    {
        TravelType type = (TravelType)reader.ReadByte();
        int targetPlayerIndex = reader.ReadInt16();

        if (Main.netMode != NetmodeID.Server)
            return;

        if (whoAmI < 0 || whoAmI >= Main.maxPlayers || Main.player[whoAmI] is not { active: true } player)
            return;

        //Log.Chat($"[TravelTeleport] Request player={player.name} type={type} target={targetPlayerIndex}");

        if (type == TravelType.Random)
        {
            player.velocity = Vector2.Zero;
            player.TeleportationPotion();
            player.fallStart = (int)(player.position.Y / 16f);

            NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 0, player.whoAmI, player.position.X, player.position.Y, TeleportationStyleID.TeleportationPotion);
            NetMessage.SendData(MessageID.SyncPlayer, -1, -1, null, player.whoAmI);
            SendTeleportSound(player.Center);

            //Log.Chat($"[TravelTeleport] Random teleported {player.name} pos={player.position}");
            return;
        }

        if (!TryResolveTarget(player, type, targetPlayerIndex, out Vector2 position, out string reason))
        {
            //Log.Chat($"[TravelTeleport] Blocked {player.name}: type={type}, target={targetPlayerIndex}, reason={reason}");
            return;
        }

        player.velocity = Vector2.Zero;
        player.Teleport(position, TeleportationStyleID.RodOfDiscord);
        player.fallStart = (int)(position.Y / 16f);

        NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 0, player.whoAmI, position.X, position.Y, TeleportationStyleID.RodOfDiscord);
        NetMessage.SendData(MessageID.SyncPlayer, -1, -1, null, player.whoAmI);
        SendTeleportSound(player.Center);

        if (ModContent.GetInstance<ClientConfig>().ShowTeleportPlayerMessages)
        {
            TeleportChat.Announce(player, type, targetPlayerIndex);
        }

        //Log.Chat($"[TravelTeleport] Teleported {player.name}: type={type}, target={targetPlayerIndex}, pos={position}");
    }

    private static void SendTeleportSound(Vector2 worldPosition)
    {
        if (Main.netMode != NetmodeID.Server)
        {
            PlayTeleportSound(worldPosition);
            return;
        }

        ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.TravelTeleport);
        packet.Write((byte)TravelTeleportPacketType.TeleportSound);
        packet.Write(worldPosition.X);
        packet.Write(worldPosition.Y);
        packet.Send();
    }

    private static void ReceiveTeleportSound(BinaryReader reader)
    {
        Vector2 worldPosition = new(reader.ReadSingle(), reader.ReadSingle());

        if (Main.netMode == NetmodeID.Server)
            return;

        PlayTeleportSound(worldPosition);
    }

    private static void PlayTeleportSound(Vector2 worldPosition)
    {
        if (Main.dedServ)
            return;

        SoundEngine.PlaySound(SoundID.Item6, worldPosition);
    }

    private static bool TryResolveTarget(Player player, TravelType type, int targetPlayerIndex, out Vector2 position, out string reason)
    {
        position = Vector2.Zero;
        reason = "";

        if (player?.active != true)
        {
            reason = "Player inactive";
            return false;
        }

        if (player.dead || player.ghost)
        {
            reason = "Player dead or ghost";
            return false;
        }

        //Log.Chat($"[TravelTeleport] Resolve start player={player.name} type={type} target={targetPlayerIndex}");

        switch (type)
        {
            case TravelType.World:
                position = GetPlayerTopLeftAtTile(player, Main.spawnTileX, Main.spawnTileY);
                //Log.Chat($"[TravelTeleport] Resolved world pos={position}");
                return true;

            case TravelType.Bed:
                return TryResolveBed(player, targetPlayerIndex, out position, out reason);

            case TravelType.Portal:
                return TryResolvePortal(player, targetPlayerIndex, out position, out reason);

            default:
                reason = "Unsupported travel type";
                return false;
        }
    }

    private static bool TryResolveBed(Player player, int ownerIndex, out Vector2 position, out string reason)
    {
        position = Vector2.Zero;
        reason = "";

        if (!TryGetFriendlyPlayer(player, ownerIndex, out Player owner))
        {
            reason = "Invalid bed owner";
            return false;
        }

        if (owner.SpawnX < 0 || owner.SpawnY < 0 || !Player.CheckSpawn(owner.SpawnX, owner.SpawnY))
        {
            reason = $"No valid bed for owner={owner.name}, spawn=({owner.SpawnX},{owner.SpawnY})";
            return false;
        }

        position = GetPlayerTopLeftAtTile(player, owner.SpawnX, owner.SpawnY);
        return true;
    }

    private static bool TryResolvePortal(Player player, int ownerIndex, out Vector2 position, out string reason)
    {
        position = Vector2.Zero;
        reason = "";

        if (!TryGetFriendlyPlayer(player, ownerIndex, out Player owner))
        {
            reason = "Invalid portal owner";
            return false;
        }

        foreach (PortalNPC portal in PortalSystem.ActivePortals())
        {
            if (portal.OwnerIndex != ownerIndex || !PortalSystem.IsFriendlyPortal(player, portal))
                continue;

            position = GetPlayerTopLeftAtWorldBottom(player, portal.WorldPosition);
            return true;
        }

        reason = $"No portal for owner={owner.name}";
        return false;
    }

    private static bool TryGetFriendlyPlayer(Player player, int index, out Player target)
    {
        target = null;

        if (index < 0 || index >= Main.maxPlayers || Main.player[index] is not { active: true } found)
            return false;

        if (index != player.whoAmI && (player.team <= 0 || found.team != player.team))
            return false;

        target = found;
        return true;
    }

    private static bool Fail(string failReason, out Vector2 position, out string reason)
    {
        position = Vector2.Zero;
        reason = failReason;
        return false;
    }

    private static Vector2 GetPlayerTopLeftAtTile(Player player, int tileX, int tileY)
    {
        return new Vector2(tileX * 16f + 8f - player.width * 0.5f, tileY * 16f - player.height);
    }

    private static Vector2 GetPlayerTopLeftAtWorldBottom(Player player, Vector2 worldBottom)
    {
        return new Vector2(worldBottom.X - player.width * 0.5f, worldBottom.Y - player.height);
    }
}
