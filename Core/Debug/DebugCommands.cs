using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.Chat;
using Terraria.Enums;
using Terraria.GameContent.UI.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace PvPAdventure.Core.Debug;

// This is only run in debug builds.
#if DEBUG

public class ClearChatCommand : ModCommand
{
    public override string Command => "clear";
    public override CommandType Type => CommandType.World | CommandType.Server;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        // Clear Main.chatMonitor messages
        var clearMethod = Main.chatMonitor.GetType().GetMethod("Clear", BindingFlags.Instance | BindingFlags.Public);

        // Try to clear in 3 different ways but only 30 empty messages works lol.
        if (clearMethod != null)
        {
            (Main.chatMonitor as RemadeChatMonitor)?.Clear();
            clearMethod?.Invoke(Main.chatMonitor, null);
            for (int i = 0; i < 30; i++)
            {
                ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(string.Empty), Color.White);
            }
            Log.Chat("Chat cleared!!");
        }
    }
}

public class ChatTimeCommand : ModCommand
{
    public override string Command => "chattime";
    public override CommandType Type => CommandType.World | CommandType.Server;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        if (args.Length != 1 || !int.TryParse(args[0], out int seconds))
        {
            caller.Reply("Usage: /chattime <seconds>", Color.Red);
            return;
        }

        var monitor = Main.chatMonitor as RemadeChatMonitor;
        if (monitor == null)
            return;

        // Access _messages
        var messagesField = typeof(RemadeChatMonitor)
            .GetField("_messages", BindingFlags.NonPublic | BindingFlags.Instance);

        var messages = (List<ChatMessageContainer>)messagesField.GetValue(monitor);
        if (messages == null)
            return;

        // Access ChatMessageContainer._timeLeft
        var timeLeftField = typeof(ChatMessageContainer)
            .GetField("_timeLeft", BindingFlags.NonPublic | BindingFlags.Instance);

        int frames = seconds * 60;

        // Apply to all messages
        foreach (var msg in messages)
            timeLeftField.SetValue(msg, frames);

        caller.Reply($"Chat message lifetime set to {seconds} seconds.", Color.Green);
    }
}

#endif