using Microsoft.Xna.Framework;
using PvPAdventure.System;
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
            for (int i=0;i<30;i++)
            {
                ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(string.Empty), Color.White);
            }
            ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral("[DEBUG/SERVER] Chat cleared!!"), Color.White);
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

public class DBStartCommand : ModCommand
{
    public override string Command => "dbstart";
    public override CommandType Type => CommandType.World | CommandType.Server;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        var gm = ModContent.GetInstance<GameManager>();
        gm.StartGame(time: 60000, countdownTimeInSeconds: 0);
        ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral("[DEBUG/SERVER] Game started!"), Color.White);
    }
}

public class DBEndCommand : ModCommand
{
    public override string Command => "dbend"; // ends the game
    public override CommandType Type => CommandType.World | CommandType.Server;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        var gm = ModContent.GetInstance<GameManager>();
        gm._startGameCountdown = null;
        gm.TimeRemaining = 0;
        gm.CurrentPhase = GameManager.Phase.Waiting;
    }
}

public class DBTeamCommand : ModCommand
{
    public override string Command => "dbteam";

    public override CommandType Type => CommandType.World | CommandType.Server;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        // Must be a player
        if (caller.Player is null)
        {
            caller.Reply("Must be used by a player.", Color.Red);
            return;
        }

        // Only run on server / singleplayer world context
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        var player = caller.Player;

        // No args -> cycle teams 0–5
        if (args.Length == 0)
        {
            player.team = (player.team + 1) % 6; // 0 = None, 1–5 = colors
        }
        // One arg -> parse team name
        else if (args.Length == 1)
        {
            string arg = args[0].ToLowerInvariant();
            Team newTeam;

            switch (arg)
            {
                case "none":
                case "n":
                case "0":
                    newTeam = Team.None;
                    break;
                case "red":
                case "r":
                case "1":
                    newTeam = Team.Red;
                    break;
                case "green":
                case "g":
                case "2":
                    newTeam = Team.Green;
                    break;
                case "blue":
                case "b":
                case "3":
                    newTeam = Team.Blue;
                    break;
                case "yellow":
                case "y":
                case "4":
                    newTeam = Team.Yellow;
                    break;
                case "pink":
                case "p":
                case "5":
                    newTeam = Team.Pink;
                    break;

                default:
                    caller.Reply(
                        "Error: invalid team. Use none, red, green, blue, yellow, or pink.",
                        Color.Red
                    );
                    return;
            }

            player.team = (int)newTeam;
        }
        else
        {
            caller.Reply(
                "Usage: /dbteam [none|red|green|blue|yellow|pink]",
                Color.Red
            );
            return;
        }

        // Sync team to all clients
        NetMessage.SendData(MessageID.PlayerTeam, -1, -1, null, player.whoAmI);

        caller.Reply(
            $"Switched team to {(Team)player.team}.",
            Color.Green
        );
    }
}

#endif