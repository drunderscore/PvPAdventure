using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaTeam = Terraria.Enums.Team;

namespace PvPAdventure.Common.Teams;

[Autoload(Side =ModSide.Both)]
internal sealed class TeamBedSystem : ModSystem
{
    private readonly Dictionary<Point, TerrariaTeam> bedTeams = [];
    private readonly Dictionary<Point, int> _bedLastSetter = [];
    private readonly Dictionary<int, Point> _lastOriginByPlayer = [];

    public bool TryGetTeam(Point origin, out TerrariaTeam team) =>
        bedTeams.TryGetValue(origin, out team);

    // Client: receive from server
    public void SetFromNet(Point origin, TerrariaTeam team)
    {
        if (team == TerrariaTeam.None)
        {
            bool changed = bedTeams.Remove(origin) | _bedLastSetter.Remove(origin);
            if (changed)
                Log.Chat($"Bed at ({origin.X},{origin.Y}) owned by: None");
            return;
        }

        if (bedTeams.TryGetValue(origin, out TerrariaTeam prev) && prev == team)
            return;

        bedTeams[origin] = team;
        Log.Chat($"Bed at ({origin.X},{origin.Y}) owned by: {team}");
    }

    // Server: update from player's current bed + team and broadcast
    public void UpdateFromPlayer(Player p)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || p == null || !p.active)
            return;

        int pid = p.whoAmI;
        _lastOriginByPlayer.TryGetValue(pid, out Point prevOrigin);

        int sx = p.SpawnX;
        int sy = p.SpawnY;

        Point origin = default;

        bool hasSpawn = sx >= 0 && sy >= 0 && Player.CheckSpawn(sx, sy);
        bool hasOrigin = hasSpawn && TryFindBedOrigin(sx, sy, out origin);

        if (!hasOrigin)
        {
            _lastOriginByPlayer[pid] = new Point(-1, -1);

            if (prevOrigin.X >= 0)
                TryClearIfOwnedByPlayer(prevOrigin, pid);

            return;
        }

        _lastOriginByPlayer[pid] = origin;

        if (prevOrigin.X >= 0 && prevOrigin != origin)
            TryClearIfOwnedByPlayer(prevOrigin, pid);

        TerrariaTeam team = (TerrariaTeam)p.team;

        if (team == TerrariaTeam.None)
        {
            TryClearIfOwnedByPlayer(origin, pid);
            return;
        }

        if (bedTeams.TryGetValue(origin, out TerrariaTeam prevTeam) && prevTeam == team)
        {
            _bedLastSetter[origin] = pid;
            return;
        }

        bedTeams[origin] = team;
        _bedLastSetter[origin] = pid;
        SendUpdate(origin, team);
    }


    private void TryClearIfOwnedByPlayer(Point origin, int pid)
    {
        if (!_bedLastSetter.TryGetValue(origin, out int owner) || owner != pid)
            return;

        if (!bedTeams.Remove(origin))
            return;

        _bedLastSetter.Remove(origin);
        SendUpdate(origin, TerrariaTeam.None);
    }

    private void SendUpdate(Point origin, TerrariaTeam team)
    {
        //Log.Chat($"Bed at ({origin.X},{origin.Y}) owned by: {(team == TerrariaTeam.None ? "None" : team.ToString())}");

        if (Main.netMode == NetmodeID.Server)
        {
            var packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
            packet.Write((byte)Core.Net.AdventurePacketIdentifier.PlayerBed);
            packet.Write((byte)255);
            packet.Write(origin.X);
            packet.Write(origin.Y);
            packet.Write((byte)team);
            packet.Send();
            return;
        }

        SetFromNet(origin, team);
    }




    public override void OnWorldUnload() => bedTeams.Clear();

    private static bool TryFindBedOrigin(int sx, int sy, out Point origin)
    {
        const int w = 4;
        const int h = 2;
        const int stride = 18;
        const int r = 1;

        origin = default;

        int best = int.MaxValue;
        HashSet<Point> seen = [];

        for (int dx = -r; dx <= r; dx++)
        {
            for (int dy = -r; dy <= r; dy++)
            {
                int x = sx + dx;
                int y = sy + dy;

                if ((uint)x >= (uint)Main.maxTilesX || (uint)y >= (uint)Main.maxTilesY)
                    continue;

                Tile t = Main.tile[x, y];
                if (t == null || !t.HasTile || t.TileType != TileID.Beds)
                    continue;

                int ox = x - (t.TileFrameX / stride) % w;
                int oy = y - (t.TileFrameY / stride) % h;

                Point o = new(ox, oy);
                if (!seen.Add(o))
                    continue;

                int cx = ox + 2;
                int cy = oy + 1;

                int ddx = cx - sx;
                int ddy = cy - sy;
                int dist = ddx * ddx + ddy * ddy;

                if (dist >= best)
                    continue;

                best = dist;
                origin = o;
            }
        }

        return best != int.MaxValue;
    }

}
