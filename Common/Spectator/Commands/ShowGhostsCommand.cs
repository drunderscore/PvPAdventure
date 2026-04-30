using PvPAdventure.Core.Config;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Spectator.Commands;

internal class ShowGhostsCommand : ModCommand
{
    public override string Command => "showghosts";
    public override string Description => "Toggles the config option that draws ghosts (spectators) and their nameplates";

    public override CommandType Type => CommandType.Chat;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        var clientConfig = ModContent.GetInstance<ClientConfig>();
        clientConfig.DrawSpectators = !clientConfig.DrawSpectators;
        clientConfig.SaveChanges(clientConfig);
    }
}
