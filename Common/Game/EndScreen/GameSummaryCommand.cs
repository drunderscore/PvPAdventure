using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Game.EndScreen;

/// <summary>Re-opens the most recent end screen summary on the local client.</summary>
public class GameSummaryCommand : ModCommand
{
    public override string Command => "gamesummary";
    public override string Description => "Re-open the most recent match summary screen.";
    public override CommandType Type => CommandType.Chat;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        // Chat commands on a server have no client UI to show.
        if (Main.netMode == NetmodeID.Server)
            return;

        if (!ModContent.GetInstance<EndScreenSystem>().ReshowLastSummary())
            caller.Reply("No summary available — no game has ended yet.", Color.Red);
    }
}
