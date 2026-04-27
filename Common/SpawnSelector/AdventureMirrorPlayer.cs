using Microsoft.Xna.Framework;
using PvPAdventure.Content.Items;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PvPAdventure.Common.SpawnSelector;

internal class AdventureMirrorPlayer : ModPlayer
{
    public override void OnHurt(Player.HurtInfo info)
    {
        base.OnHurt(info);

        if (Player.itemTime <= 0 ||
            Player.HeldItem?.type != ModContent.ItemType<AdventureMirror>() ||
            Player.GetModPlayer<SpawnPlayer>().SpawnedPortalThisUse)
            return;

        if (Player.HeldItem.ModItem is AdventureMirror mirror)
            mirror.CancelItemUse(Player);

        if (Player.whoAmI == Main.myPlayer)
            PopupText.NewText(new AdvancedPopupRequest
            {
                Color = Color.Crimson,
                Text = Language.GetTextValue("Mods.PvPAdventure.AdventureMirror.Cancelled"),
                Velocity = new(0f, -4),
                DurationInFrames = 120
            }, Player.Top + new Vector2(0, -4));
    }
}
