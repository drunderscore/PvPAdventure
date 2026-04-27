using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.Spectator.Visualization;

/// <summary>
/// Vanilla: Resource Bars 	Draws health, mana, and breath bars, as well as buff icons.
/// https://github.com/tModLoader/tModLoader/wiki/Vanilla-Interface-layers-values
/// </summary>
internal class DisableGhostResourceBarsSystem : ModSystem
{
    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        if (Main.LocalPlayer?.ghost != true)
            return;

        GameInterfaceLayer layer = layers.Find(static layer => layer.Name == "Vanilla: Resource Bars");

        if (layer is not null)
            layer.Active = false;
    }
}
