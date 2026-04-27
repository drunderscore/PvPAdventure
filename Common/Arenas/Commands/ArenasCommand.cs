using PvPAdventure.Common.Arenas.UI;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Arenas;

internal sealed class ArenasCommand : ModCommand
{
    public override string Command => "arenas";
    public override CommandType Type => CommandType.Chat;
    public override string Description => "Toggles the arenas UI.";

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        ArenasUISystem.Toggle();
    }
}