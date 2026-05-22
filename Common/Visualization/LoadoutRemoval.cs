using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Visualization;

/// <summary>
/// Prevents players from switching loadouts
/// </summary>
internal class LoadoutRemoval : ModSystem
{
    public override void Load()
    {
        if (Main.dedServ) return;
        On_Player.TrySwitchingLoadout += OnTrySwitchingLoadout;
        On_Main.DrawLoadoutButtons += (orig, a, b, c) => { };
    }

    private static void OnTrySwitchingLoadout(On_Player.orig_TrySwitchingLoadout orig, Player self, int loadoutIndex)
    {
        if (self.CurrentLoadoutIndex != 0)
            orig(self, 0);
    }
}