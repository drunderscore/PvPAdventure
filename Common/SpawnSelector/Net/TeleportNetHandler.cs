using Microsoft.Xna.Framework;
using PvPAdventure.Common.Chat;
using System.IO;
using Terraria;
using Terraria.ID;

namespace PvPAdventure.Common.SpawnSelector.Net;

public static class TeleportNetHandler
{
    public static void HandlePacket(BinaryReader reader, int whoAmI)
    {
        int requesterId = reader.ReadByte();
        SpawnType type = (SpawnType)reader.ReadByte();
        short packetTargetIdx = reader.ReadInt16();

        if (Main.netMode != NetmodeID.Server)
            return;

        if (requesterId != whoAmI || requesterId < 0 || requesterId >= Main.maxPlayers)
            return;

        if (Main.player[requesterId] is not { active: true } requester)
            return;

        SpawnPlayer spawnPlayer = requester.GetModPlayer<SpawnPlayer>();
        if (!spawnPlayer.CanTeleportNow())
            return;

        Vector2 teleportPos;
        int targetIdx = -1;

        switch (type)
        {
            case SpawnType.World:
                teleportPos = new Vector2(Main.spawnTileX, Main.spawnTileY - 3).ToWorldCoordinates();
                break;

            case SpawnType.MyPortal:
                if (!SpawnSystem.CanUseStoredPortal(requester))
                    return;

                if (!TryGetPortalTeleportPos(requester, requester, out teleportPos))
                    return;

                break;

            case SpawnType.TeammatePortal:
                if (!SpawnPlayer.IsValidTeammatePortalIndex(requester, packetTargetIdx))
                    return;

                targetIdx = packetTargetIdx;

                if (!TryGetPortalTeleportPos(requester, Main.player[packetTargetIdx], out teleportPos))
                    return;

                break;

            case SpawnType.TeammateBed:
                targetIdx = packetTargetIdx;

                if (!TryGetBedTeleportPos(requester, targetIdx, out teleportPos))
                    return;

                break;

            case SpawnType.MyBed:
                if (!TryGetBedTeleportPos(requester, requester.whoAmI, out teleportPos))
                    return;

                break;

            case SpawnType.Random:
                requester.TeleportationPotion();
                SyncTeleport(requester, requester.position);
                TeleportChat.Announce(requester, type);
                spawnPlayer.StartTeleportCooldown();
                return;

            default:
                return;
        }

        requester.Teleport(teleportPos, TeleportationStyleID.RecallPotion);
        SyncTeleport(requester, teleportPos);
        TeleportChat.Announce(requester, type, targetIdx);
        spawnPlayer.StartTeleportCooldown();
    }

    private static void SyncTeleport(Player player, Vector2 position)
    {
        NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 0, player.whoAmI, position.X, position.Y, TeleportationStyleID.RecallPotion);
        TeleportFxNetHandler.Send(player.whoAmI);
    }

    private static bool TryGetBedTeleportPos(Player requester, int ownerIndex, out Vector2 teleportPos)
    {
        teleportPos = Vector2.Zero;

        if (ownerIndex < 0 || ownerIndex >= Main.maxPlayers || Main.player[ownerIndex] is not { active: true } owner)
            return false;

        if (ownerIndex != requester.whoAmI && (requester.team == 0 || owner.team != requester.team))
            return false;

        if (owner.SpawnX < 0 || owner.SpawnY < 0 || !Player.CheckSpawn(owner.SpawnX, owner.SpawnY))
            return false;

        teleportPos = new Vector2(owner.SpawnX, owner.SpawnY - 6).ToWorldCoordinates();
        return true;
    }

    private static bool TryGetPortalTeleportPos(Player requester, Player portalOwner, out Vector2 teleportPos)
    {
        teleportPos = Vector2.Zero;
        if (portalOwner?.active != true || !PortalSystem.TryGetPortalWorldPos(portalOwner, out Vector2 worldPos))
            return false;

        teleportPos = worldPos - new Vector2(requester.width * 0.5f, requester.height);
        return true;
    }
}
