using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Reflection;
using Terraria.GameContent.UI.Chat;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace PvPAdventure.Core.Debug;

internal class DebugChat : ModSystem
{
#if DEBUG
    public override void Load()
    {
        On_RemadeChatMonitor.AddNewMessage += OnAddNewChatMessage;
    }
#endif

    public override void Unload()
    {
        On_RemadeChatMonitor.AddNewMessage -= OnAddNewChatMessage;
    }

    private void OnAddNewChatMessage(On_RemadeChatMonitor.orig_AddNewMessage orig, RemadeChatMonitor self, string text, Color color, int widthLimitInPixels)
    {
        orig(self, text, color, widthLimitInPixels);

        // Increase chat visible lines to 20
        var showCountField = typeof(RemadeChatMonitor).GetField("_showCount", BindingFlags.NonPublic | BindingFlags.Instance);
        showCountField?.SetValue(self, 20);

        // Access _messages
        var messagesField = typeof(RemadeChatMonitor).GetField("_messages", BindingFlags.NonPublic | BindingFlags.Instance);
        var messages = (List<ChatMessageContainer>)messagesField.GetValue(self);

        // Null check
        if (messages == null || messages.Count == 0)
            return;

        // Extend message lifetime to 60 seconds
        var msg = messages[0];
        var timeLeftField = typeof(ChatMessageContainer).GetField("_timeLeft", BindingFlags.NonPublic | BindingFlags.Instance);
        timeLeftField?.SetValue(msg, 60 * 60);
    }
}
