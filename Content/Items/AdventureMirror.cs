using Discord;
using Microsoft.Xna.Framework;
using PvPAdventure.Core.Features;
using PvPAdventure.Core.Helpers;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static PvPAdventure.Core.Features.TeleportMapSystem;

namespace PvPAdventure.Content.Items;

internal class AdventureMirror : ModItem
{
    public override bool CanUseItem(Player player)
    {
        // Prevent item use if the player is moving
        if (player.velocity.Length() > 0f)
        {
            return false;
        }

        return base.CanUseItem(player);
    }

    public override bool? UseItem(Player player)
    {
        Log.Debug("Adventure mirror used.");
        Log.Debug("is rtp menu enabled: " + RTPSpawnSelectorSettings.GetIsEnabled());

        //for (int d = 0; d < 70; d++)
        //{
        //    Dust.NewDust(player.position, player.width, player.height, DustID.MagicMirror, 0f, 0f, 150, default(Microsoft.Xna.Framework.Color), 1.5f);
        //}

        return true;
    }

    public override void SetDefaults()
    {
        //Item.CloneDefaults(ItemID.MagicMirror);

        var config = ModContent.GetInstance<AdventureConfig>();
        int recallFrames = config.RTPRecallFrames;

        Item.useTime = recallFrames;
        Item.useAnimation = recallFrames;
        Item.useStyle = ItemUseStyleID.HoldUp;
        Item.UseSound = SoundID.Item6;
        Item.rare = ItemRarityID.Blue;
        Item.value = Item.buyPrice(gold: 5);
        Item.noUseGraphic = false;
    }

    // public override void HoldItem(Player player)
    // {
    //     if (player.itemAnimation == 1 && player.HeldItem.type == Item.type)
    //     {
    //         Log.Debug("Adventure mirror time is up, opening map!");

    //         // This is the last frame of use animation
    //         if (Main.myPlayer == player.whoAmI)
    //         {
    //             Main.mapFullscreen = true;
    //             RTPSpawnSelectorSettings.SetIsEnabled(true);

    //             Log.Debug("Main.mapFullscreen set to: " + Main.mapFullscreen);
    //         }
    //     }
    // }

    // UseStyle is called each frame that the item is being actively used.
    public override void UseStyle(Player player, Rectangle heldItemFrame)
    {
        if (Main.rand.NextBool())
        {
            SpawnMirrorDust(player);
        }

        // debug 
        Main.NewText(player.itemTime);

        // This sets up the itemTime correctly.
        if (player.itemTime == 0)
        {
            player.ApplyItemTime(Item);
        }
        else if (player.itemTime <= 10)
        {
            Main.playerInventory = false;
            if (!Main.mapFullscreen)
            {
                Main.NewText("Opened RTP spawn options at <10 frames");
                Main.mapFullscreen = true;
                Main.resetMapFull = true;
            }
            RTPSpawnSelectorSettings.SetIsEnabled(true);

            // This code releases all grappling hooks and kills/despawns them.
            player.RemoveAllGrapplingHooks();
        }
    }

    private static void SpawnMirrorDust(Player player)
    {
        Dust.NewDust(
            player.position,
            player.width,
            player.height,
            DustID.MagicMirror,
            player.velocity.X * 0.5f,
            player.velocity.Y * 0.5f,
            Alpha: 150,
            newColor: default,
            Scale: 1.5f
        );
    }

    public override void UpdateInventory(Player player)
    {
        if (!Main.mapFullscreen)
        {
            if (!RTPSpawnSelectorSettings.GetIsEnabled())
            {
                RTPSpawnSelectorSettings.SetIsEnabled(false);
            }
        }

        if (player.HeldItem.type == Item.type && player.itemAnimation > 0)
        {
            if (player.velocity.Length() > 0f)
            {
                // Cancel item use
                player.itemAnimation = 0;
                player.itemTime = 0;
                //SoundEngine.PlaySound(SoundID.MenuClose, player.position);

                Log.Debug("cancelled RTP mirror");
            }
        }
    }
}