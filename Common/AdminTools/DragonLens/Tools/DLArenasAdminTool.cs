using DragonLens.Core.Systems.ThemeSystem;
using DragonLens.Core.Systems.ToolSystem;
using DragonLens.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.AdminTools.Tools.ArenasTool;
using PvPAdventure.Core.Utilities;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PvPAdventure.Common.AdminTools.DragonLens.Tools;

[JITWhenModsEnabled("DragonLens")]
[ExtendsFromMod("DragonLens")]
public sealed class DLArenasAdminTool : Tool
{
    public override string IconKey => DLToolIcons.ArenasAdminKey;
    public override string DisplayName => Language.GetTextValue("Mods.PvPAdventure.Tools.DLArenasAdminTool.DisplayName");
    public override string Description => Language.GetTextValue("Mods.PvPAdventure.Tools.DLArenasAdminTool.Description");

    public override void OnActivate()
    {
        var sys = ModContent.GetInstance<ArenasAdminSystem>();
        if (sys == null)
        {
            Main.NewText("Failed to open ArenasAdminSystem: System not found.", Color.Red);
            return;
        }

        sys.ToggleActive();
    }

    public override void DrawIcon(SpriteBatch spriteBatch, Rectangle position)
    {
        //base.DrawIcon(spriteBatch, position);
        spriteBatch.Draw(Ass.Icon_Arenas.Value, position, Color.White);

        var sys = ModContent.GetInstance<ArenasAdminSystem>();
        if (sys == null || !sys.IsActive())
            return;

        GUIHelper.DrawOutline(
            spriteBatch,
            new Rectangle(position.X - 4, position.Y - 4, 46, 46),
            ThemeHandler.ButtonColor.InvertColor());
    }
}
