using Microsoft.Xna.Framework;
using PvPAdventure.Core.Net;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using static PvPAdventure.Common.SpawnSelector.SpawnSystem;

namespace PvPAdventure.Common.SpawnSelector;

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

        Vector2 teleportPos;

        switch (type)
        {
            case SpawnType.World:
                teleportPos = new Vector2(Main.spawnTileX, Main.spawnTileY - 3).ToWorldCoordinates();
                break;

            case SpawnType.Teammate:
                {
                    short idx = reader.ReadInt16();
                    if (!SpawnSystem.IsValidTeammateIndex(requester, idx))
                        return;

                    Player target = Main.player[idx];
                    teleportPos = target.position;
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
                    // Use vanilla logic to choose a random teleport destination
                    Vector2 oldPos = requester.position;

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
    }
}



