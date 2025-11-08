using Microsoft.Xna.Framework;
using PvPAdventure.Content.Items;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Features
{
    public class RTPMirrorPlayer : ModPlayer
    {
        public override void PostUpdate()
        {
            // Check if player is using RTPMirror
            Item currentItem = Main.LocalPlayer.HeldItem;
            int rtpMirrorItemType = ModContent.ItemType<RTPMirror>();

            // Debug: item use time
            //Main.NewText(Main.LocalPlayer.itemAnimation);

            if (currentItem != null &&
                currentItem.type == rtpMirrorItemType &&
                Main.LocalPlayer.itemAnimation > 0 && 
                Main.LocalPlayer.itemAnimation % 60 == 0)
            {
                int itemUseTimeLeft = Main.LocalPlayer.itemAnimation / 60 - 1;
                string timeLeft = itemUseTimeLeft.ToString();

                // Old combat text
                //CombatText.NewText(Player.getRect(), Color.Cyan, itemUseTimeLeft);

                // Display text above the player every second
                PopupText.NewText(new AdvancedPopupRequest
                {
                    Color = Color.Crimson,
                    Text = timeLeft,
                    Velocity = new(0.0f, -4.0f),
                    DurationInFrames = 60 * 1
                }, Player.Top);
            }
        }
    }
}