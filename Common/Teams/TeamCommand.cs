using System;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Team = Terraria.Enums.Team;

namespace PvPAdventure.Common.Teams;

public class TeamCommand : ModCommand
{
    public override void Action(CommandCaller caller, string input, string[] args)
    {
        // You can only use this command from chat in singleplayer.
        if (caller.CommandType == CommandType.Chat && Main.netMode != NetmodeID.SinglePlayer)
            return;

        if (args.Length < 2)
            return;

        var player = Main.player
            .Where(player => player.active)
            .Where(player => player.name.Contains(args[0], StringComparison.CurrentCultureIgnoreCase))
            .FirstOrDefault();

        if (player == null)
            return;

        if (!Enum.TryParse(args[1], true, out Terraria.Enums.Team team) || (int)team >= Enum.GetValues<Terraria.Enums.Team>().Length)
            return;

        player.team = (int)team;
        NetMessage.SendData(MessageID.PlayerTeam, number: player.whoAmI);
    }

    public override string Command => "team";
    public override CommandType Type => CommandType.Chat | CommandType.Console;
}
