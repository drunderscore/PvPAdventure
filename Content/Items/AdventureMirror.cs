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
        if (player.whoAmI != Main.myPlayer || !CanUseItem(player))
            return;

        int index = -1;
        for (int i = 0; i < player.inventory.Length; i++)
        {
            if (player.inventory[i] == Item)
            {
                index = i;
                break;
            }
        }

        if (index == -1 || player.CCed || player.itemAnimation > 0 || player.reuseDelay > 0)
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
            p.Send();
        }
    }
    #endregion

    public override bool CanUseItem(Player player)
    {
        if (ModContent.GetInstance<GameManager>().CurrentPhase != GameManager.Phase.Playing)
        {
            Warning(player, "Mods.PvPAdventure.AdventureMirror.GameNotStarted");
            return false;
        }

        if (player.GetModPlayer<SpawnPointPlayer>().IsPlayerInSpawnRegion())
        {
            Warning(player, "Mods.PvPAdventure.AdventureMirror.CannotUseInSpawn");
            return false;
        }

        if (player.velocity.LengthSquared() > 0)
        {
            Warning(player, "Mods.PvPAdventure.AdventureMirror.CannotUseWhileMoving");
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

        // If this player moves, cancel their use
        if (player.velocity.LengthSquared() > 0)
        {
            Warning(player, "Mods.PvPAdventure.AdventureMirror.Cancelled");
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
            player.Spawn(PlayerSpawnContext.RecallFromItem);
        }
    }

    public override void UpdateInventory(Player player)
    {
        // Force favorite every tick
        // A little redundant now that we disallowed unfavorite in ItemSlotHooks left click hook
        Item.favorited = true;
    }

    // Helper warning popup text
    private static void Warning(Player player, string localizationKey, Color color = default)
    {
        if (player.whoAmI != Main.myPlayer || !ModContent.GetInstance<AdventureClientConfig>().ShowPopupText)
            return;

        if (color == default)
            color = Color.Crimson;

        PopupText.NewText(new AdvancedPopupRequest
        {
            Color = color,
            Text = Language.GetTextValue(localizationKey),
            Velocity = new Vector2(0f, -4f),
            DurationInFrames = 120
        }, player.Top + new Vector2(0, -4));
    }

}
