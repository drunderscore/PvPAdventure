using PvPAdventure.Core.Config;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.World;

internal class DisableLunarApocalypse : ModSystem
{
    public override void Load()
    {
        // Prevent the world from entering the lunar apocalypse (killing cultist and spawning pillars).
        On_WorldGen.TriggerLunarApocalypse += SuppressLunarApocalypse;
    }

    public override void Unload()
    {
        On_WorldGen.TriggerLunarApocalypse -= SuppressLunarApocalypse;
    }

    private static void SuppressLunarApocalypse(On_WorldGen.orig_TriggerLunarApocalypse orig)
    {
        if (!ModContent.GetInstance<ServerConfig>().DisableLunarApocalypse)
        {
            orig();
        }
    }
}
