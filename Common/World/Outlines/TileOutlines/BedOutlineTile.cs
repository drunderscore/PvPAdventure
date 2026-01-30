using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Teams;
using PvPAdventure.Core.Config;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Team = Terraria.Enums.Team;

namespace PvPAdventure.Common.World.Outlines.TileOutlines;

// Draw team colored outline around all beds.
internal sealed class BedOutlineTile : GlobalTile
{
    public override bool PreDraw(int i, int j, int type, SpriteBatch sb)
    {
        var config = ModContent.GetInstance<ClientConfig>();
        if (!config.BedOutlines || type != TileID.Beds)
            return true;

        const int w = 4;
        const int h = 2;

        Point bedTile = GetBedTileWorldPos(i, j);
        int bedX = bedTile.X;
        int bedY = bedTile.Y;

        // draw once per bed
        if (i != bedX || j != bedY)
            return true;

        Team team = GetBedTeam(bedX, bedY);
        if (team == Team.None)
            return true;

        Color border = Main.teamColor[(int)team];
        border.A = 255;

        int frameX = Main.tile[bedX, bedY].TileFrameX;
        int frameY = Main.tile[bedX, bedY].TileFrameY;

        var outlineSys = ModContent.GetInstance<TileOutlineSystem>();
        if (!outlineSys.TryGet(TileID.Beds, frameX, frameY, w, h, border, out RenderTarget2D renderTarget, out Vector2 origin))
            return true;

        Vector2 bedScreenPos = GetBedScreenPos(bedX, bedY, w, h);
        sb.Draw(renderTarget, bedScreenPos, null, Color.White, 0f, origin, 1f, SpriteEffects.None, 0f);

        return true;
    }

    // Retrieves the top left tile position of the bed in the world.
    private Point GetBedTileWorldPos(int i, int j)
    {
        Tile t = Main.tile[i, j];

        const int w = 4;
        const int h = 2;
        const int stride = 18;

        int bedX = i - (t.TileFrameX / stride) % w;
        int bedY = j - (t.TileFrameY / stride) % h;

        Point bedPoint = new(bedX, bedY);
        return bedPoint;
    }

    private Vector2 GetBedScreenPos(int bedX, int bedY, int w, int h)
    {
        // Position calculation
        Vector2 off = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange, Main.offScreenRange);
        Vector2 worldCenter = new(bedX * 16 + w * 8, bedY * 16 + h * 8);
        Vector2 pos = worldCenter - Main.screenPosition + off;

        return pos;
    }

    // Retrieves the team that owns the bed at the given position.
    private static Team GetBedTeam(int bedX, int bedY)
    {
        // debug: always draw local player team.
        //return (Team)Main.LocalPlayer.team;

        var teamBeds = ModContent.GetInstance<TeamBedSystem>();
        if (!teamBeds.TryGetTeam(new Point(bedX, bedY), out Team team) || team == Team.None)
        {
            return Team.None;
        }
        return team;
    }

}

