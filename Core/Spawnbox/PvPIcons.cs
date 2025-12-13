using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Helpers;
using PvPAdventure.System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Core.Spawnbox;

/// <summary>
/// Draws PvP icons above players when players are in the spawn region.
/// </summary>
[Autoload(Side = ModSide.Client)]
public class PvPIconDrawerLayer : ModSystem
{
    public override void PostUpdateEverything()
    {
        var gm = ModContent.GetInstance<GameManager>();
        var rm = ModContent.GetInstance<RegionManager>();

        // Iterate all players.
        // Set the conditional PvP flag if player is in spawn box and the game is live.
        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player p = Main.player[i];
            if (!p.active || p.dead)
                continue;

            bool inRegion = rm.GetRegionContaining(p.Center.ToTileCoordinates()) != null;
            bool isPlaying = gm.CurrentPhase == GameManager.Phase.Playing;
            bool shouldDisablePvP = inRegion;

            var mp = p.GetModPlayer<PvPIconPlayer>();
            if (mp.ShowPvPIcon != shouldDisablePvP)
            {
                mp.ShowPvPIcon = shouldDisablePvP;
            }
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
            : base("PvPAdventure:PvPIconLayer", InterfaceScaleType.Game)
        {
        }

        protected override bool DrawSelf()
        {
            // Check playing status
            var gm = ModContent.GetInstance<GameManager>();
            bool isPlaying = gm.CurrentPhase == GameManager.Phase.Playing;

            SpriteBatch sb = Main.spriteBatch;
            int headOffset = 12; // distance above head

            // Setup assets
            var pvpTex = Main.Assets.Request<Texture2D>("Images/UI/PVP_0");
            //var cooldownTex = Main.Assets.Request<Texture2D>("Images/CoolDown"); // old asset
            var cooldownTex = Ass.Stop_Icon;

            float iconFullW = pvpTex.Value.Width;
            float iconFullH = pvpTex.Value.Height;
            float iconW = iconFullW / 4f;
            float iconH = iconFullH / 6f;

            // Iterate players and draw PvP icon if 
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (!p.active || p.dead)
                    continue;

                // Skip self
                //if (p.whoAmI == Main.myPlayer)
                    //continue;

                if (!p.GetModPlayer<PvPIconPlayer>().ShowPvPIcon)
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
                    frameX: 1, 
                    frameY: p.team);

                // Draw PvP icon
                sb.Draw(pvpTex.Value, iconRect, iconSrc, Color.White * 1f);

                float cooldownWidth = iconW;
                float cooldownHeight = iconH;

                Rectangle cdRect = new(
                    (int)(screenPos.X + (iconW - cooldownWidth) * 0.5f),
                    (int)(screenPos.Y + (iconH - cooldownHeight) * 0.5f-2),
                    (int)cooldownWidth,
                    (int)cooldownHeight
                );

                // Draw cooldown icon
                //if (isPlaying)
                sb.Draw(cooldownTex.Value, cdRect, Color.White * 1f);
            }

            return true;
        }
    }
}

public class PvPIconPlayer : ModPlayer
{
    public bool ShowPvPIcon;
}

