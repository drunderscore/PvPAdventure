using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.World;

internal class DisableLunarApocalypse : ModSystem
{
    public override void Load()
    {
        // Prevent the world from entering the lunar apocalypse (killing cultist and spawning pillars)
        On_WorldGen.TriggerLunarApocalypse += _ => { };
    }
}
