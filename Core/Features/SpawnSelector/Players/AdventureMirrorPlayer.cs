using Microsoft.Xna.Framework;
using PvPAdventure.Content.Items;
using PvPAdventure.Core.Features.SpawnSelector.Systems;
using PvPAdventure.Core.Features.SpawnSelector.UI;
using PvPAdventure.System;
using PvPAdventure.System.Client;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Features.SpawnSelector.Players;

public class AdventureMirrorPlayer : ModPlayer
{
    public override void OnHurt(Player.HurtInfo info)
    {
        base.OnHurt(info);
        // TODO Stop the channel + reset animation
        //PopupTextHelper.NewText("Mirror interrupted!", Color.Crimson);
    }

    public bool IsPlayerInSpawnRegion()
    {
        // Is player in spawn region?
        var regionManager = ModContent.GetInstance<RegionManager>();
        Point tilePos = Player.Center.ToTileCoordinates();
        var region = regionManager.GetRegionContaining(tilePos);
        if (region != null)
        {
            return true;
        }

        // Is player in bed spawn region?
        Vector2 bedSpawnPoint = new(Player.SpawnX, Player.SpawnY);
        float distanceToBedSpawn = Vector2.Distance(bedSpawnPoint * 16f, Player.Center);
        if (distanceToBedSpawn <= 25 * 16f)
        {
            return true;
        }

        return false;
    }

    public override void PostUpdate()
    {
        // Only run on clients
        if (Main.dedServ)
            return;

        // Only run on this client
        if (Player.whoAmI != Main.myPlayer)
            return;

        // Update spawn selector UI state every frame
        if (ModContent.GetInstance<GameManager>().CurrentPhase == GameManager.Phase.Playing && Main.mapFullscreen && IsPlayerInSpawnRegion())
        {
            SpawnSelectorSystem.SetEnabled(true);
        }
        else
        {
            SpawnSelectorSystem.SetEnabled(false);
        }
    }

    public void CustomTeleportWithoutSound(Vector2 newPos)
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
            TryUseAdventureMirrorKeybind();
        }
    }

    public void TryUseAdventureMirrorKeybind()
    {
        int mirrorIndex = EnsureMirrorInInventory();

        if (mirrorIndex == -1)
        {
            PopupTextHelper.NewText("No Adventure Mirror found in inventory or banks!", Player);
            return;
        }

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
