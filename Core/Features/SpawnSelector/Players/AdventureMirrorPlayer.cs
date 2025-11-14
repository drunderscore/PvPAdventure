using Microsoft.Xna.Framework;
using PvPAdventure.Content.Items;
using PvPAdventure.Core.Features.SpawnSelector.Systems;
using PvPAdventure.System;
using PvPAdventure.System.Client;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Features.SpawnSelector.Players;

public class AdventureMirrorPlayer : ModPlayer
{
    public int MirrorTimer; // frames left
    public bool MirrorActive;

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

        // Force closes SSS whenever map is closed.
        if (!Main.mapFullscreen)
            SpawnSelectorSystem.SetEnabled(false);
    }
    public bool CancelIfPlayerMoves()
    {
        if (Player.velocity.LengthSquared() <= 0f)
            return false;

        // stop the mirror channel
        if (MirrorActive)
        {
            MirrorActive = false;
            MirrorTimer = 0;
        }

        // stop the actual item use / animation too
        if (Player.itemAnimation > 0 && Player.HeldItem.type == ModContent.ItemType<AdventureMirror>())
        {
            Player.controlUseItem = false;
            Player.channel = false;
            Player.itemAnimation = 0;
            Player.itemTime = 0;
            Player.reuseDelay = 0;
        }

        PopupText.NewText(new AdvancedPopupRequest
        {
            Color = Color.Crimson,
            Text = "Cannot use while moving!",
            Velocity = new(0f, -4f),
            DurationInFrames = 60
        }, Player.Top);

        return true;
    }

    public override void PostUpdate()
    {
        UpdateSpawnSelectorEnabledState();

        if (!MirrorActive)
            return;

        if (Main.dedServ || Player.whoAmI != Main.myPlayer)
            return;

        if (CancelIfPlayerMoves())
            return;

        // spawn dust
        if (Main.rand.NextBool())
            Dust.NewDust(Player.position, Player.width, Player.height,DustID.MagicMirror, Player.velocity.X * 0.5f,Player.velocity.Y * 0.5f,150,default,1.5f);

        // countdown tick
        MirrorTimer--;
        //Main.NewText(MirrorTimer);

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
            // Close player inventory
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

                Vector2 spawnPos = new(Main.spawnTileX * 16, Main.spawnTileY * 16 - 48);
                //Main.LocalPlayer.Teleport(spawnPos, 0);
                CustomTeleportWithoutSound(spawnPos);
            }

            SpawnSelectorSystem.SetEnabled(true);
            Player.RemoveAllGrapplingHooks();

            MirrorActive = false;
        }
    }

    private void CustomTeleportWithoutSound(Vector2 newPos)
    {
        try
        {
            Player._funkytownAchievementCheckCooldown = 100;
            Player.environmentBuffImmunityTimer = 4;
            Player.RemoveAllGrapplingHooks();
            Player.StopVanityActions();
            if (Player.shimmering || Player.shimmerWet)
            {
                Player.shimmering = false;
                Player.shimmerWet = false;
                Player.wet = false;
                Player.ClearBuff(353);
            }
            int extraInfo2 = 0;

            float num = MathHelper.Clamp(1f - Player.teleportTime * 0.99f, 0.01f, 1f);
            Vector2 otherPosition = Player.position;
            //Main.TeleportEffect(Player.getRect(), 0, extraInfo2, num, TeleportationSide.Entry, newPos);
            float num2 = Vector2.Distance(Player.position, newPos);
            PressurePlateHelper.UpdatePlayerPosition(Player);
            Player.position = newPos;
            Player.fallStart = (int)(Player.position.Y / 16f);
            if (Player.whoAmI == Main.myPlayer)
            {
                bool flag = false;
                if (num2 < new Vector2(Main.screenWidth, Main.screenHeight).Length() / 2f + 100f)
                {
                    int time = 0;
                    Main.SetCameraLerp(0.1f, time);
                    flag = true;
                }
                else
                {
                    NPC.ResetNetOffsets();
                    Main.BlackFadeIn = 255;
                    Lighting.Clear();
                    Main.screenLastPosition = Main.screenPosition;
                    Main.screenPosition.X = Player.position.X + (float)(Player.width / 2) - (float)(Main.screenWidth / 2);
                    Main.screenPosition.Y = Player.position.Y + (float)(Player.height / 2) - (float)(Main.screenHeight / 2);
                    Main.instantBGTransitionCounter = 10;
                    Player.ForceUpdateBiomes();
                }
                //if (num > 0.1f || !flag || Style != 0)
                //{
                //    if (Main.mapTime < 5)
                //    {
                //        Main.mapTime = 5;
                //    }
                //    Main.maxQ = true;
                //    Main.renderNow = true;
                //}
            }

            PressurePlateHelper.UpdatePlayerPosition(Player);
            Player.ResetAdvancedShadows();
            for (int i = 0; i < 3; i++)
            {
                Player.UpdateSocialShadow();
            }
            Player.oldPosition = Player.position + Player.BlehOldPositionFixer;
            //Main.TeleportEffect(Player.getRect(), 0, extraInfo2, num, TeleportationSide.Exit, otherPosition);
            Player.teleportTime = 1f;
            Player.teleportStyle = 0;
        }
        catch
        {
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

    public void TryUseAdventureMirror()
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

        CancelIfPlayerMoves();

        // Force the player to use the item
        Player.selectedItem = mirrorIndex;
        Player.controlUseItem = true;
        Player.ItemCheck();
    }
}
