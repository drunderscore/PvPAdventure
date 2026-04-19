using PvPAdventure.Common.Spectator.UI.State;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Spectator._Temp;

// Make everyone spectate, or everyone players.
internal class SpectateCommand : ModCommand
{
    public override string Command => "spec";

    public override CommandType Type => CommandType.Chat;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        // Show spectate UI.
        SpectatorUISystem.ToggleSpectateJoinUI();
    }
}


internal class GhostCommand : ModCommand
{
    public override string Command => "g";

    public override CommandType Type => CommandType.Chat;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        // Toggle ghost mode.
        if (Main.LocalPlayer.ghost) 
            Main.LocalPlayer.ghost = false;
        else 
            Main.LocalPlayer.ghost = true;
    }
}
