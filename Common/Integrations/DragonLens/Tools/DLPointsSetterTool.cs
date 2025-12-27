using DragonLens.Core.Systems.ThemeSystem;
using DragonLens.Core.Systems.ToolSystem;
using DragonLens.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.AdminTools.PointsSetter;
using PvPAdventure.System;
using System.Collections.Generic;
using Terraria;
using Terraria.Enums;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Integrations.DragonLens.Tools;

[JITWhenModsEnabled("DragonLens")]
[ExtendsFromMod("DragonLens")]
public class DLPointsSetterTool : Tool
{
    public override string IconKey => DLIntegration.PointsSetterKey;

    public override string DisplayName => Language.GetTextValue("Mods.PvPAdventure.Tools.DLPointsSetterTool.DisplayName");

    public override string Description => GetDescription();
    private string GetDescription()
    {
        // Count active players on each team
        Dictionary<Team, int> counts = [];

        foreach (var p in Main.ActivePlayers)
        {
            Team team = (Team)p.team;

            if (counts.TryGetValue(team, out int value))
                counts[team] = ++value;
            else
                counts[team] = 1;
        }

        // Count points on each team
        var pm = ModContent.GetInstance<PointsManager>();
        var teamPointsDict = pm.Points;
        string result = "";

        foreach (var kvp in teamPointsDict)
        {
            Team team = kvp.Key;
            int points = kvp.Value;

            if (points != 0)
            {
                result += $"{team}: {points} points\n";
            }
        }

        return result;
    }
    public override void OnActivate()
    {
        var sys = ModContent.GetInstance<PointsSetterSystem>();
        if (sys == null)
        {
            Main.NewText("Failed to open PointsSetterSystem: System not found.", Color.Red);
            return;
        }

        sys.ToggleActive();
    }

    public override void DrawIcon(SpriteBatch spriteBatch, Rectangle position)
    {
        base.DrawIcon(spriteBatch, position);

        // We have to manually draw it for some reason
        spriteBatch.Draw(Ass.Icon_PointsSetter.Value, position, Color.White);

        var pss = ModContent.GetInstance<PointsSetterSystem>();

        if (pss.IsActive())
        {
            GUIHelper.DrawOutline(spriteBatch, new Rectangle(position.X - 4, position.Y - 4, 46, 46), ThemeHandler.ButtonColor.InvertColor());

            Texture2D tex = DLIntegration.GlowAlpha.Value;
            if (tex == null) return;

            Color color = new(255, 215, 150);
            color.A = 0;
            var target = new Rectangle(position.X, position.Y, 38, 38);

            spriteBatch.Draw(tex, target, color);
        }
    }
}