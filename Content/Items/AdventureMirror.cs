using Microsoft.Xna.Framework;
using PvPAdventure.Core.Features.SpawnSelector.Players;
using PvPAdventure.System;
using Terraria;
using Terraria.Audio;
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

        // BLOCK BEFORE GAME STARTS
        if (!adventureMirrorPlayer.IsGameStarted())
            return false;

        // BLOCK IN SPAWN
        if (adventureMirrorPlayer.IsPlayerInSpawnRegion())
        {
            if (player.whoAmI == Main.myPlayer)
            {
                PopupText.NewText(new AdvancedPopupRequest
                {
                    Color = Color.Crimson,
                    Text = "Cannot use in spawn!",
                    Velocity = new(0f, -4f),
                    DurationInFrames = 60
                }, player.Top);
            }

            return false;
        }

        // BLOCK IF MOVING
        if (adventureMirrorPlayer.CancelIfPlayerMoves())
            return false;

        // BLOCK IF ALREADY CHANNELING
        if (adventureMirrorPlayer.MirrorActive)
            return false;

        return true;
    }

    public override bool CanRightClick() => true;
    public override bool ConsumeItem(Player player) => false;
    public override void RightClick(Player player)
    {
        var adventureMirrorPlayer = player.GetModPlayer<AdventureMirrorPlayer>();

        // BLOCK BEFORE GAME STARTS
        if (!adventureMirrorPlayer.IsGameStarted())
            return;

        // BLOCK IN SPAWN
        if (adventureMirrorPlayer.IsPlayerInSpawnRegion())
        {
            if (player.whoAmI == Main.myPlayer)
            {
                PopupText.NewText(new AdvancedPopupRequest
                {
                    Color = Color.Crimson,
                    Text = "Cannot use in spawn!",
                    Velocity = new(0f, -4f),
                    DurationInFrames = 60
                }, player.Top);
            }

            return;
        }

        if (adventureMirrorPlayer.CancelIfPlayerMoves())
            return;

        if (adventureMirrorPlayer.MirrorActive)
            return;

        var config = ModContent.GetInstance<AdventureConfig>();
        int recallFrames = config.AdventureMirrorRecallFrames + 1;

        SoundEngine.PlaySound(SoundID.Item6); // magic mirror

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
