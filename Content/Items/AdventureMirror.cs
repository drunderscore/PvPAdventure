using Microsoft.Xna.Framework;
using PvPAdventure.Core.Features.SpawnSelector.Systems;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Content.Items;

internal class AdventureMirror : ModItem
{
    public override bool CanUseItem(Player player)
    {
        // Prevent item use if the player is moving
        if (player.velocity.Length() > 0f)
        {
            // Display text above the player every second
            PopupText.NewText(new AdvancedPopupRequest
            {
                Color = Color.Crimson,
                Text = "Cannot use while moving!",
                Velocity = new(0.0f, -4.0f),
                DurationInFrames = 60 * 1
            }, player.Top);
            return false;
        }

        return base.CanUseItem(player);
    }

    public override bool? UseItem(Player player)
    {
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
        Item.UseSound = SoundID.Item6; // MAGIC MIRROR SOUND
        Item.rare = ItemRarityID.Blue;
        Item.value = Item.buyPrice(gold: 5);
        Item.noUseGraphic = false;
    }

    // UseStyle is called each frame that the item is being actively used.
    public override void UseStyle(Player player, Rectangle heldItemFrame)
    {
        // Only run for the client who is actually using the item.
        if (player.whoAmI != Main.myPlayer)
            return;

        if (Main.rand.NextBool())
        {
            SpawnMirrorDust(player);
        }

        // debug 
        //Main.NewText(player.itemTime);

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
                SoundEngine.PlaySound(SoundID.MenuOpen);
                Main.mapFullscreen = true; // open the fullscreen map

                //Main.resetMapFull = true; // reset the map view position to be zoomed out and centered on the player

                // center map
                float worldCenterX = Main.maxTilesX / 2f;
                float worldCenterY = Main.maxTilesY / 2f;
                Main.mapFullscreenPos.X = worldCenterX;
                Main.mapFullscreenPos.Y = worldCenterY;

                // zoom out
                Main.mapFullscreenScale = 0.21f;

                // teleport to spawn
                Vector2 spawnPos = new(Main.spawnTileX*16, Main.spawnTileY*16-100);

                Main.LocalPlayer.Teleport(spawnPos);

                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    // ?
                }
            }
            SpawnSelectorSystem.SetEnabled(true);

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
        //if (!Main.mapFullscreen)
        //{
        //    if (!SpawnSelectorSettings.GetIsEnabled())
        //    {
        //        SpawnSelectorSettings.SetIsEnabled(false);
        //    }
        //}

        if (player.HeldItem.type == Item.type && player.itemAnimation > 0)
        {
            if (player.velocity.Length() > 0f)
            {
                // Cancel item use
                player.itemAnimation = 0;
                player.itemTime = 0;

                // Display text above the player every second
                PopupText.NewText(new AdvancedPopupRequest
                {
                    Color = Color.Crimson,
                    Text = "Cancelled!",
                    Velocity = new(0.0f, -4.0f),
                    DurationInFrames = 60 * 1
                }, player.Top);
            }
        }
    }
}