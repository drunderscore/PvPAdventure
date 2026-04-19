using Microsoft.Xna.Framework;
using PvPAdventure.Core.Debug;
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
        bool createPortal = reader.ReadBoolean();

        if (requesterId != whoAmI)
            return;

        Player requester = Main.player[requesterId];
        if (requester == null || !requester.active)
            return;

        Vector2 portalPosition = requester.position;
        Vector2 teleportPos;

        switch (type)
        {
            case SpawnType.World:
                teleportPos = new Vector2(Main.spawnTileX, Main.spawnTileY - 3).ToWorldCoordinates();
                break;

            case SpawnType.Teammate:
                {
                    short idx = reader.ReadInt16();
                    if (!AdventurePortalSystem.TryGetTeleportPosition(requester, idx, out teleportPos))
                    {
                        Log.Chat($"Adventure portal teleport failed, moreinfo: requester={requester.name} targetSlot={idx}");
                        return;
                    }

                    Log.Chat($"Adventure portal teleport target resolved, moreinfo: requester={requester.name} target={Main.player[idx]?.name ?? idx.ToString()} pos=({teleportPos.X:0},{teleportPos.Y:0})");
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
                    requester.TeleportationPotion();

                    if (createPortal)
                    {
                        Log.Chat($"Adventure portal creation requested after random teleport, moreinfo: player={requester.name} from=({portalPosition.X:0},{portalPosition.Y:0})");
                        AdventurePortalSystem.SetPortal(requester, portalPosition);
                    }

                    NetMessage.SendData(
                        MessageID.TeleportEntity,
                        -1, -1, null,
                        number: 0,
                        number2: requester.whoAmI,
                        number3: requester.position.X,
                        number4: requester.position.Y,
                        number5: TeleportationStyleID.RecallPotion
                    );

                    TeleportFxNetHandler.Send(requester.whoAmI);
                    return;
                }

            default:
                return;
        }

        requester.Teleport(teleportPos, TeleportationStyleID.RecallPotion);

        if (createPortal)
        {
            Log.Chat($"Adventure portal creation requested after teleport, moreinfo: player={requester.name} from=({portalPosition.X:0},{portalPosition.Y:0})");
            AdventurePortalSystem.SetPortal(requester, portalPosition);
        }

        NetMessage.SendData(
            MessageID.TeleportEntity,
            -1, -1, null,
            number: 0,
            number2: requester.whoAmI,
            number3: teleportPos.X,
            number4: teleportPos.Y,
            number5: TeleportationStyleID.RecallPotion
        );

        TeleportFxNetHandler.Send(whoAmI);
    }
}
