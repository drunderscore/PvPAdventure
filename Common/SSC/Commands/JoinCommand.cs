using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.SSC.Commands;

public class JoinCommand : ModCommand
{
    public override string Command => "join";

    public override string Description => "Enter the world with your Server Sided Character.";

    public override CommandType Type => CommandType.Chat;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        if (caller.Player == null || !caller.Player.active)
        {
            Main.NewText("Error: Player not found. Could not enter the world.", Color.Red);
            return;
        }

        SSCDelayJoinSystem.SendJoinRequest();
    }
}

