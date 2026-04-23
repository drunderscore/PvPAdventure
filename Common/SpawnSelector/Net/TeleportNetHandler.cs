using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.ID;

namespace PvPAdventure.Common.SpawnSelector.Net;

public static class TeleportNetHandler
{
    public static void HandlePacket(BinaryReader reader, int whoAmI)
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        byte requesterId = reader.ReadByte();
        SpawnType type = (SpawnType)reader.ReadByte();

        if (requesterId != whoAmI)
            return;

        Player requester = Main.player[requesterId];
        if (requester == null || !requester.active)
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
                if (!TryGetPortalTeleportPos(requester, requester, out teleportPos))
                    return;

                break;

            case SpawnType.TeammatePortal:
                {
                    short idx = reader.ReadInt16();
                    if (!SpawnPlayer.IsValidTeammatePortalIndex(requester, idx))
                        return;

                    targetIdx = idx;

                    if (!TryGetPortalTeleportPos(requester, Main.player[idx], out teleportPos))
                        return;

                    break;
                }

            case SpawnType.TeammateBed:
                {
                    short idx = reader.ReadInt16();
                    if (idx < 0 || idx >= Main.maxPlayers)
                        return;

                    Player bedOwner = Main.player[idx];
                    if (bedOwner == null || !bedOwner.active)
                        return;

                    targetIdx = idx;

                    if (idx != requester.whoAmI)
                    {
                        if (requester.team == 0 || bedOwner.team != requester.team)
                            return;
                    }

                    if (bedOwner.SpawnX < 0 || bedOwner.SpawnY < 0 || !Player.CheckSpawn(bedOwner.SpawnX, bedOwner.SpawnY))
                        return;

                    teleportPos = new Vector2(bedOwner.SpawnX, bedOwner.SpawnY - 6).ToWorldCoordinates();
                    break;
                }
            case SpawnType.MyBed:
                {
                    if (requester.SpawnX < 0 || requester.SpawnY < 0 ||
                        !Player.CheckSpawn(requester.SpawnX, requester.SpawnY))
                        return;

                    teleportPos = new Vector2(
                        requester.SpawnX,
                        requester.SpawnY - 6
                    ).ToWorldCoordinates();
                    break;
                }
            case SpawnType.Random:
                {
                    requester.TeleportationPotion();

                    // TeleportationPotion() already moved the player.
                    // We must re-sync the final position to all clients.
                    NetMessage.SendData(
                        MessageID.TeleportEntity,
                        -1, -1, null,
                        number: 0,
                        number2: requester.whoAmI,
                        number3: requester.position.X,
                        number4: requester.position.Y,
                        number5: TeleportationStyleID.RecallPotion
                    );

                    // Play teleport sound for everyone (local guaranteed)
                    TeleportFxNetHandler.Send(requester.whoAmI);
                    TeleportChat.Announce(requester, type);
                    spawnPlayer.StartTeleportCooldown();
                    return;
                }

            default:
                return;
        }

        requester.Teleport(teleportPos, TeleportationStyleID.RecallPotion);

        NetMessage.SendData(
            MessageID.TeleportEntity,
            -1, -1, null,
            number: 0,
            number2: requester.whoAmI,
            number3: teleportPos.X,
            number4: teleportPos.Y,
            number5: TeleportationStyleID.RecallPotion
        );

        // Send teleport sound effect to all clients
        TeleportFxNetHandler.Send(whoAmI);
        TeleportChat.Announce(requester, type, targetIdx);
        spawnPlayer.StartTeleportCooldown();
    }

    private static bool TryGetPortalTeleportPos(Player requester, Player portalOwner, out Vector2 teleportPos)
    {
        teleportPos = Vector2.Zero;

        if (portalOwner == null || !portalOwner.active)
            return false;

        if (!PortalSystem.TryGetPortalWorldPos(portalOwner, out Vector2 worldPos))
            return false;

        teleportPos = worldPos - new Vector2(requester.width * 0.5f, requester.height);
        return true;
    }
}



