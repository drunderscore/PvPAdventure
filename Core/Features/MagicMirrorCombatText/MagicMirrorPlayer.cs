using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace PvPAdventure.Core.Features.MagicMirrorCombatText
{
    public class MagicMirrorPlayer : ModPlayer
    {
        //private TimeSpan timeSinceLastText = TimeSpan.Zero;
        //private readonly TimeSpan interval = TimeSpan.FromSeconds(1); // 1 second interval
        //public bool usedMirror = false;

        public override void PostUpdate()
        {
            Item currentItem = Main.LocalPlayer.HeldItem;

            bool playerIsUsingMagicMirror = currentItem != null && 
                currentItem.type == Terraria.ID.ItemID.MagicMirror && 
                Main.LocalPlayer.itemAnimation > 0;

            if (playerIsUsingMagicMirror && Main.LocalPlayer.itemAnimation % 60 == 0)
            {
                int itemUseTimeLeft = Main.LocalPlayer.itemAnimation / 60; // Convert to seconds
                CombatText.NewText(Player.getRect(), Color.Cyan, itemUseTimeLeft); // Display over the player
            }

            //if (!usedMirror) return; 

            //// Increment timer
            //timeSinceLastText += TimeSpan.FromSeconds(1.0 / 60.0);

            //if (timeSinceLastText >= interval)
            //{
            //    timeSinceLastText = TimeSpan.Zero;

            //    if (Player.whoAmI == Main.myPlayer)
            //    {
            //    }

            //    usedMirror = false; // Reset flag after showing text once
            //}
        }
    }
}
