
using DragonLens.Core.Systems.ThemeSystem;
using DragonLens.Core.Systems.ToolSystem;
using DragonLens.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Integrations.TeamAssigner;
using PvPAdventure.Core.Helpers;
using System.Collections.Generic;
using Terraria;
using Terraria.Enums;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Integrations.DragonLens;

[JITWhenModsEnabled("DragonLens")]
[ExtendsFromMod("DragonLens")]
public class DLTeamAssignerTool : Tool
{
    public override string IconKey => DLIntegration.TeamAssignerKey;

    public override string DisplayName => "Assign teams";

    public override string Description => GetDescription();
    private string GetDescription()
    {
        // Count players on each team
        Dictionary<Team, int> counts = [];

        foreach (var p in Main.ActivePlayers)
        {
            Team team = (Team)p.team;

            if (counts.TryGetValue(team, out int value))
                counts[team] = ++value;
            else
                counts[team] = 1;
        }

        string result = "";

        foreach (var kvp in counts)
        {
            Team team = kvp.Key;
            int count = kvp.Value;

            if (count <= 0)
                continue; // skip empty

            result += $"{team}: {count}\n";
        }

        return result;
    }


    public override void OnActivate()
    {
        Log.Info("DLTeamAssignerTool activated");
        var sys = ModContent.GetInstance<TeamAssignerSystem>();
        if (sys == null)
        {
            Main.NewText("Failed to open TeamAssignerSystem: System not found.", Color.Red);
            return;
        }

        sys.ToggleActive();
    }

    public override void DrawIcon(SpriteBatch spriteBatch, Rectangle position)
    {
        base.DrawIcon(spriteBatch, position);

        var tas = ModContent.GetInstance<TeamAssignerSystem>();

        if (tas.IsActive())
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