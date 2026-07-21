using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Bounties;
using PvPAdventure.Core.Utilities;
using PvPFramework.Common.Scoreboard;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Statistics.UI;

// Fills the scoreboard header's top-right corner (freed up by PvP Framework's ScoreboardTeamHeader
// hook) with each team's match score: total points and bounty shards.
[Autoload(Side = ModSide.Client)]
public class AdventureScoreboardHeader : ModSystem
{
    public override void Load() => ScoreboardTeamHeader.DrawExtra = DrawTeamExtra;
    public override void Unload() => ScoreboardTeamHeader.DrawExtra = null;

    private static void DrawTeamExtra(Rectangle corner, Team team)
    {
        int points = ModContent.GetInstance<PointsManager>().Points.TryGetValue(team, out int p) ? p : 0;
        int shards = ModContent.GetInstance<BountyManager>().Bounties.TryGetValue(team, out IList<BountyManager.Page> pages)
            ? pages.Count
            : 0;

        // Left half: points. Right half: bounty shards (with the gem icon).
        int half = corner.Width / 2;
        Rectangle pointsCell = new(corner.X, corner.Y, half, corner.Height);
        Rectangle shardsCell = new(corner.X + half, corner.Y, corner.Width - half, corner.Height);

        SpriteBatch spriteBatch = Main.spriteBatch;
        Texture2D gem = Ass.IconGem is { IsLoaded: true } gemAsset ? gemAsset.Value : null;

        DrawLabeledValue(spriteBatch, pointsCell, "POINTS", points.ToString(), null);
        DrawLabeledValue(spriteBatch, shardsCell, "SHARDS", shards.ToString(), gem);

        if (corner.Contains(Main.MouseScreen.ToPoint()))
        {
            Main.LocalPlayer.mouseInterface = true;
            Main.instance.MouseText($"{team} Team — {points} points, {shards} bounty shards");
        }
    }

    private static void DrawLabeledValue(SpriteBatch spriteBatch, Rectangle cell, string label, string value, Texture2D icon)
    {
        // Label across the top, dimmed and centered.
        Utils.DrawBorderString(spriteBatch, label, new Vector2(cell.Center.X, cell.Y), Color.White * 0.55f, 0.6f, 0.5f, 0f);

        // Value below, centered as an (optional icon + number) group.
        const float valueScale = 1.15f;
        float valueWidth = FontAssets.MouseText.Value.MeasureString(value).X * valueScale;
        float iconSize = icon != null ? 18f : 0f;
        float gap = icon != null ? 3f : 0f;
        float left = cell.Center.X - (iconSize + gap + valueWidth) / 2f;
        float valueCenterY = cell.Y + 24f;

        if (icon != null)
        {
            float iconScale = iconSize / Math.Max(icon.Width, icon.Height);
            spriteBatch.Draw(icon, new Vector2(left + iconSize / 2f, valueCenterY), null, Color.White, 0f,
                icon.Size() / 2f, iconScale, SpriteEffects.None, 0f);
        }

        Utils.DrawBorderString(spriteBatch, value, new Vector2(left + iconSize + gap, valueCenterY), Color.White, valueScale, 0f, 0.5f);
    }
}
