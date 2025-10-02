using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.System;

[Autoload(Side = ModSide.Client)]
public class RandomTeleportManager : ModSystem
{
    private readonly RandomTeleportGameInterfaceLayer _randomTeleportGameInterfaceLayer = new();

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        if (!layers.Contains(_randomTeleportGameInterfaceLayer))
        {
            var layerIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Settings Button");

            if (layerIndex != -1)
                layers.Insert(layerIndex + 1, _randomTeleportGameInterfaceLayer);
        }
    }

    private static void RandomTeleport()
    {
        if (Main.netMode == NetmodeID.SinglePlayer)
            Main.LocalPlayer.TeleportationPotion();
        else
            NetMessage.SendData(MessageID.RequestTeleportationByServer);
    }

    private class RandomTeleportGameInterfaceLayer()
        : GameInterfaceLayer("PvPAdventure: Random Teleport", InterfaceScaleType.UI)
    {
        private bool _mouseOver;
        private float _scale = 0.8f;

        protected override bool DrawSelf()
        {
            var hitbox = Main.LocalPlayer.Hitbox;
            var tileHitbox = new Rectangle(hitbox.X / 16, hitbox.Y / 16, hitbox.Width / 16, hitbox.Height / 16);

            if (Main.playerInventory &&
                ModContent.GetInstance<RegionManager>().GetRegionsIntersecting(tileHitbox)
                    .Any(region => region.CanRandomTeleport))
            {
                Main.DrawSettingButton(ref _mouseOver, ref _scale, Main.screenWidth / 2, Main.screenHeight - 20,
                    "Random Teleport", "Random Teleport", RandomTeleport);
            }

            return base.DrawSelf();
        }
    }
}