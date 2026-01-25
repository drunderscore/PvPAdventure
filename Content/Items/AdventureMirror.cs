using Microsoft.Xna.Framework;
using PvPAdventure.Common.GameTimer;
using PvPAdventure.Common.SpawnSelector;
using PvPAdventure.Core.Config;
using PvPAdventure.Core.Net;
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
    public override string Texture => $"PvPAdventure/Assets/Items/AdventureMirror";
    public override void SetDefaults()
    {
        //Item.CloneDefaults(ItemID.MagicMirror);

        var config = ModContent.GetInstance<ServerConfig>();
        int recallFrames = config.AdventureMirrorRecallSeconds * 60; // 5 seconds = 60 * 5

        Item.useTime = recallFrames + 3; // + a few frames to ensure countdown shows
        Item.useAnimation = recallFrames + 3; // + a few frames to ensure countdown 
        Item.useStyle = ItemUseStyleID.HoldUp;
        Item.UseSound = SoundID.Item6;
        Item.rare = ItemRarityID.Blue;
        Item.value = Item.buyPrice(gold: 0);
        Item.noUseGraphic = false;
    }
    public override bool ConsumeItem(Player player) => false;
    public override bool? UseItem(Player player)
    {
        //if (Main.netMode == NetmodeID.MultiplayerClient)
        //{
        //    ModPacket p = Mod.GetPacket();
        //    p.Write((byte)AdventurePacketIdentifier.AdventureMirrorRightClickUse);
        //    p.Write((byte)player.whoAmI);
        //    p.Write((byte)player.selectedItem);
        //    p.Send();
        //}

        return true;
    }
    public override void UseItemFrame(Player player)
    {
        if (player.itemAnimation == Item.useAnimation)
        {
            ResetUseTimer(player);
        }
    }

    private void ResetUseTimer(Player player)
    {
        int recallFrames = ModContent.GetInstance<ServerConfig>().AdventureMirrorRecallSeconds * 60;

        player.itemTime = recallFrames + 1;
        player.itemAnimation = recallFrames + 1;
        player.reuseDelay = 0;
    }

    #region Right click use
    public override bool CanRightClick() => true;
    public override bool AltFunctionUse(Player player) => false;

    public override void RightClick(Player player)
    {
        TryUse(player);
    }

    public static void TryUse(Player player)
    {
        if (player == null || player.whoAmI != Main.myPlayer)
            return;

        for (int i = 0; i < player.inventory.Length; i++)
        {
            if (player.inventory[i].ModItem is AdventureMirror mirror)
            {
                mirror.TryUseInternal(player, i);
                return;
            }
        }
    }

    private void TryUseInternal(Player player, int index)
    {
        if (!CanUseItem(player))
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

        if (player.GetModPlayer<SpawnPlayer>().IsPlayerInSpawnRegion())
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

        player.controlUseItem = false;
        player.channel = false;

        // If this player moves, cancel their use and reset
        if (player.velocity.LengthSquared() > 0)
        {
            Warning(player, "Mods.PvPAdventure.AdventureMirror.Cancelled");
            CancelItemUse(player);
            return;
        }

        int framesLeft = player.itemTime - 2;
        if (framesLeft < 0)
            framesLeft = 0;

        int secondsLeft = (framesLeft + 59) / 60;

        // Stop dust once we are at 0
        if (framesLeft > 0)
        {
            if (Main.rand.NextBool())
                Dust.NewDust(player.position, player.width, player.height, DustID.MagicMirror,
                player.velocity.X * 0.5f, player.velocity.Y * 0.5f, 150, default, 1.5f);
        }

        // Countdown popup with team color
        Color teamColor = Main.teamColor[(int)player.team];

        if (framesLeft == 2 && player.reuseDelay == 0)
        {
            PopupText.NewText(new AdvancedPopupRequest
            {
                Color = teamColor,
                Text = "0",
                Velocity = new(0f, -4),
                DurationInFrames = 120
            }, player.Top + new Vector2(0, -4));
        }
        else if (framesLeft >= 2 && player.itemTime % 60 == 0 && secondsLeft > 0)
        {
            PopupText.NewText(new AdvancedPopupRequest
            {
                Color = teamColor,
                Text = secondsLeft.ToString(),
                Velocity = new(0f, -4),
                DurationInFrames = 120
            }, player.Top + new Vector2(0, -4));
            return;
        }

        // Make held item hold on indefinitely.
        if (player.itemTime <= 2)
        {
            player.itemTime = 2;

            if (player.itemAnimation < 2)
                player.itemAnimation = 2;

            if (player.reuseDelay < 2)
                player.reuseDelay = 2;
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
        if (player.whoAmI != Main.myPlayer)
            return;

        //if (!ModContent.GetInstance<AdventureClientConfig>().ShowPopupText)
        //return;

        if (color == default)
            color = Color.Crimson;

        Color teamColor = Main.teamColor[(int)player.team];

        PopupText.NewText(new AdvancedPopupRequest
        {
            Color = teamColor,
            Text = Language.GetTextValue(localizationKey),
            Velocity = new Vector2(0f, -4f),
            DurationInFrames = 120
        }, player.Top + new Vector2(0, -4));
    }

}