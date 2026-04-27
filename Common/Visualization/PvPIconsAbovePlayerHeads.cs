using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.GameTimer;
using PvPAdventure.Common.Spawnbox;
using PvPAdventure.Common.Spectator.SpectatorMode;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.Visualization;

/// <summary>
/// Draws PvP icons above player heads based on their PvP status.
/// </summary>
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
            if (!p.active)
                continue;

            var mp = p.GetModPlayer<PvPIconPlayer>();

            if (p.dead || p.ghost || SpectatorModeSystem.IsInSpectateMode(p))
            {
                mp.ShowPvPIcon = false;
                mp.PvPEnabledIconTimer = 0;
                continue;
            }

            bool inRegion = rm.GetRegionContaining(p.Center.ToTileCoordinates()) != null;

            bool wasInRegion = mp.ShowPvPIcon;

            // If they JUST left the region, start 120 tick timer.
            if (wasInRegion && !inRegion)
                mp.PvPEnabledIconTimer = 180; // 3 seconds

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
            if (Main.LocalPlayer.ghost || SpectatorModeSystem.IsInSpectateMode(Main.LocalPlayer))
                return true;

            SpriteBatch sb = Main.spriteBatch;
            int headOffset = 12;

            var pvpTex = Main.Assets.Request<Texture2D>("Images/UI/PVP_0");
            //var cooldownTex = Ass.Stop_Icon;

            float iconFullW = pvpTex.Value.Width;
            float iconFullH = pvpTex.Value.Height;
            float iconW = iconFullW / 4f;
            float iconH = iconFullH / 6f;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (!p.active || p.dead || p.ghost || SpectatorModeSystem.IsInSpectateMode(p))
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

                float alpha = 1f;
                float scale = 1f;

                if (!mp.ShowPvPIcon && mp.PvPEnabledIconTimer > 0 && mp.PvPEnabledIconTimer <= 60)
                {
                    const float peakScale = 1.42f;
                    const float popFrac = 0.22f;

                    float u = 1f - (mp.PvPEnabledIconTimer / 60f); // 0 -> 1 over the last 60 ticks

                    if (u < popFrac)
                    {
                        float k = u / popFrac;
                        k = k * k * (3f - 2f * k); // smoothstep
                        scale = MathHelper.Lerp(1f, peakScale, k);
                        alpha = 1f;
                    }
                    else
                    {
                        float v = (u - popFrac) / (1f - popFrac); // 0 -> 1
                        float inv = 1f - v;

                        scale = peakScale * inv * inv * inv * inv * inv; // fast zoom out
                        alpha = inv;                   // only fade during zoom out
                    }
                }

                Vector2 center = p.Top + new Vector2(0f, -headOffset - iconH * 0.5f) - Main.screenPosition;

                int drawW = (int)(iconW * scale);
                int drawH = (int)(iconH * scale);

                if (drawW <= 0 || drawH <= 0 || alpha <= 0f)
                    continue;

                iconRect = new(
                    (int)(center.X - drawW * 0.5f),
                    (int)(center.Y - drawH * 0.5f),
                    drawW,
                    drawH
                );

                sb.Draw(pvpTex.Value, iconRect, iconSrc, Color.White * alpha);
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
