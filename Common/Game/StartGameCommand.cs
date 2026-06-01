using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static PvPAdventure.Common.GameTimer.GameManager;

namespace PvPAdventure.Common.GameTimer;

public class StartGameCommand : ModCommand
{
    public override void Action(CommandCaller caller, string input, string[] args)
    {
        // You can only use this command from chat in singleplayer.
        if (caller.CommandType == CommandType.Chat && Main.netMode != NetmodeID.SinglePlayer)
            return;

        if (args.Length == 0 || !int.TryParse(args[0], out var time))
        {
            caller.Reply("Invalid time.", Color.Red);
            return;
        }

        var gm = ModContent.GetInstance<GameManager>();
        if (gm.CurrentPhase == Phase.Playing)
        {
            //caller.Reply("The game is already being played.", Color.Red);
            gm.EndGame();
            return;
        }

        if (gm._startGameCountdown.HasValue)
        {
            caller.Reply("The game is already being started.", Color.Red);
            return;
        }

        gm.StartGame(time);
    }

    public override string Command => "startgame";
    public override CommandType Type => CommandType.Chat | CommandType.Console;
}
