using Microsoft.Xna.Framework;
using PvPAdventure.Common.Chat;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Team = Terraria.Enums.Team;

namespace PvPAdventure.Common.Travel.Beds;

[Autoload(Side = ModSide.Both)]
internal sealed class TeamBedSystem : ModSystem
{
    private readonly Dictionary<Point, Team> bedTeams = [];
    private readonly Dictionary<Point, int> bedOwners = [];
    private readonly Dictionary<int, BedState> lastStates = [];
    private int updateTimer;

    public IEnumerable<(Point Origin, Team Team)> ActiveBeds()
    {
        foreach ((Point origin, Team team) in bedTeams)
            if (team != Team.None)
                yield return (origin, team);
    }

    public bool TryGetTeam(Point origin, out Team team) => bedTeams.TryGetValue(origin, out team);

    public override void OnWorldUnload()
    {
        bedTeams.Clear();
        bedOwners.Clear();
        lastStates.Clear();
        updateTimer = 0;
    }

    public override void PostUpdatePlayers()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || ++updateTimer < 15)
            return;

        updateTimer = 0;

        for (int i = 0; i < Main.maxPlayers; i++)
            if (Main.player[i]?.active == true)
                UpdateFromPlayer(Main.player[i]);
    }

    public void SetFromNet(Point origin, Team team)
    {
        if (team == Team.None)
        {
            bedTeams.Remove(origin);
            bedOwners.Remove(origin);
            return;
        }

        bedTeams[origin] = team;
    }

    public void UpdateFromPlayer(Player player)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || player?.active != true)
            return;

        int playerId = player.whoAmI;
        BedState current = GetState(player);

        if (lastStates.TryGetValue(playerId, out BedState previous) && previous.Equals(current))
            return;

        lastStates[playerId] = current;

        if (previous.HasOrigin && (!current.HasOrigin || previous.Origin != current.Origin))
            ClearIfOwner(previous.Origin, playerId);

        if (!current.HasOrigin)
            return;

        Team team = (Team)player.team;

        if (team == Team.None)
        {
            ClearIfOwner(current.Origin, playerId);
            return;
        }

        ClaimBed(player, current.Origin, team);
    }

    public void SendAllBedsToClient(int toClient)
    {
        foreach ((Point origin, Team team) in bedTeams)
            SendBedUpdate(origin, team, toClient);
    }

    public void SendAllStateToClient(int toClient)
    {
        SendAllBedsToClient(toClient);

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player player = Main.player[i];

            if (player?.active == true)
                SendPlayerSpawn(i, player.SpawnX, player.SpawnY, toClient: toClient);
        }
    }

    private void ClaimBed(Player player, Point origin, Team team)
    {
        int playerId = player.whoAmI;

        bool hadOwner = bedOwners.TryGetValue(origin, out int previousOwnerId);
        bool sameOwner = hadOwner && previousOwnerId == playerId;
        bool sameTeamOwner = hadOwner && IsSameTeam(previousOwnerId, team);
        bool alreadyClaimed = bedTeams.TryGetValue(origin, out Team oldTeam) && oldTeam == team && sameOwner;

        if (alreadyClaimed)
            return;

        if (hadOwner && !sameOwner && !sameTeamOwner)
        {
            Log.Chat($"New bed claim pos=({origin.X},{origin.Y}) previousOwner={Main.player[previousOwnerId].name} newOwner={player.name}");
            DestroyPreviousOwnerBed(origin, previousOwnerId);
        }

        bedTeams[origin] = team;

        if (!hadOwner || !sameTeamOwner)
            bedOwners[origin] = playerId;

        SendBedUpdate(origin, team);
        TeleportChat.AnnounceBedSet(player);
    }

    private static bool IsSameTeam(int playerId, Team team)
    {
        if (playerId < 0 || playerId >= Main.maxPlayers || Main.player[playerId] is not { active: true } player)
            return false;

        return player.team > 0 && (Team)player.team == team;
    }

    private void DestroyPreviousOwnerBed(Point origin, int ownerId)
    {
        if (ownerId < 0 || ownerId >= Main.maxPlayers || Main.player[ownerId] is not { active: true } owner)
            return;

        if (!PlayerOwnsOrigin(owner, origin))
            return;

        Log.Chat($"Destroy previous owner bed pos=({origin.X},{origin.Y}) ownerId={ownerId}");

        owner.SpawnX = -1;
        owner.SpawnY = -1;
        lastStates[ownerId] = GetState(owner);

        SendPlayerSpawn(ownerId, -1, -1);
        TeleportChat.AnnounceBedDestroyed(owner, owner.name);
    }

    private void ClearIfOwner(Point origin, int playerId)
    {
        if (!bedOwners.TryGetValue(origin, out int owner) || owner != playerId)
            return;

        bool removed = bedTeams.Remove(origin);
        bedOwners.Remove(origin);

        if (removed)
            SendBedUpdate(origin, Team.None);
    }

    private static bool PlayerOwnsOrigin(Player player, Point origin)
    {
        return player.SpawnX >= 0 &&
            player.SpawnY >= 0 &&
            Player.CheckSpawn(player.SpawnX, player.SpawnY) &&
            TryFindBedOrigin(player.SpawnX, player.SpawnY, out Point found) &&
            found == origin;
    }

    private static BedState GetState(Player player)
    {
        Point origin = new(-1, -1);
        bool hasSpawn = player.SpawnX >= 0 && player.SpawnY >= 0 && Player.CheckSpawn(player.SpawnX, player.SpawnY);
        bool hasOrigin = hasSpawn && TryFindBedOrigin(player.SpawnX, player.SpawnY, out origin);

        if (!hasOrigin)
            origin = new Point(-1, -1);

        return new BedState(player.SpawnX, player.SpawnY, player.team, origin, hasOrigin);
    }

    private static void SendBedUpdate(Point origin, Team team, int toClient = -1)
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
        packet.Write((byte)Core.Net.AdventurePacketIdentifier.TeamBed);
        packet.Write((byte)255);
        packet.Write(origin.X);
        packet.Write(origin.Y);
        packet.Write((byte)team);
        packet.Send(toClient);
    }

    internal static void SendPlayerSpawn(int playerId, int spawnX, int spawnY, int toClient = -1, int ignoreClient = -1)
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
        packet.Write((byte)Core.Net.AdventurePacketIdentifier.TeamBed);
        packet.Write((byte)playerId);
        packet.Write(spawnX);
        packet.Write(spawnY);
        packet.Send(toClient, ignoreClient);
    }

    private static bool TryFindBedOrigin(int sx, int sy, out Point origin)
    {
        origin = default;
        int best = int.MaxValue;
        HashSet<Point> seen = [];

        for (int dx = -8; dx <= 8; dx++)
        {
            for (int dy = -8; dy <= 8; dy++)
            {
                int x = sx + dx;
                int y = sy + dy;

                if ((uint)x >= (uint)Main.maxTilesX || (uint)y >= (uint)Main.maxTilesY)
                    continue;

                Tile tile = Main.tile[x, y];

                if (tile == null || !tile.HasTile || tile.TileType != TileID.Beds)
                    continue;

                Point candidate = new(x - tile.TileFrameX / 18 % 4, y - tile.TileFrameY / 18 % 2);

                if (!seen.Add(candidate))
                    continue;

                int distX = candidate.X + 2 - sx;
                int distY = candidate.Y + 1 - sy;
                int dist = distX * distX + distY * distY;

                if (dist >= best)
                    continue;

                best = dist;
                origin = candidate;
            }
        }

        return best != int.MaxValue;
    }

    private readonly struct BedState
    {
        public readonly int SpawnX;
        public readonly int SpawnY;
        public readonly int Team;
        public readonly Point Origin;
        public readonly bool HasOrigin;

        public BedState(int spawnX, int spawnY, int team, Point origin, bool hasOrigin)
        {
            SpawnX = spawnX;
            SpawnY = spawnY;
            Team = team;
            Origin = origin;
            HasOrigin = hasOrigin;
        }

        public bool Equals(BedState other) => SpawnX == other.SpawnX && SpawnY == other.SpawnY && Team == other.Team && Origin == other.Origin && HasOrigin == other.HasOrigin;
    }
}