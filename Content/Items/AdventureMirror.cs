using Microsoft.Xna.Framework;
using PvPAdventure.Core.SpawnAndSpectate;
using PvPAdventure.System;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PvPAdventure.Content.Items;

/// <summary>
/// An item that teleports the player to world spawn.
/// Has a countdown of a few seconds before being used.
/// </summary>
internal class AdventureMirror : ModItem
{
    public override string Texture => $"PvPAdventure/Assets/Item/AdventureMirror";
    public override void SetDefaults()
    {
        //Item.CloneDefaults(ItemID.MagicMirror);

        var config = ModContent.GetInstance<AdventureServerConfig>();
        int recallFrames = config.AdventureMirrorRecallFrames; // 5 seconds = 60 * 5

        Item.useTime = recallFrames + 3; // + a few frames to ensure countdown shows
        Item.useAnimation = recallFrames + 3; // + a few frames to ensure countdown 
        Item.useStyle = ItemUseStyleID.HoldUp;
        Item.UseSound = SoundID.Item6;
        Item.rare = ItemRarityID.Blue;
        Item.value = Item.buyPrice(gold: 5);
        Item.noUseGraphic = false;
    }
    public override bool ConsumeItem(Player player) => false;
    public override bool? UseItem(Player player) => true;

    #region Right click use
    public override bool CanRightClick() => true;
    public override bool AltFunctionUse(Player player) => false;

