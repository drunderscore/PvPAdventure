using Microsoft.Xna.Framework;
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
    private readonly Dictionary<Point, int> bedLastSetter = [];
    private readonly Dictionary<int, BedState> lastStateByPlayer = [];

    private int updateTimer;

    public IEnumerable<(Point Origin, Team Team)> ActiveBeds()
    {
        foreach ((Point origin, Team team) in bedTeams)
            if (team != Team.None)
                yield return (origin, team);
    }

    public bool TryGetTeam(Point origin, out Team team)
    {
        return bedTeams.TryGetValue(origin, out team);
    }

    public override void OnWorldUnload()
    {
        bedTeams.Clear();
        bedLastSetter.Clear();
        lastStateByPlayer.Clear();
        updateTimer = 0;
    }

    public override void PostUpdatePlayers()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        if (++updateTimer < 15)
            return;

        updateTimer = 0;

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player player = Main.player[i];

            if (player?.active == true)
                UpdateFromPlayer(player);
        }
    }

    public void SetFromNet(Point origin, Team team)
    {
        if (team == Team.None)
        {
            bool changed = bedTeams.Remove(origin);
            bedLastSetter.Remove(origin);

            //Log.Chat($"[TeamBed] Client clear origin=({origin.X},{origin.Y}) changed={changed}");
            return;
        }

        Team previous = bedTeams.TryGetValue(origin, out Team found) ? found : Team.None;
        bedTeams[origin] = team;

        //Log.Chat($"[TeamBed] Client set origin=({origin.X},{origin.Y}) {previous} -> {team}");
    }

    public void UpdateFromPlayer(Player player)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || player?.active != true)
            return;

        int playerId = player.whoAmI;
        bool hasPrevious = lastStateByPlayer.TryGetValue(playerId, out BedState previous);
        BedState current = GetCurrentState(player);

        if (hasPrevious && previous.Equals(current))
            return;

        lastStateByPlayer[playerId] = current;

        //Log.Chat($"[TeamBed] Update player={player.name} id={playerId} team={(Team)player.team} spawn=({player.SpawnX},{player.SpawnY}) hasBed={current.HasOrigin} origin=({current.Origin.X},{current.Origin.Y})");

        if (hasPrevious && previous.HasOrigin && (!current.HasOrigin || previous.Origin != current.Origin))
            TryClearIfOwnedByPlayer(previous.Origin, playerId);

        if (!current.HasOrigin)
            return;

        Team team = (Team)player.team;

        if (team == Team.None)
        {
            TryClearIfOwnedByPlayer(current.Origin, playerId);
            return;
        }

        if (bedTeams.TryGetValue(current.Origin, out Team previousTeam) && previousTeam == team && bedLastSetter.TryGetValue(current.Origin, out int setter) && setter == playerId)
            return;

        bedTeams[current.Origin] = team;
        bedLastSetter[current.Origin] = playerId;

        //Log.Chat($"[TeamBed] Set origin=({current.Origin.X},{current.Origin.Y}) owner={player.name} team={team}");
        SendUpdate(current.Origin, team);
    }

    public void SendAllBedsToClient(int toClient)
    {
        foreach ((Point origin, Team team) in bedTeams)
        {
            ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
            packet.Write((byte)Core.Net.AdventurePacketIdentifier.TeamBed);
            packet.Write((byte)255);
            packet.Write(origin.X);
            packet.Write(origin.Y);
            packet.Write((byte)team);
            packet.Send(toClient);

            //Log.Chat($"[TeamBed] Send existing bed to client={toClient} origin=({origin.X},{origin.Y}) team={team}");
        }
    }

    private BedState GetCurrentState(Player player)
    {
        Point origin = new(-1, -1);

        bool hasSpawn = player.SpawnX >= 0 && player.SpawnY >= 0 && Player.CheckSpawn(player.SpawnX, player.SpawnY);
        bool hasOrigin = hasSpawn && TryFindBedOrigin(player.SpawnX, player.SpawnY, out origin);

        if (!hasOrigin)
            origin = new Point(-1, -1);

        return new BedState(player.SpawnX, player.SpawnY, player.team, origin, hasOrigin);
    }

    private void TryClearIfOwnedByPlayer(Point origin, int playerId)
    {
        if (!bedLastSetter.TryGetValue(origin, out int owner) || owner != playerId)
        {
            //Log.Chat($"[TeamBed] Clear skipped origin=({origin.X},{origin.Y}) player={playerId}");
            return;
        }

        bool removed = bedTeams.Remove(origin);
        bedLastSetter.Remove(origin);

        //Log.Chat($"[TeamBed] Clear origin=({origin.X},{origin.Y}) player={playerId} removed={removed}");

        if (removed)
            SendUpdate(origin, Team.None);
    }

    private void SendUpdate(Point origin, Team team)
    {
        //Log.Chat($"[TeamBed] Broadcast/local origin=({origin.X},{origin.Y}) team={team}");

        if (Main.netMode != NetmodeID.Server)
            return;

        ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
        packet.Write((byte)Core.Net.AdventurePacketIdentifier.TeamBed);
        packet.Write((byte)255);
        packet.Write(origin.X);
        packet.Write(origin.Y);
        packet.Write((byte)team);
        packet.Send();
    }

    private static bool TryFindBedOrigin(int sx, int sy, out Point origin)
    {
        const int width = 4;
        const int height = 2;
        const int stride = 18;
        const int searchRadius = 8;

        origin = default;

        int best = int.MaxValue;
        HashSet<Point> seen = [];

        for (int dx = -searchRadius; dx <= searchRadius; dx++)
        {
            for (int dy = -searchRadius; dy <= searchRadius; dy++)
            {
                int x = sx + dx;
                int y = sy + dy;

                if ((uint)x >= (uint)Main.maxTilesX || (uint)y >= (uint)Main.maxTilesY)
                    continue;

                Tile tile = Main.tile[x, y];

                if (tile == null || !tile.HasTile || tile.TileType != TileID.Beds)
                    continue;

                int ox = x - tile.TileFrameX / stride % width;
                int oy = y - tile.TileFrameY / stride % height;
                Point candidate = new(ox, oy);

                if (!seen.Add(candidate))
                    continue;

                int centerX = ox + 2;
                int centerY = oy + 1;
                int distX = centerX - sx;
                int distY = centerY - sy;
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

        public bool Equals(BedState other)
        {
            return SpawnX == other.SpawnX && SpawnY == other.SpawnY && Team == other.Team && Origin == other.Origin && HasOrigin == other.HasOrigin;
        }
    }
}