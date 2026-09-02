using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Combat.EJ;

/// <summary>
/// Fixes that Player.GrabItems runs every tick for every active Player object, including corpses
/// </summary>
public sealed class DeadPlayerGrabItemsFix : ModSystem
{
    public override void Load() => On_Player.GrabItems += BlockGrabItemsWhileDead;
    public override void Unload() => On_Player.GrabItems -= BlockGrabItemsWhileDead;

    private void BlockGrabItemsWhileDead(On_Player.orig_GrabItems orig, Player self, int i)
    {
        if (self.dead)
            return;

        orig(self, i);
    }
}