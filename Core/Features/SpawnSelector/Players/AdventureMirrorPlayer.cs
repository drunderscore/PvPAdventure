using Microsoft.Xna.Framework;
using PvPAdventure.Content.Items;
using PvPAdventure.Core.Features.SpawnSelector.Systems;
using PvPAdventure.Core.Features.SpawnSelector.UI;
using PvPAdventure.System;
using PvPAdventure.System.Client;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.ModLoader.BackupIO;

namespace PvPAdventure.Core.Features.SpawnSelector.Players;

public class AdventureMirrorPlayer : ModPlayer
{
    public int MirrorTimer; // frames left
    public bool MirrorActive; // currently channeling
    public int MirrorCountdownLastSecond = -1;
    private bool didRebuildOnDeath;
    public bool TryStartMirrorChannel()
    {
        if (ModContent.GetInstance<GameManager>().CurrentPhase != GameManager.Phase.Playing)
        {
            if (Player.whoAmI == Main.myPlayer)
                PopupTextHelper.NewText("Cannot use before game has started!");
            return false;
        }

        if (IsPlayerInSpawnRegion())
        {
            if (Player.whoAmI == Main.myPlayer)
                PopupTextHelper.NewText("Cannot use in spawn region!");
            return false;
        }

        if (MirrorActive)
            return false;

        if (Player.whoAmI == Main.myPlayer && IsMoving())
        {
            CancelMirrorUse();
            PopupTextHelper.NewText("Cannot use while moving!");
            return false;
        }

        SoundEngine.PlaySound(SoundID.Item6);
        var config = ModContent.GetInstance<AdventureConfig>();
        int recallFrames = config.AdventureMirrorRecallFrames + 1;

        MirrorActive = true;
        MirrorTimer = recallFrames;
        return true;
    }

    public bool IsPlayerInSpawnRegion()
    {
        // Force SSS to be enabled when player is within the spawn region.
        var regionManager = ModContent.GetInstance<RegionManager>();
        Point tilePos = Player.Center.ToTileCoordinates();
        var region = regionManager.GetRegionContaining(tilePos);
        if (region != null)
        {
            return true;
        }
        return false;
    }

    private bool IsMoving() => Player.velocity.LengthSquared() > 0f;

    private void CancelMirrorUse()
    {
        MirrorActive = false;
        MirrorTimer = 0;
        MirrorCountdownLastSecond = -1;

        if (Player.itemAnimation > 0 &&
            Player.HeldItem.type == ModContent.ItemType<AdventureMirror>())
        {
            Player.controlUseItem = false;
            Player.channel = false;
            Player.itemAnimation = 0;
            Player.itemTime = 0;
            Player.reuseDelay = 0;
        }
    }

    public bool CancelIfPlayerMoves()
    {
        if (Player.whoAmI != Main.myPlayer)
            return false;

        if (!IsMoving())
            return false;

        CancelMirrorUse();
        PopupTextHelper.NewText("Cannot use while moving!");
        return true;
    }

    public override void PostUpdate()
    {
        if (!Player.dead)
        {
            didRebuildOnDeath = false;
        }

        if (Main.dedServ || Player.whoAmI != Main.myPlayer)
            return;


        if (ModContent.GetInstance<GameManager>().CurrentPhase == GameManager.Phase.Playing && Main.mapFullscreen && IsPlayerInSpawnRegion())
        {
            SpawnSelectorSystem.SetEnabled(true);
        }
        else
        {
            SpawnSelectorSystem.SetEnabled(false);
        }

        if (!MirrorActive)
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
            PopupTextHelper.NewText(secondsLeft.ToString(), Color.LightGreen, showInMultiplayer: true);
        }

        // Ready to teleport to spawn
        if (MirrorTimer <= 0)
        {
            var config = ModContent.GetInstance<AdventureClientConfig>();
            if (config.OpenMapAfterRecall)
            {
                // Close player inventory
                Main.playerInventory = false;

                // Open fullscreen map
                Main.mapFullscreen = true;

                // center the map
                float worldCenterX = Main.maxTilesX / 2f;
                float worldCenterY = Main.maxTilesY / 2f;
                Main.mapFullscreenPos.X = worldCenterX;
                Main.mapFullscreenPos.Y = worldCenterY;
                Main.mapFullscreenScale = 0.21f; // arbitrary value to zoom out that fits the entire map
            }

            // Teleport to spawn
            Vector2 spawnPos = new(Main.spawnTileX * 16, Main.spawnTileY * 16 - 48);
            //Main.LocalPlayer.Teleport(spawnPos, 0);
            CustomTeleportWithoutSound(spawnPos);

            SpawnSelectorSystem.SetEnabled(true); // This is redundant because spawn region already sets it to true
            ModContent.GetInstance<SpawnSelectorSystem>().state.spawnSelectorPanel.Rebuild();
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

    #region Keybind Handling
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
        int mirrorIndex = EnsureMirrorInInventory();

        if (mirrorIndex == -1)
        {
            PopupTextHelper.NewText("No Adventure Mirror found in inventory or banks!");
            return;
        }

        if (CancelIfPlayerMoves())
            return;

        Player.selectedItem = mirrorIndex;
        Player.controlUseItem = true;
        Player.ItemCheck();
    }

    private int EnsureMirrorInInventory()
    {
        int mirrorType = ModContent.ItemType<AdventureMirror>();

        // 1. Look in main inventory first
        for (int i = 0; i < Player.inventory.Length; i++)
        {
            Item item = Player.inventory[i];
            if (!item.IsAir && item.type == mirrorType)
                return i;
        }

        // 2. Try to pull from personal banks
        if (TryPullMirrorFromBank(Player.bank, mirrorType, out int invIndex))
            return invIndex;

        if (TryPullMirrorFromBank(Player.bank2, mirrorType, out invIndex))
            return invIndex;

        if (TryPullMirrorFromBank(Player.bank3, mirrorType, out invIndex))
            return invIndex;

        if (TryPullMirrorFromBank(Player.bank4, mirrorType, out invIndex)) // void vault
            return invIndex;

        return -1;
    }

    private bool TryPullMirrorFromBank(Chest bank, int mirrorType, out int inventoryIndex)
    {
        inventoryIndex = -1;

        if (bank == null)
            return false;

        // Find the mirror inside this bank
        int bankSlot = -1;
        for (int i = 0; i < bank.item.Length; i++)
        {
            Item item = bank.item[i];
            if (!item.IsAir && item.type == mirrorType)
            {
                bankSlot = i;
                break;
            }
        }

        if (bankSlot == -1)
            return false;

        // Find a free inventory slot
        int freeInvSlot = -1;
        for (int i = 0; i < Player.inventory.Length; i++)
        {
            if (Player.inventory[i].IsAir)
            {
                freeInvSlot = i;
                break;
            }
        }

        if (freeInvSlot == -1)
        {
            if (Player.whoAmI == Main.myPlayer)
                Main.NewText("No free inventory slot for Adventure Mirror!", Color.Red);
            return false;
        }

        // Move the item from bank → inventory
        Player.inventory[freeInvSlot] = bank.item[bankSlot].Clone();
        bank.item[bankSlot].TurnToAir();

        inventoryIndex = freeInvSlot;
        return true;
    }
    #endregion
}
