using PvPAdventure.Core.Arenas.UI.JoinUI;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Arenas.UI;

public class ArenasCommand : ModCommand
{
    public override string Command => "arenas";

    public override CommandType Type => CommandType.Chat;
    public override string Description => "Toggle the arenas join UI";
    public override void Action(CommandCaller caller, string input, string[] args)
    {
        // This command is only meaningful on the client
        if (Main.netMode == NetmodeID.Server)
        {
            return;
        }

        // Toggle the UI
        ArenasJoinUISystem.Toggle();
    }
}
