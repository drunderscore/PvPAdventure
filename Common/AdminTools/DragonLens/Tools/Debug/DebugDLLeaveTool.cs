using DragonLens.Core.Systems.ToolSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.AdminTools.DragonLens.Tools.Debug;

#if DEBUG
[JITWhenModsEnabled("DragonLens")]
[ExtendsFromMod("DragonLens")]
public class DebugDLLeaveTool : Tool
{
    public override string IconKey => "NoIconForThisDebugTool";

    public override string DisplayName => "Exit world";

    public override string Description => "Instantly leave to main menu without saving";

    public override void OnActivate()
    {
        WorldGen.JustQuit();
    }

    public override void DrawIcon(SpriteBatch spriteBatch, Rectangle position)
    {
        base.DrawIcon(spriteBatch, position);

        //var pm = ModContent.GetInstance<PauseManager>();

        //if (pm.IsPaused)
        //{
        //    GUIHelper.DrawOutline(spriteBatch, new Rectangle(position.X - 4, position.Y - 4, 46, 46), ThemeHandler.ButtonColor.InvertColor());

        //    Texture2D tex = DLToolIcons.GlowAlpha.Value;
        //    if (tex == null) return;

        //    Color color = new(255, 215, 150);
        //    color.A = 0;
        //    var target = new Rectangle(position.X, position.Y, 38, 38);

        //    spriteBatch.Draw(tex, target, color);
        //}
    public override void DrawIcon(SpriteBatch sb, Rectangle position)
    {
        //base.DrawIcon(sb, position);

        // Draw wooden door icon
        var item = ContentSamples.ItemsByType[ItemID.WoodenDoor];
        var pos = position.Center.ToVector2();

        ItemSlot.DrawItemIcon(item, ItemSlot.Context.InventoryItem, sb, pos, 1f, Math.Min(position.Width, position.Height), Color.White);
    }
}
#endif