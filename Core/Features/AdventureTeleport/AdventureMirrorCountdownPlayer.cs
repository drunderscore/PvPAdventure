using Microsoft.Xna.Framework;
using PvPAdventure.Content.Items;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Features.AdventureTeleport
{
    public class AdventureMirrorCountdownPlayer : ModPlayer
    {
        public override void PostUpdate()
        {
            if (Main.dedServ || Player.whoAmI != Main.myPlayer)
            {
                return;
            }

            // Check if player is using RTPMirror
            Item currentItem = Main.LocalPlayer.HeldItem;
            int rtpMirrorItemType = ModContent.ItemType<AdventureMirror>();

            if (currentItem != null &&
                currentItem.type == rtpMirrorItemType &&
                Main.LocalPlayer.itemAnimation > 0 && 
                Main.LocalPlayer.itemAnimation % 60 == 0)
            {
                int itemUseTimeLeft = Main.LocalPlayer.itemAnimation / 60;
                string timeLeft = itemUseTimeLeft.ToString();

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