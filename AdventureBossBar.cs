using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace PvPAdventure;

public class AdventureBossBar : GlobalBossBar
{
    public override void PostDraw(SpriteBatch spriteBatch, NPC npc, BossBarDrawParams drawParams)
    {
        // This is a constant position from BigProgressBarHelper.DrawFancyBar
        var bossBarPosition = new Point(456, 22);

        // Calculated as BigProgressBarHelper does
        var rectangle =
            Utils.CenteredRectangle(Main.ScreenSize.ToVector2() * new Vector2(0.5f, 1f) + new Vector2(0f, -50f),
                bossBarPosition.ToVector2());

        // FIXME: This should just be a sorted dictionary by value so we can render all of them in the correct Z order.
        var highest = npc.GetGlobalNPC<AdventureNpc>().TeamLife.MinBy(v => v.Value);

        foreach (var (team, life) in npc.GetGlobalNPC<AdventureNpc>().TeamLife)
        {
            // Shouldn't be possible, but be sure not to draw this.
            if (team == Team.None)
                continue;

            // Don't draw the highest team just yet -- ensure it draws in front of everyone else.
            if (team == highest.Key)
                continue;

            Draw(team, life);
        }

        Draw(highest.Key, highest.Value);

        return;

        void Draw(Team team, int life)
        {
            var lifeRemaining = (float)life / npc.lifeMax;

            var frame = TextureAssets.Pvp[1].Value.Frame(6);
            frame.X = frame.Width * (int)team;

            // FIXME: Looks a little silly when overlapping, maybe they should get stacked or staggered when on top.
            var color = Color.White;
            color *= lifeRemaining.Remap(0.85f, 1.0f, 1.0f, 0.0f);

            spriteBatch.Draw(
                TextureAssets.Pvp[1].Value,
                rectangle.TopLeft() + new Vector2(
                    (bossBarPosition.X * lifeRemaining) - (frame.Height / 2.0f),
                    -30.0f
                ),
                frame,
                color
            );
        }
    }
}