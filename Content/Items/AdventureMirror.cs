using Microsoft.Xna.Framework;
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
        var adventureMirrorPlayer = player.GetModPlayer<AdventureMirrorPlayer>();

        if (adventureMirrorPlayer.CancelIfPlayerMoves())
            return false;

        if (adventureMirrorPlayer.MirrorActive)
            return false;

        return true;
    }

    public override bool CanRightClick() => true;
    public override bool ConsumeItem(Player player) => false;

    public override void RightClick(Player player)
    {
        var adventureMirrorPlayer = player.GetModPlayer<AdventureMirrorPlayer>();

        if (adventureMirrorPlayer.CancelIfPlayerMoves())
            return;

        if (adventureMirrorPlayer.MirrorActive)
            return;

        var config = ModContent.GetInstance<AdventureConfig>();
        int recallFrames = config.AdventureMirrorRecallFrames + 1;

        adventureMirrorPlayer.StartMirrorUse(recallFrames);
    }

    public override bool? UseItem(Player player)
    {
        var config = ModContent.GetInstance<AdventureConfig>();
        int recallFrames = config.AdventureMirrorRecallFrames + 1;

        var mp = player.GetModPlayer<AdventureMirrorPlayer>();
        mp.StartMirrorUse(recallFrames);

        return true;
    }
}
