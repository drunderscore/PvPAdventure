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

        // get the team that owns the bed at the given position.
        if (!ModContent.GetInstance<TeamBedSystem>().TryGetTeam(new Point(bedX, bedY), out Team team) || team == Team.None)
        {
            return true;
        }

        Color border = Main.teamColor[(int)team];
        border.A = 255;

        int frameX = Main.tile[bedX, bedY].TileFrameX;
        int frameY = Main.tile[bedX, bedY].TileFrameY;

        var outlineSys = ModContent.GetInstance<TileOutlineSystem>();
        if (!outlineSys.TryGet(TileID.Beds, frameX, frameY, w, h, border, out RenderTarget2D renderTarget, out Vector2 origin))
            return true;

        Vector2 bedScreenPos = GetBedScreenPos(bedX, bedY, w, h);

        // Fade the outline drawing based on light in the world
        float fade = GetBedLightFade(bedX, bedY, w, h);
        if (fade <= 0f)
            return true;
        Color drawColor = Color.White * fade;

        sb.Draw(renderTarget, bedScreenPos, null, drawColor, 0f, origin, 1f, SpriteEffects.None, 0f);

        return true;
    }

    // Retrieves the top left tile position (origin) of the bed in the world.
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

    private static float GetBedLightFade(int bedX, int bedY, int w, int h)
    {
        int x0 = bedX;
        int x1 = bedX + w - 1;
        int y0 = bedY - 1;
        int y1 = bedY + h - 1;

        float sum = 0f;
        int n = 0;

        for (int x = x0; x <= x1; x++)
        {
            for (int y = y0; y <= y1; y++)
            {
                if ((uint)x >= (uint)Main.maxTilesX || (uint)y >= (uint)Main.maxTilesY)
                    continue;

                Color c = Lighting.GetColor(x, y);
                sum += (c.R + c.G + c.B) / (255f * 3f);
                n++;
            }
        }

        float avg = n > 0 ? sum / n : 0f;

        const float fadeInAt = 0.01f;  // below this -> 0 alpha
        const float fullAt = 0.35f;  // above this -> full alpha

        float t = (avg - fadeInAt) / (fullAt - fadeInAt);
        if (t < 0f) t = 0f;
        if (t > 1f) t = 1f;

        // Smoothstep for nicer ramp
        return t * t * (3f - 2f * t);
    }

}

