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

        foreach (var (team, life) in npc.GetGlobalNPC<AdventureNpc>().TeamLife)
        {
            // This isn't normally possibly, but we for sure can't draw this.
            if (team == Team.None)
                continue;

            var lifePercent = (float)life / npc.lifeMax;

            var frame = TextureAssets.Pvp[1].Value.Frame(6);
            frame.X = frame.Width * (int)team;

            // FIXME: looks silly when overlapping, because alpha colors blend...
            //              maybe stack/stagger them? that takes math.
            var color = Color.White;
            color *= lifePercent.Remap(0.85f, 1.0f, 1.0f, 0.0f);

            spriteBatch.Draw(
                TextureAssets.Pvp[1].Value,
                rectangle.TopLeft() + new Vector2(
                    (bossBarPosition.X * lifePercent) - (frame.Height / 2.0f),
                    -30.0f
                ),
                frame,
                color
            );
        }
    }
}