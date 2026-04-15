using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;

namespace TerraBlaze.Core.Debug;

#if DEBUG
internal sealed class DebugDrawerSystem : ModSystem
{
    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int index = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
        if (index < 0)
        {
            return;
        }

        layers.Insert(index, new LegacyGameInterfaceLayer(
            "TerraBlaze: Debug UI",
            () =>
            {
                DebugDrawer.DrawButtons();
                DebugDrawer.DrawDebugInfo();
                DebugDrawer.Flush(Main.spriteBatch);
                return true;
            },
            InterfaceScaleType.UI));
    }
}
#endif
