using Microsoft.Xna.Framework;
using PvPAdventure.System;
using Terraria.ModLoader;
using static PvPAdventure.System.GameManager;

namespace PvPAdventure.Core.Debug;

#if DEBUG
public class DBStartGameCommand : ModCommand
{
    public override string Command => "dbstartgame";
    public override string Description => "[Debug] set GameManager.Phase=Playing";
    public override string Name => "[Debug] set GameManager.Phase=Playing";
    public override CommandType Type => CommandType.Chat | CommandType.Console;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        if (args.Length == 0 || !int.TryParse(args[0], out var time))
        {
            caller.Reply("Invalid time.", Color.Red);
            return;
        }

        var gameManager = ModContent.GetInstance<GameManager>();
        if (gameManager.CurrentPhase == Phase.Playing)
        {
            caller.Reply("The game is already being played.", Color.Red);
            return;
        }

        if (gameManager._startGameCountdown.HasValue)
        {
            caller.Reply("The game is already being started.", Color.Red);
            return;
        }

        gameManager.DebugStartGame(time);
    }
}
#endif
