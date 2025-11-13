using Microsoft.Xna.Framework;
using PvPAdventure.Content.Items;
using PvPAdventure.Core.Features.SpawnSelector.Systems;
using PvPAdventure.System;
using Terraria;
using Terraria.ModLoader;
using static Terraria.ModLoader.BackupIO;

namespace PvPAdventure.Core.Features.SpawnSelector.Players;

public class SpawnSelectorPlayer : ModPlayer
{
    public override void PostUpdate()
    {
        if (Main.dedServ || Player.whoAmI != Main.myPlayer)
        {
            return;
        }

        // Force SSS to be enabled when player is within the spawn region.
        var regionManager = ModContent.GetInstance<RegionManager>();
        Point tilePos = Player.Center.ToTileCoordinates();
        var region = regionManager.GetRegionContaining(tilePos);
        bool inRegion = region != null;
        if (inRegion)
            SpawnSelectorSystem.SetEnabled(true);

        // Unsure if this causes any bugs.
        // It may be too forcing to always set it to false whenever map is closed.
        if (!Main.mapFullscreen)
            SpawnSelectorSystem.SetEnabled(false);

        // Check if player is using AdventureMirror
        if (Player.HeldItem != null &&
            Player.HeldItem.type == ModContent.ItemType<AdventureMirror>() &&
            Player.itemAnimation > 0 && 
            Player.itemAnimation % 60 == 0)
        {
            int itemUseTimeLeft = Main.LocalPlayer.itemAnimation / 60;
            string timeLeft = itemUseTimeLeft.ToString();

            // Display text above the player every second
            PopupText.NewText(new AdvancedPopupRequest
            {
                Color = Color.Crimson,
                Text = timeLeft,
                Velocity = new(0.0f, -4.0f),
                DurationInFrames = 60 * 1
            }, Player.Top);
        }
    }

    public bool hasSelectedSpawnPoint = false;

    public override void UpdateDead()
    {
        //Main.NewText(Player.respawnTimer);

        // Keeps the player from respawning until they have selected a spawn.
        if (Player.respawnTimer <= 10 && !hasSelectedSpawnPoint)
        {
            //Player.respawnTimer = 10;
            if (!Main.mapFullscreen)
            {
                Main.mapFullscreen = true;
                SpawnSelectorSystem.SetEnabled(true);
            }
        }
    }
}