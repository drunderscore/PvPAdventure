using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.MainMenu.MatchHistory;
using PvPAdventure.Core.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.MainMenu.MatchHistory.UI;

public class UITeamBossCompletion : UIElement
{
    private readonly TeamBossCompletion[] _completions;

    public UITeamBossCompletion(TeamBossCompletion[] completions)
    {
        _completions = completions ?? Array.Empty<TeamBossCompletion>();
    }

    private bool HasAnyTeamCompleted(short bossId)
    {
        for (int i = 0; i < _completions.Length; i++)
        {
            if (_completions[i].BossId == bossId)
                return true;
        }
        return false;
    }

    private bool HasCompleted(short bossId, Team team)
    {
        for (int i = 0; i < _completions.Length; i++)
        {
            if (_completions[i].BossId == bossId && _completions[i].Team == team)
                return true;
        }
        return false;
    }

    protected override void DrawSelf(SpriteBatch sb)
    {
        CalculatedStyle dims = GetDimensions();

        // debug: do not delete! draw full size
#if DEBUG
        //sb.Draw(TextureAssets.MagicPixel.Value, dims.ToRectangle(), Color.Red * 0.35f);
#endif

        const int horizontalSpaceBetweenBossHeads = 50;
        const int verticalSeparationBetweenBossHeadAndTeamIcon = 40;
        const int verticalSpaceBetweenTeamIcons = 26;
        const int padding = 14;
        const int teamIconXOffset = 8;
        const int separatorYOffset = 28;

        var config = ModContent.GetInstance<ServerConfig>();

        List<short> bosses = config.BossOrder
            .Select(d => (short)d.Type)
            .Where(id => id != -1)
            .ToList();

        bool onlyDisplayWorldEvilBoss =
            config.OnlyDisplayWorldEvilBoss &&
            bosses.Contains(NPCID.EaterofWorldsHead) &&
            bosses.Contains(NPCID.BrainofCthulhu);

        if (onlyDisplayWorldEvilBoss)
        {
            if (WorldGen.crimson)
                bosses.Remove(NPCID.EaterofWorldsHead);
            else
                bosses.Remove(NPCID.BrainofCthulhu);
        }

        float contentWidth = dims.Width - padding * 2f;
        float neededWidth = bosses.Count <= 1 ? 1f : ((bosses.Count - 1) * horizontalSpaceBetweenBossHeads + 1f);
        float scale = Math.Min(1f, contentWidth / neededWidth);

        Vector2 nextBossHeadPosition = new(dims.X + padding, dims.Y + padding);

        sb.Draw(
            TextureAssets.MagicPixel.Value,
            new Rectangle(
                (int)(dims.X + 2),
                (int)(nextBossHeadPosition.Y + separatorYOffset * scale),
                (int)(dims.Width - 4),
                2),
            Color.White with { A = 60 });

        Texture2D teamIconsTexture = TextureAssets.Pvp[1].Value;

        Team[] teams = GetTeamsToDisplay(_completions);

        int maxTeams = Math.Min(5, teams.Length);
        for (int i = 0; i < bosses.Count; i++)
        {
            short bossId = bosses[i];

            bool anyTeamCompleted = HasAnyTeamCompleted(bossId);

            int headId = NPCID.Sets.BossHeadTextures[bossId == NPCID.Golem ? NPCID.GolemHead : bossId];
            if (headId != -1)
            {
                Main.BossNPCHeadRenderer.DrawWithOutlines(
                    null,
                    headId,
                    nextBossHeadPosition,
                    anyTeamCompleted ? Color.White : Color.Gray,
                    0f,
                    scale,
                    SpriteEffects.None);
            }

            Vector2 nextTeamIconPosition = new(
                nextBossHeadPosition.X - teamIconXOffset * scale,
                nextBossHeadPosition.Y + verticalSeparationBetweenBossHeadAndTeamIcon * scale);

            for (int t = 0; t < maxTeams; t++)
            {
                DrawTeamIconIfCompleted(teamIconsTexture, bossId, teams[t], nextTeamIconPosition, sb, scale);
                nextTeamIconPosition.Y += verticalSpaceBetweenTeamIcons * scale;
            }

            nextBossHeadPosition.X += horizontalSpaceBetweenBossHeads * scale;
        }

    }

    private void DrawTeamIconIfCompleted(Texture2D teamIconsTexture, short bossId, Team team, Vector2 pos, SpriteBatch spriteBatch, float scale)
    {
        if (!HasCompleted(bossId, team))
            return;

        Rectangle frame = teamIconsTexture.Frame(6, 1, (int)team);
        spriteBatch.Draw(teamIconsTexture, pos, frame, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    private static Team[] GetTeamsToDisplay(TeamBossCompletion[] completions)
    {
        if (completions == null || completions.Length == 0)
            return Array.Empty<Team>();

        return completions
            .Select(c => c.Team)
            .Where(t => t != Team.None)
            .Distinct()
            .OrderBy(t => (int)t)
            .ToArray();
    }

}

