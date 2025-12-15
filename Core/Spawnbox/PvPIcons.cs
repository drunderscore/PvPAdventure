using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Helpers;
using PvPAdventure.System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Core.Spawnbox;

[Autoload(Side = ModSide.Client)]
public class PvPIconDrawerLayer : ModSystem
{
    public override void PostUpdateEverything()
    {
        var gm = ModContent.GetInstance<GameManager>();
        var rm = ModContent.GetInstance<RegionManager>();

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player p = Main.player[i];
            if (!p.active || p.dead)
                continue;

            bool inRegion = rm.GetRegionContaining(p.Center.ToTileCoordinates()) != null;

            var mp = p.GetModPlayer<PvPIconPlayer>();

            bool wasInRegion = mp.ShowPvPIcon; 

            // If they JUST left the region, start 120 tick timer.
            if (wasInRegion && !inRegion)
                mp.PvPEnabledIconTimer = 120;

            // If they re-enter, cancel the timer.
            if (inRegion)
                mp.PvPEnabledIconTimer = 0;
            else if (mp.PvPEnabledIconTimer > 0)
                mp.PvPEnabledIconTimer--;

            // show disabled while in region
            mp.ShowPvPIcon = inRegion;
        }
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int index = layers.FindIndex(layer => layer.Name == "Vanilla: Interface Logic 1");
        if (index != -1)
            layers.Insert(index + 1, new PvPIconLayer());
    }

    private sealed class PvPIconLayer : GameInterfaceLayer
    {
        public PvPIconLayer()
            : base("PvPAdventure: PvP Icons Above Player Heads", InterfaceScaleType.Game)
        {
        }

        protected override bool DrawSelf()
        {
            SpriteBatch sb = Main.spriteBatch;
            int headOffset = 12;

            var pvpTex = Main.Assets.Request<Texture2D>("Images/UI/PVP_0");
            var cooldownTex = Ass.Stop_Icon;

            float iconFullW = pvpTex.Value.Width;
            float iconFullH = pvpTex.Value.Height;
            float iconW = iconFullW / 4f;
            float iconH = iconFullH / 6f;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (!p.active || p.dead)
                    continue;

                var mp = p.GetModPlayer<PvPIconPlayer>();

                // Decide which icon frameX to draw:
                // if in spawn: 0
                // if just left spawn: 2
                int frameX = -1;

                if (mp.ShowPvPIcon)
                    frameX = 0;
                else if (mp.PvPEnabledIconTimer > 0)
                    frameX = 2;

                if (frameX == -1)
                    continue;

                Vector2 screenPos = p.Top + new Vector2(-iconW / 2f, -headOffset - iconH) - Main.screenPosition;

                Rectangle iconRect = new(
                    (int)screenPos.X,
                    (int)screenPos.Y,
                    (int)iconW,
                    (int)iconH
                );

                Rectangle iconSrc = pvpTex.Frame(
                    horizontalFrames: 4,
                    verticalFrames: 6,
                    frameX: frameX,
                    frameY: p.team
                );

                // Draw pvp icon
                sb.Draw(pvpTex.Value, iconRect, iconSrc, Color.White * 1f);
            }

            return true;
        }
    }
}

public class PvPIconPlayer : ModPlayer
{
    public bool ShowPvPIcon;
    public int PvPEnabledIconTimer; // starts when player leaves spawn
}
