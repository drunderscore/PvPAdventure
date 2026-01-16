using Microsoft.Xna.Framework;
using PvPAdventure.Common.Content.Items;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PvPAdventure.Common.SpawnSelector;

internal class AdventureMirrorPlayer : ModPlayer
{
    public override void OnHurt(Player.HurtInfo info)
    {
        base.OnHurt(info);

        // Only care if the player is currently using the AdventureMirror
        if (Player.itemTime > 0 &&
            Player.HeldItem?.type == ModContent.ItemType<AdventureMirror>())
        {
            if (Player.HeldItem.ModItem is AdventureMirror mirror)
            {
                mirror.CancelItemUse(Player);
            }

            // Show hurt popup to indicate cancellation
            if (Player.whoAmI == Main.myPlayer)
            {
                PopupText.NewText(new AdvancedPopupRequest
                {
                    Color = Color.Crimson,
                    Text = Language.GetTextValue("Mods.PvPAdventure.AdventureMirror.Cancelled"),
                    Velocity = new(0f, -4),
                    DurationInFrames = 120
                }, Player.Top + new Vector2(0, -4));
            }
        }

    }
}
