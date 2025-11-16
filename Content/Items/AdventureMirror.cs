using PvPAdventure.Core.Features.SpawnSelector.Players;
using PvPAdventure.Core.Features.SpawnSelector.UI;
using PvPAdventure.System;
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

        Item.useTime = recallFrames;
        Item.useAnimation = recallFrames;
        Item.useStyle = ItemUseStyleID.HoldUp;
        Item.UseSound = SoundID.Item6;
        Item.rare = ItemRarityID.Blue;
        Item.value = Item.buyPrice(gold: 5);
        Item.noUseGraphic = false;
    }

    public override bool CanUseItem(Player player)
    {
        var amp = player.GetModPlayer<AdventureMirrorPlayer>();
        var gm = ModContent.GetInstance<GameManager>();

        if (gm.CurrentPhase != GameManager.Phase.Playing)
        {
            if (player.whoAmI == Main.myPlayer)
                PopupTextHelper.NewText("Cannot use before game has started!");
            return false;
        }

        if (amp.IsPlayerInSpawnRegion())
        {
            if (player.whoAmI == Main.myPlayer)
                PopupTextHelper.NewText("Cannot use in spawn region!");
            return false;
        }

        if (amp.MirrorActive)
            return false;

        if (player.whoAmI == Main.myPlayer && player.velocity.LengthSquared() > 0f)
        {
            //PopupTextHelper.NewText("Cannot use while moving!");
            return false;
        }

        return true;
    }

    public override bool AltFunctionUse(Player player) => true; 
    public override bool CanRightClick() => true;
    public override bool ConsumeItem(Player player) => false;
    public override void RightClick(Player player)
    {
        if (!player.GetModPlayer<AdventureMirrorPlayer>().TryStartMirrorChannel())
            return;
    }

    public override bool? UseItem(Player player)
    {
        var amp = player.GetModPlayer<AdventureMirrorPlayer>();
        amp.TryStartMirrorChannel();
        return true;
    }

    public override void UpdateInventory(Player player)
    {
        // Force favorite every tick
        Item.favorited = true;
    }
}
