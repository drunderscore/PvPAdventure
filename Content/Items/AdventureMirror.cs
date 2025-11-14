using Microsoft.Xna.Framework;
using PvPAdventure;
using PvPAdventure.Core.Features.SpawnSelector.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Content.Items;

internal class AdventureMirror : ModItem
{
    public override void SetDefaults()
    {
        var config = ModContent.GetInstance<AdventureConfig>();
        int recallFrames = config.AdventureMirrorRecallFrames; // 5 seconds = 60 * 5

        Item.useTime = 10;
        Item.useAnimation = 10;
        Item.useStyle = ItemUseStyleID.HoldUp;
        Item.UseSound = SoundID.Item6;
        Item.rare = ItemRarityID.Blue;
        Item.value = Item.buyPrice(gold: 5);
        Item.noUseGraphic = false;
    }

    public override bool CanUseItem(Player player)
    {
        if (player.velocity.Length() > 0f)
        {
            PopupText.NewText(new AdvancedPopupRequest
            {
                Color = Color.Crimson,
                Text = "Cannot use while moving!",
                Velocity = new(0f, -4f),
                DurationInFrames = 60
            }, player.Top);
            return false;
        }

        // Don’t allow starting a second mirror while one is active
        var mp = player.GetModPlayer<AdventureMirrorPlayer>();
        if (mp.MirrorActive)
            return false;

        return true;
    }

    public override bool? UseItem(Player player)
    {
        var config = ModContent.GetInstance<AdventureConfig>();
        int recallFrames = config.AdventureMirrorRecallFrames;

        player.GetModPlayer<AdventureMirrorPlayer>()
              .StartMirrorUse(recallFrames);

        return true;
    }
}
