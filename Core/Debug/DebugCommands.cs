using Microsoft.Xna.Framework;
using PvPAdventure.System;
using Terraria;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Debug;

// Only run in debug builds.
#if DEBUG
public class DBStartCommand : ModCommand
{
    public override string Command => "dbstart";
    public override CommandType Type => CommandType.World | CommandType.Server;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        var gm = ModContent.GetInstance<GameManager>();
        gm.StartGame(time: 60000, countdownTimeInSeconds: 0);
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