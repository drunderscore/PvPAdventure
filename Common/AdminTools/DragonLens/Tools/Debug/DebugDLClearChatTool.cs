using DragonLens.Core.Systems.ThemeSystem;
using DragonLens.Core.Systems.ToolSystem;
using DragonLens.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.AdminTools.DragonLens;
using PvPAdventure.Common.GameTimer;
using PvPAdventure.Core.Net;
using System;
using System.Reflection;
using Terraria;
using Terraria.Chat;
using Terraria.GameContent.UI.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using static PvPAdventure.Common.GameTimer.GameTimerNetHandler;

namespace PvPAdventure.Common.AdminTools.DragonLens.Tools;

#if DEBUG
[JITWhenModsEnabled("DragonLens")]
[ExtendsFromMod("DragonLens")]
public class DebugDLClearChatTool : Tool
{
    public override string IconKey => "NoIconForThisDebugTool";

    public override string DisplayName => "Clear chat";

    public override string Description => "Clears the Terraria chat";

    public override void OnActivate()
    {
        // Clear Main.chatMonitor messages
        var clearMethod = Main.chatMonitor.GetType().GetMethod("Clear", BindingFlags.Instance | BindingFlags.Public);

        // Try to clear in 3 different ways but only 30 empty messages works lol.
        if (clearMethod != null)
        {
            // attempt 1
            (Main.chatMonitor as RemadeChatMonitor)?.Clear();

            // attempt 2
            clearMethod?.Invoke(Main.chatMonitor, null);

            // attempt 3
            for (int i = 0; i < 30; i++)
            {
                ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(string.Empty), Color.White);
            }
            Log.Chat("Chat cleared!!");
        }
    }

    public override void DrawIcon(SpriteBatch spriteBatch, Rectangle position)
    {
        base.DrawIcon(spriteBatch, position);

        var pm = ModContent.GetInstance<PauseManager>();

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
        var item = ContentSamples.ItemsByType[ItemID.AnnouncementBox];
        var pos = position.Center.ToVector2();

        ItemSlot.DrawItemIcon(item, ItemSlot.Context.InventoryItem, sb, pos, 0.8f, Math.Min(position.Width, position.Height), Color.White);
    }
}
#endif