using DragonLens.Core.Systems.ToolSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Reflection;
using Terraria;
using Terraria.Chat;
using Terraria.GameContent.UI.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.AdminTools.DragonLens.Tools.Debug;

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