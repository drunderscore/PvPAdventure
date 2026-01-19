using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Combat.TeamBoss;

public sealed class TeamBossBar : GlobalBossBar
{
    public override void PostDraw(SpriteBatch spriteBatch, NPC npc, BossBarDrawParams drawParams)
    {
        NPC bossNpc = TeamBossNPC.ResolveBossEntity(npc);
        TeamBossNPC bossData = bossNpc.GetGlobalNPC<TeamBossNPC>();

        bossData.RebuildTeamLifeCacheIfNeeded(bossNpc);

        IReadOnlyList<TeamBossNPC.TeamLifeEntry> entries = bossData.SortedTeamLifeCache;
        if (entries.Count == 0)
        {
            return;
        }

        // This is a constant position from BigProgressBarHelper.DrawFancyBar
        Point bossBarPosition = new Point(456, 22);

        // Calculated as BigProgressBarHelper does
        Rectangle rectangle = Utils.CenteredRectangle(
            Main.ScreenSize.ToVector2() * new Vector2(0.5f, 1f) + new Vector2(0f, -50f),
            bossBarPosition.ToVector2()
        );

        Texture2D texture = TextureAssets.Pvp[1].Value;

        for (int i = 0; i < entries.Count; i++)
        {
            TeamBossNPC.TeamLifeEntry entry = entries[i];
            Team team = entry.Team;

            // Shouldn't be possible, but be sure not to draw this.
            if (team == Team.None)
            {
                continue;
            }

            float lifeRemaining = 0f;
            if (bossNpc.lifeMax > 0)
            {
                lifeRemaining = (float)entry.Life / bossNpc.lifeMax;
            }

            Rectangle frame = texture.Frame(6);
            frame.X = frame.Width * (int)team;

            float visibility = 1f - Utils.GetLerpValue(0.85f, 1.0f, lifeRemaining, true);
            if (visibility <= 0f)
            {
                continue;
            }

            Color color = Color.White * visibility;

            // FIXME: Looks a little silly when overlapping, maybe they should get stacked or staggered when on top.
            Vector2 position = rectangle.TopLeft();
            position += new Vector2(
                (bossBarPosition.X * lifeRemaining) - (frame.Height / 2.0f),
                -30.0f
            );

            spriteBatch.Draw(texture, position, frame, color);
        }
    }
}
