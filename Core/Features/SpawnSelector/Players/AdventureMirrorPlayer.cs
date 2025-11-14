using Microsoft.Xna.Framework;
using PvPAdventure.Content.Items;
using PvPAdventure.Core.Features.SpawnSelector.Systems;
using PvPAdventure.System;
using PvPAdventure.System.Client;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.GameContent.Animations.IL_Actions.Sprites;

namespace PvPAdventure.Core.Features.SpawnSelector.Players;

public class AdventureMirrorPlayer : ModPlayer
{
    public int MirrorTimer; // frames left
    public bool MirrorActive;
    private int _mirrorDuration; // for countdown

    public void StartMirrorUse(int durationFrames)
    {
        if (MirrorActive)
            return;

        MirrorActive = true;
        MirrorTimer = durationFrames;
    }

    private void UpdateSpawnSelectorEnabledState()
    {
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
    }

    public override void PostUpdate()
    {
        UpdateSpawnSelectorEnabledState();

        if (!MirrorActive)
            return;

        if (Main.dedServ || Player.whoAmI != Main.myPlayer)
            return;

        

        // cancel if player moves
        if (Player.velocity.Length() > 0f)
        {
            MirrorActive = false;
            PopupText.NewText(new AdvancedPopupRequest
            {
                Color = Color.Crimson,
                Text = "Cancelled!",
                Velocity = new(0f, -4f),
                DurationInFrames = 60
            }, Player.Top);
            return;
        }

        // dust while channeling
        if (Main.rand.NextBool())
        {
            Dust.NewDust(
                Player.position,
                Player.width,
                Player.height,
                DustID.MagicMirror,
                Player.velocity.X * 0.5f,
                Player.velocity.Y * 0.5f,
                150,
                default,
                1.5f
            );
        }

        // countdown tick
        MirrorTimer--;

        // Show countdown every full second
        if (MirrorTimer > 0 && MirrorTimer % 60 == 0)
        {
            int secondsLeft = MirrorTimer / 60;
            PopupText.NewText(new AdvancedPopupRequest
            {
                Color = Color.Crimson,
                Text = secondsLeft.ToString(),
                Velocity = new(0f, -4f),
                DurationInFrames = 60
            }, Player.Top);
        }

        // End of channel, teleport once
        if (MirrorTimer <= 0)
        {
            Main.playerInventory = false;

            if (!Main.mapFullscreen)
            {
                SoundEngine.PlaySound(SoundID.MenuOpen);
                Main.mapFullscreen = true;

                float worldCenterX = Main.maxTilesX / 2f;
                float worldCenterY = Main.maxTilesY / 2f;
                Main.mapFullscreenPos.X = worldCenterX;
                Main.mapFullscreenPos.Y = worldCenterY;

                Main.mapFullscreenScale = 0.21f;

                Vector2 spawnPos = new(Main.spawnTileX * 16, Main.spawnTileY * 16 - 100);
                Main.LocalPlayer.Teleport(spawnPos);

                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    // TODO: sync needed?
                }
            }

            SpawnSelectorSystem.SetEnabled(true);
            Player.RemoveAllGrapplingHooks();

            MirrorActive = false;
        }
    }

    public override void UpdateDead()
    {
        // debug
        //Main.NewText(Player.respawnTimer);

        // Keeps the player from respawning until they have selected a spawn.
        if (Player.respawnTimer <= 10)
        {
            //Player.respawnTimer = 10;
            if (!Main.mapFullscreen)
            {
                Main.mapFullscreen = true;
                SpawnSelectorSystem.SetEnabled(true);
            }
        }
    }

    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        var keybinds = ModContent.GetInstance<Keybinds>();

        if (keybinds.AdventureMirrorKeybind.JustPressed)
        {
            TryUseAdventureMirror();
        }
    }

    private void TryUseAdventureMirror()
    {
        // Find a mirror in the player's inventory
        int mirrorIndex = -1;

        for (int i = 0; i < Player.inventory.Length; i++)
        {
            if (Player.inventory[i].type == ModContent.ItemType<AdventureMirror>())
            {
                mirrorIndex = i;
                break;
            }
        }

        if (mirrorIndex == -1)
        {
            Main.NewText("You don't have an Adventure Mirror!", Color.Red);
            return;
        }

        // Cancel if the player is moving
        if (Player.velocity.Length() > 0f)
        {
            PopupText.NewText(new AdvancedPopupRequest
            {
                Color = Color.Crimson,
                Text = "Cannot use while moving!",
                Velocity = new(0, -4),
                DurationInFrames = 60
            }, Player.Top);
            return;
        }

        // Force the player to use the item
        Player.selectedItem = mirrorIndex;
        Player.controlUseItem = true;
        Player.ItemCheck();
    }
}
