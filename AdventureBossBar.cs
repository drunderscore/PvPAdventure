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

        var adventureNpc = npc.GetGlobalNPC<AdventureNpc>();

        // FIXME: This sort order should be pre-calculated for us so we don't need to do it all the time!
        var teamLife = adventureNpc.TeamLife
            .Where(kv => adventureNpc.HasBeenHurtByTeam.Contains(kv.Key))
            .OrderByDescending(kv => kv.Value);

        foreach (var (team, life) in teamLife)
        {
            // Shouldn't be possible, but be sure not to draw this.
            if (team == Team.None)
                continue;

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