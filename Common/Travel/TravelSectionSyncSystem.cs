using Microsoft.Xna.Framework;
using PvPAdventure.Common.Travel.Portals;
using PvPAdventure.Content.Portals;
using PvPAdventure.Core.Config;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Travel;

/// <summary>
/// Loads only the world sections needed for validated travel previews and teleports.
/// </summary>
internal sealed class TravelSectionSyncSystem : ModSystem
{
    private const ulong ClientRequestIntervalTicks = 30;
    private const ulong ServerRequestIntervalTicks = 10;

    private static readonly Dictionary<PreviewTarget, ulong> LastClientRequestTicks = [];
    private static readonly Dictionary<ServerPreviewTarget, ulong> LastServerRequestTicks = [];

    internal static void RequestPreview(TravelTarget target)
    {
        RequestPreview(target.Type, target.PlayerIndex);
    }

    internal static void RequestPreview(TravelType type, int targetPlayerIndex)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient || !TravelRules.AllowSpectating ||
            !IsWellFormedTarget(type, targetPlayerIndex))
            return;

        ulong now = Main.GameUpdateCount;
        PreviewTarget target = new(type, targetPlayerIndex);

        if (LastClientRequestTicks.TryGetValue(target, out ulong lastRequest) &&
            ElapsedTicks(now, lastRequest) < ClientRequestIntervalTicks)
            return;

        LastClientRequestTicks[target] = now;

        TravelTeleportNetHandler.SendSectionRequest(type, targetPlayerIndex);
    }

    internal static void ReceiveRequest(BinaryReader reader, int whoAmI)
    {
        TravelType type = (TravelType)reader.ReadByte();
        int targetPlayerIndex = reader.ReadInt16();

        if (Main.netMode != NetmodeID.Server || !TravelRules.AllowSpectating || !IsActiveClient(whoAmI) ||
            !IsWellFormedTarget(type, targetPlayerIndex))
            return;

        ulong now = Main.GameUpdateCount;
        ServerPreviewTarget target = new(whoAmI, type, targetPlayerIndex);

        if (LastServerRequestTicks.TryGetValue(target, out ulong lastRequest) &&
            ElapsedTicks(now, lastRequest) < ServerRequestIntervalTicks)
            return;

        LastServerRequestTicks[target] = now;

        Player requester = Main.player[whoAmI];
        if (!TryResolvePreviewPosition(requester, type, targetPlayerIndex, out Vector2 position))
            return;

        RemoteClient.CheckSection(whoAmI, position);
    }

    internal static void PrepareTeleport(int whoAmI, Vector2 destination)
    {
        if (Main.netMode != NetmodeID.Server || !IsActiveClient(whoAmI))
            return;

        RemoteClient.CheckSection(whoAmI, destination);
    }

    public override void OnWorldUnload()
    {
        Reset();
    }

    public override void Unload()
    {
        Reset();
    }

    private static bool TryResolvePreviewPosition(Player requester, TravelType type, int targetPlayerIndex, out Vector2 position)
    {
        position = Vector2.Zero;
        ServerConfig.TravelSystemConfig config = ModContent.GetInstance<ServerConfig>().TravelSystem;

        switch (type)
        {
            case TravelType.World when config.IsWorldSpawnTeleportEnabled:
                if (Main.spawnTileX <= 0 || Main.spawnTileY <= 0)
                    return false;

                position = new Vector2(Main.spawnTileX, Main.spawnTileY - 3).ToWorldCoordinates();
                return true;

            case TravelType.Bed when config.IsTeammateSpawnTeleportEnabled:
                if (!TryGetFriendlyPlayer(requester, targetPlayerIndex, out Player bedOwner) ||
                    bedOwner.SpawnX < 0 || bedOwner.SpawnY < 0 || !Player.CheckSpawn(bedOwner.SpawnX, bedOwner.SpawnY))
                    return false;

                position = new Vector2(bedOwner.SpawnX, bedOwner.SpawnY - 3).ToWorldCoordinates();
                return true;

            case TravelType.Portal when config.IsTeammateSpawnTeleportEnabled:
                if (!TryGetFriendlyPlayer(requester, targetPlayerIndex, out _))
                    return false;

                foreach (PortalNPC portal in PortalSystem.ActivePortals())
                {
                    if (portal.OwnerIndex != targetPlayerIndex || !PortalSystem.IsFriendlyPortal(requester, portal))
                        continue;

                    position = portal.WorldPosition;
                    return true;
                }

                return false;

            default:
                return false;
        }
    }

    private static bool TryGetFriendlyPlayer(Player requester, int playerIndex, out Player target)
    {
        target = null;

        if (requester?.active != true || playerIndex < 0 || playerIndex >= Main.maxPlayers ||
            Main.player[playerIndex] is not { active: true } found)
            return false;

        if (playerIndex != requester.whoAmI && (requester.team <= 0 || found.team != requester.team))
            return false;

        target = found;
        return true;
    }

    private static bool IsActiveClient(int whoAmI)
    {
        return whoAmI >= 0 && whoAmI < Main.maxPlayers && Main.player[whoAmI]?.active == true &&
            Netplay.Clients[whoAmI].IsActive && Netplay.Clients[whoAmI].State == 10;
    }

    private static bool IsWellFormedTarget(TravelType type, int targetPlayerIndex)
    {
        return type switch
        {
            TravelType.World => targetPlayerIndex == -1,
            TravelType.Bed or TravelType.Portal => targetPlayerIndex >= 0 && targetPlayerIndex < Main.maxPlayers,
            _ => false
        };
    }

    private static ulong ElapsedTicks(ulong now, ulong then)
    {
        return now >= then ? now - then : ulong.MaxValue;
    }

    private static void Reset()
    {
        LastClientRequestTicks.Clear();
        LastServerRequestTicks.Clear();
    }

    private readonly record struct PreviewTarget(TravelType Type, int TargetPlayerIndex);
    private readonly record struct ServerPreviewTarget(int RequestingPlayerIndex, TravelType Type, int TargetPlayerIndex);
}
