using Microsoft.Xna.Framework;
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
        //Item.CloneDefaults(ItemID.MagicMirror);

        var config = ModContent.GetInstance<AdventureConfig>();
        int recallFrames = config.AdventureMirrorRecallFrames; // 5 seconds = 60 * 5

        Item.useTime = recallFrames + 1;
        Item.useAnimation = recallFrames + 1;
        Item.useStyle = ItemUseStyleID.HoldUp;
        Item.UseSound = SoundID.Item6;
        Item.rare = ItemRarityID.Blue;
        Item.value = Item.buyPrice(gold: 5);
        Item.noUseGraphic = false;
    }
    public override bool ConsumeItem(Player player) => false;

    #region Right click use
    public override bool AltFunctionUse(Player player) => true; 
    public override bool CanRightClick() => true;

    public override void RightClick(Player player)
    {
        // Find this item in the player's inventory
        int index = -1;
        for (int i = 0; i < player.inventory.Length; i++)
        {
            if (player.inventory[i] == Item)  // 'Item' is this ModItem's backing Item
            {
                index = i;
                break;
            }
        }

        if (index == -1)
            return;

        // Make it the selected hotbar item
        player.selectedItem = index;

        // Simulate pressing the use button so vanilla + your UseStyle/UseItem run
        player.controlUseItem = true;
        player.ItemCheck();   // runs use logic, respects useTime/useAnimation
    }
    #endregion
    public override bool CanUseItem(Player player)
    {
        // If this player moves, cancel their use
        if (player.velocity.LengthSquared() > 0)
        {
            //if (player.whoAmI == Main.myPlayer)
                //PopupTextHelper.NewText("stay still!", player);
            return false;
        }

        // Redundant check, just to be sure we don't allow any shenanigans
        var gm = ModContent.GetInstance<GameManager>();
        if (gm.CurrentPhase != GameManager.Phase.Playing)
        {
            if (player.whoAmI == Main.myPlayer)
                PopupTextHelper.NewText("wait until game starts!", player);
            return false;
        }

        return true;
    }

    private void CancelItemUse(Player player)
    {
        player.controlUseItem = false;
        player.channel = false;
        player.itemAnimation = 0;
        player.itemTime = 0;
        player.reuseDelay = 0;
    }

    public bool IsPlayerInSpawnRegion(Player player)
    {
        // Is player in resin's spawn region?
        var regionManager = ModContent.GetInstance<RegionManager>();
        Point tilePos = player.Center.ToTileCoordinates();
        var region = regionManager.GetRegionContaining(tilePos);
        if (region != null)
        {
            return true;
        }

        // Is player in bed spawn region?
        Vector2 bedSpawnPoint = new(player.SpawnX, player.SpawnY);
        float distanceToBedSpawn = Vector2.Distance(bedSpawnPoint * 16f, player.Center);
        if (distanceToBedSpawn <= 25 * 16f)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Main logic for using the Adventure Mirror
    /// </summary>
    public override void UseStyle(Player player, Rectangle heldItemFrame)
    {
        base.UseStyle(player, heldItemFrame);

        if (IsPlayerInSpawnRegion(player))
        {
            //PopupTextHelper.NewText("cannot use in spawn!", player);
            //CancelItemUse(player);
            //return;
        }

        // If this player moves, cancel their use
        if (player.velocity.LengthSquared() > 0)
        {
            //if (player.whoAmI == Main.myPlayer)
                //PopupTextHelper.NewText("stay still!", player);
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
            PopupTextHelper.NewText(secondsLeft.ToString(), player, Color.GreenYellow);
        }

        // Teleport the player who used the item to their spawn and open their map 
        if (player.whoAmI == Main.myPlayer && player.itemTime == 1)
        {
            TeleportToSpawn(player);
            OpenFullscreenMap();
        }
    }

    private void TeleportToSpawn(Player player)
    {
        // Get player spawn pos
        int twoTilesAbove = 2 * 16;
        Vector2 playerSpawnPos = new Vector2(player.SpawnX * 16, player.SpawnY * 16 - twoTilesAbove);
        if (player.SpawnX == -1 && player.SpawnY == -1)
        {
            playerSpawnPos = new(Main.spawnTileX * 16, Main.spawnTileY * 16 - 48);
        }

        player.Teleport(playerSpawnPos);
    }

    private void OpenFullscreenMap()
    {
        // Do nothing if config value is false
        var config = ModContent.GetInstance<AdventureClientConfig>();
        if (!config.OpenMapAfterRecall)
            return;

        // First we must close inventory, otherwise map is not allowed to open
        Main.playerInventory = false;

        // Open fullscreen map
        Main.mapFullscreen = true;

        // Center the map
        float worldCenterX = Main.maxTilesX / 2f;
        float worldCenterY = Main.maxTilesY / 2f;
        Main.mapFullscreenPos.X = worldCenterX;
        Main.mapFullscreenPos.Y = worldCenterY;

        // Zoom out a bit to see the whole map
        Main.mapFullscreenScale = 0.01f; 
    }

    public override void UseAnimation(Player player)
    {
        base.UseAnimation(player);
    }

    public override bool? UseItem(Player player)
    {
        return true;
    }

    public override void UpdateInventory(Player player)
    {
        // Force favorite every tick
        Item.favorited = true;
    }
}
