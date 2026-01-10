
using DragonLens.Core.Systems.ThemeSystem;
using DragonLens.Core.Systems.ToolSystem;
using DragonLens.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.AdminTools.AdminManagerTool;
using PvPAdventure.Core.AdminTools.StartGameTool;
using PvPAdventure.System;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Integrations.DragonLens.Tools;

[JITWhenModsEnabled("DragonLens")]
[ExtendsFromMod("DragonLens")]
public class DLAdminManagerTool : Tool
{
    public override string IconKey => DLIntegration.AdminManagerKey;
    public override string DisplayName => Language.GetTextValue("Mods.PvPAdventure.Tools.DLAdminManagerTool.DisplayName");
    public override string Description => GetDescription();
    private string GetDescription()
    {
        var gm = ModContent.GetInstance<GameManager>();
        string localizationKey = "Mods.PvPAdventure.Tools.DLAdminManagerTool.Description";
        return Language.GetTextValue(localizationKey);
    }

    public override void OnActivate()
    {
        //if (Main.netMode == NetmodeID.SinglePlayer)
        //{
            //Main.NewText("Cannot open Admin Manager in SP.", Color.Red);
            //return;
        //}

        var am = ModContent.GetInstance<AdminManagerSystem>();
        if (am == null)
        {
            Main.NewText("Failed to open AdminManagerSystem: System not found.", Color.Red);
            return;
        }

        am.ToggleActive();
    }

    public override void DrawIcon(SpriteBatch spriteBatch, Rectangle position)
    {
        base.DrawIcon(spriteBatch, position);

        var gms = ModContent.GetInstance<AdminManagerSystem>();

        if (gms.IsActive())
        {
            GUIHelper.DrawOutline(spriteBatch, new Rectangle(position.X - 4, position.Y - 4, 46, 46), ThemeHandler.ButtonColor.InvertColor());

            Texture2D tex = DLIntegration.GlowAlpha.Value;
            if (tex == null) return;

            Color color = new(255, 215, 150, 0);
            color.A = 0;
            var target = new Rectangle(position.X, position.Y, 38, 38);

            spriteBatch.Draw(tex, target, color);
        }
    }
}