    public override void RightClick(Player player)
    {
        // Redundant check, just to be sure we don't allow any shenanigans
        var gm = ModContent.GetInstance<GameManager>();
        if (gm.CurrentPhase != GameManager.Phase.Playing)
        {
            // Check if the config allows popup text
            var config = ModContent.GetInstance<AdventureClientConfig>();
            if (!config.ShowPopupText)
                return;

            // Create and display the popup text (locally)
            if (player.whoAmI == Main.myPlayer)
            {
                PopupText.NewText(new AdvancedPopupRequest
                {
                    Color = Color.Crimson,
                    Text = "wait until game starts!",
                    Velocity = new(0f, -4),
                    DurationInFrames = 120
                }, player.Top + new Vector2(0, -4));
            }
            return;
        }

        if (player.whoAmI != Main.myPlayer)
            return;

        // Find the slot of this item
        int index = -1;
        for (int i = 0; i < player.inventory.Length; i++)
        {
            if (player.inventory[i] == Item)
            {
                index = i;
                break;
            }
        }
        if (index == -1)
            return;

        if (player.CCed || player.itemAnimation > 0 || player.reuseDelay > 0)
            return;

        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            player.selectedItem = index;
            player.controlUseItem = true;
            player.releaseUseItem = true;
            player.ItemCheck();
            player.controlUseItem = false;
            player.releaseUseItem = false;
            return;
        }

        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            ModPacket p = Mod.GetPacket();
            p.Write((byte)AdventurePacketIdentifier.AdventureMirrorRightClickUse);
            p.Write((byte)player.whoAmI);
            p.Write((byte)index);
            p.Send(); // to server
        }
    }
    #endregion

    public override bool CanUseItem(Player player)
    {
        // Redundant check, just to be sure we don't allow any shenanigans
        var gm = ModContent.GetInstance<GameManager>();
        if (gm.CurrentPhase != GameManager.Phase.Playing)
        {
            // Check if the config allows popup text
            var config = ModContent.GetInstance<AdventureClientConfig>();
            if (!config.ShowPopupText)
                return false;

            // Create and display the popup text (locally)
            if (player.whoAmI == Main.myPlayer)
            {
                PopupText.NewText(new AdvancedPopupRequest
                {
                    Color = Color.Crimson,
                    Text = Language.GetTextValue("Mods.PvPAdventure.AdventureMirror.GameNotStarted"),
                    Velocity = new(0f, -4),
                    DurationInFrames = 120
                }, player.Top + new Vector2(0, -4));
            }
            return false;

        }

        // If this player moves, cancel their use
        if (player.velocity.LengthSquared() > 0)
        {
            // Create and display the popup text (locally)
            if (player.whoAmI == Main.myPlayer)
            {
                PopupText.NewText(new AdvancedPopupRequest
                {
                    Color = Color.Crimson,
                    Text = Language.GetTextValue("Mods.PvPAdventure.AdventureMirror.CannotUseWhileMoving"),
                    Velocity = new(0f, -4),
                    DurationInFrames = 120
                }, player.Top + new Vector2(0, -4));
            }
            return false;
        }

        return true;
    }

    internal void CancelItemUse(Player player)
    {
        player.controlUseItem = false;
        player.channel = false;
        player.itemAnimation = 0;
        player.itemTime = 0;
        player.reuseDelay = 0;
    }

    public override void UseStyle(Player player, Rectangle heldItemFrame)
    {
        base.UseStyle(player, heldItemFrame);

        //if (ModContent.GetInstance<SpawnPointPlayer>().IsPlayerInSpawnRegion())
        //{
        //    if (player.whoAmI == Main.myPlayer)
        //    {
        //        PopupText.NewText(new AdvancedPopupRequest
        //        {
        //            Color = Color.Crimson,
        //            Text = Language.GetTextValue("Mods.PvPAdventure.AdventureMirror.CannotUseInSpawn"),
        //            Velocity = new(0f, -4),
        //            DurationInFrames = 120
        //        }, player.Top + new Vector2(0, -4));
        //    }
        //    CancelItemUse(player);
        //    return;
        //}

        // If this player moves, cancel their use
        if (player.velocity.LengthSquared() > 0)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                PopupText.NewText(new AdvancedPopupRequest
                {
                    Color = Color.Crimson,
                    Text = Language.GetTextValue("Mods.PvPAdventure.AdventureMirror.Cancelled"),
                    Velocity = new(0f, -4),
                    DurationInFrames = 120
                }, player.Top + new Vector2(0, -4));
            }
            CancelItemUse(player);
            return;
        }

        // Spawn dust for all to see
        if (Main.rand.NextBool())
            Dust.NewDust(player.position, player.width, player.height,DustID.MagicMirror, player.velocity.X * 0.5f,player.velocity.Y * 0.5f,150,default,1.5f);

        // Show countdown for all to see
        int secondsLeft = (player.itemTime + 59) / 60;
        if (player.itemTime % 60 == 0 && secondsLeft > 0)
        {
            // Check if the config allows popup text
            var config = ModContent.GetInstance<AdventureClientConfig>();
            if (!config.ShowPopupText)
                return;

            // Create and display the popup text
            PopupText.NewText(new AdvancedPopupRequest
            {
                Color = Color.MediumPurple,
                Text = secondsLeft.ToString(),
                Velocity = new(0f, -4),
                DurationInFrames = 120
            }, player.Top + new Vector2(0, -4));
            return;
        }

        // Teleport the player who used the item to their spawn
        if (player.whoAmI == Main.myPlayer && player.itemTime == 1)
        {
            TeleportToSpawn(player);
        }
    }

    private void TeleportToSpawn(Player player)
    {
        // Get player spawn pos
        int twoTilesAbove = 2 * 16;
        //Vector2 playerSpawnPos = new Vector2(player.SpawnX * 16, player.SpawnY * 16 - twoTilesAbove);
        //if (player.SpawnX == -1 && player.SpawnY == -1)
        //{
            Vector2 playerSpawnPos = new(Main.spawnTileX * 16, Main.spawnTileY * 16 - twoTilesAbove);
        //}

        player.Teleport(playerSpawnPos);
    }

    public override void UpdateInventory(Player player)
    {
        // Force favorite every tick
        // A little redundant now that we disallowed unfavorite in ItemSlotHooks left click hook
        Item.favorited = true;
    }
}
