using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Config;
using System;
using Terraria;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.World.Outlines.TileOutlines;

// Draw team colored outline around all beds.
internal sealed class BedOutlineTile : GlobalTile
{
    private static int _myBedCooldown;
    private static Point _cachedMyBedOrigin = new(-1, -1);
    private static ulong _cachedAtUpdate;

    public override bool PreDraw(int i, int j, int type, SpriteBatch sb)
    {
        //return true;

        var config = ModContent.GetInstance<ClientConfig>();
        if (!config.PlayerOutline.Beds || type != TileID.Beds)
            return true;

        Tile t = Main.tile[i, j];

        const int w = 4;
        const int h = 2;
        const int stride = 18;

        int ox = i - (t.TileFrameX / stride) % w;
        int oy = j - (t.TileFrameY / stride) % h;
        if (i != ox || j != oy)
            return true;

        Team team = GetBedTeam(ox, oy);
        if (team == Team.None)
            return true;

        Color border = Main.teamColor[(int)team];
        border.A = 255;

        int frameX = Main.tile[ox, oy].TileFrameX;
        int frameY = Main.tile[ox, oy].TileFrameY;

        var sys = ModContent.GetInstance<TileOutlineSystem>();
        if (!sys.TryGet(TileID.Beds, frameX, frameY, w, h, border, out RenderTarget2D rt, out Vector2 origin))
            return true;

        // Position calculation
        Vector2 off = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange, Main.offScreenRange);
        Vector2 worldCenter = new(ox * 16 + w * 8, oy * 16 + h * 8);
        Vector2 pos = worldCenter - Main.screenPosition + off;

        sb.Draw(rt, pos, null, Color.White, 0f, origin, 1f, SpriteEffects.None, 0f);

        return true;
    }

    private static Team GetBedTeam(int ox, int oy)
    {
        return (Team)Main.LocalPlayer.team;

        //const int w = 4, h = 2;

        //for (int p = 0; p < Main.maxPlayers; p++)
        //{
        //    Player plr = Main.player[p];
        //    if (plr == null || !plr.active || plr.team == 0)
        //        continue;

        //    int sx = plr.SpawnX, sy = plr.SpawnY;
        //    if (sx < 0 || sy < 0)
        //        continue;

        //    bool inside = sx >= ox && sx < ox + w && sy >= oy && sy < oy + h;
        //    bool above = sx >= ox && sx < ox + w && (sy == oy - 1 || sy == oy - 2);

        //    if (inside || above)
        //        return (Team)plr.team;
        //}

        //return Team.None;
    }

}

