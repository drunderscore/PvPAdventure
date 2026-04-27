using PvPAdventure.Common.Spectator.SpectatorMode;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Spectator.Visualization;

/// <summary>
/// Disables spawn rate for players who are spectators
/// </summary>
internal class DisableSpawnRateNPC : GlobalNPC
{
    public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
    {
        if (!SpectatorModeSystem.IsInSpectateMode(player))
            return;

        spawnRate = int.MaxValue;
        maxSpawns = 0;
    }
}
