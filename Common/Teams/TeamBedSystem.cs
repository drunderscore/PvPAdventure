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

    public bool TryGetTeam(Point origin, out TerrariaTeam team) =>
        bedTeams.TryGetValue(origin, out team);

    // Client: receive from server
    public void SetFromNet(Point origin, TerrariaTeam team)
    {
        if (team == TerrariaTeam.None)
        {
            bedTeams.Remove(origin);
            Log.Chat($"Bed at ({origin.X},{origin.Y}) owned by: None");
            return;
        }

        bedTeams[origin] = team;
        Log.Chat($"Bed at ({origin.X},{origin.Y}) owned by: {team}");
    }

    // Server: update from player's current bed + team and broadcast
    public void UpdateFromPlayer(Player p)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || p == null || !p.active)
            return;

        int sx = p.SpawnX;
        int sy = p.SpawnY;

        if (sx < 0 || sy < 0 || !Player.CheckSpawn(sx, sy))
            return;

        if (!TryFindBedOrigin(sx, sy, out Point origin))
            return;

        TerrariaTeam team = (TerrariaTeam)p.team;

        bool changed;
        if (team == TerrariaTeam.None)
        {
            changed = bedTeams.Remove(origin);
        }
        else
        {
            changed = !bedTeams.TryGetValue(origin, out TerrariaTeam prev) || prev != team;
            if (changed)
                bedTeams[origin] = team;
        }

        if (!changed)
            return;

        Log.Chat($"Bed at ({origin.X},{origin.Y}) owned by: {(team == TerrariaTeam.None ? "None" : team.ToString())}");

        if (Main.netMode == NetmodeID.Server)
        {
            var packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
            packet.Write((byte)Core.Net.AdventurePacketIdentifier.PlayerBed);
            packet.Write((byte)255);
            packet.Write(origin.X);
            packet.Write(origin.Y);
            packet.Write((byte)team);
            packet.Send();
        }
        else
        {
            // Singleplayer: apply locally
            SetFromNet(origin, team);
        }
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
