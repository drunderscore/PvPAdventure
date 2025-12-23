using DragonLens.Core.Systems.ThemeSystem;
using DragonLens.Core.Systems.ToolSystem;
using DragonLens.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Spectate;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Integrations.DragonLens;

[JITWhenModsEnabled("DragonLens")]
[ExtendsFromMod("DragonLens")]
public class DLSpectateTool : Tool
{
    public override string IconKey => DLIntegration.SpectateKey;
    public override string DisplayName => "Spectate";
    public override string Description => "Allows you to move the camera to teammates positions.";
    public override void OnActivate()
    {
        var sys = ModContent.GetInstance<SpectateSystem>();
        if (sys == null)
        {
            Main.NewText("Failed to open SpectateSystem: System not found.", Color.Red);
            return;
        }

        if (sys.IsActive())
        {
            sys.ExitSpectateUI();
        }
        else
        {
            sys.ShowAllPlayers = true;
            sys.EnterSpectateUI(clearTarget: true);
        }
    }

    public override void DrawIcon(SpriteBatch spriteBatch, Rectangle position)
    {
        base.DrawIcon(spriteBatch, position);

        var ss = ModContent.GetInstance<SpectateSystem>();

        if (ss.IsActive())
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