using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Items;

/// <summary>
/// Prevents campfires from being toggled via right-click
/// </summary>
public class PreventCampfireToggleSystem : ModSystem
{
    public override void Load() => On_Player.TileInteractionsUse += BlockCampfireInteraction;
    public override void Unload() => On_Player.TileInteractionsUse -= BlockCampfireInteraction;

    private void BlockCampfireInteraction(On_Player.orig_TileInteractionsUse orig, Player self, int myX, int myY)
    {
        if (WorldGen.InWorld(myX, myY) && Main.tile[myX, myY].type == TileID.Campfire)
            return;

        orig(self, myX, myY);
    }
}