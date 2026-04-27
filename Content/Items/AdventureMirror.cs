using Microsoft.Xna.Framework;
using PvPAdventure.Common.GameTimer;
using PvPAdventure.Common.SpawnSelector;
using PvPAdventure.Core.Config;
using PvPAdventure.Core.Input;
using PvPAdventure.Core.Net;
using System;
using System.Collections.Generic;
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

    private static int GetRecallFrames() =>
        ModContent.GetInstance<ServerConfig>().AdventureMirrorRecallSeconds * 60;

    public override void SetDefaults()
    {
        //Item.CloneDefaults(ItemID.MagicMirror);

        int recallFrames = GetRecallFrames();

        Item.useTime = recallFrames + 3; // + a few frames to ensure countdown shows
        Item.useAnimation = recallFrames + 3; // + a few frames to ensure countdown 
        Item.useStyle = ItemUseStyleID.HoldUp;
        Item.UseSound = SoundID.Item6;
        Item.rare = ItemRarityID.Blue;
        Item.value = Item.buyPrice(gold: 0);
        Item.noUseGraphic = false;
    }
    public override bool ConsumeItem(Player player) => false;
    public override bool? UseItem(Player player) => true;

    public override void UseItemFrame(Player player)
    {
        if (player.itemAnimation == Item.useAnimation)
        {
            StartUseCountdown(player);
        }
    }

    private static void StartUseCountdown(Player player)
    {
        int recallFrames = GetRecallFrames();

        player.itemTime = recallFrames + 1;
        player.itemAnimation = recallFrames + 1;
        player.itemTimeMax = player.itemTime;
        player.itemAnimationMax = player.itemAnimation;
        player.reuseDelay = 0;

        ResetUseFlags(player);
    }

    private static void ResetUseFlags(Player player)
    {
        SpawnPlayer sp = player.GetModPlayer<SpawnPlayer>();
        sp.SpawnedPortalThisUse = false;
        sp.AdventureMirrorHadCountdownThisUse = false;
    }

    internal static void ResetUseState(Player player)
    {
        player.controlUseItem = false;
        player.releaseUseItem = false;
        player.channel = false;
        player.itemAnimation = 0;
        player.itemAnimationMax = 0;
        player.itemTime = 0;
        player.itemTimeMax = 0;
        player.reuseDelay = 0;

        ResetUseFlags(player);
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
                mirror.TryStartUse(player, i, sendNet: true);
                return;
            }
        }
    }

    private void TryStartUse(Player player, int index, bool sendNet)
    {
        SpawnPlayer sp = player.GetModPlayer<SpawnPlayer>();
        Log.Debug($"[Mirror] try {player.name} slot={index} anim={player.itemAnimation} spawned={sp.SpawnedPortalThisUse}");

        if (sp.SelectedType == SpawnType.MyPortal)
            sp.ClearSelection();

        if (!CanUseItem(player))
        {
            Log.Debug($"[Mirror] blocked CanUse {player.name}");
            return;
        }

        if (player.CCed || player.itemAnimation > 0 || player.reuseDelay > 0)
        {
            Log.Debug($"[Mirror] blocked state {player.name} anim={player.itemAnimation} reuse={player.reuseDelay}");
            return;
        }

        if (!BeginMirrorUse(player, index))
            return;

        if (sendNet && Main.netMode == NetmodeID.MultiplayerClient)
        {
            ModPacket p = Mod.GetPacket();
            p.Write((byte)AdventurePacketIdentifier.AdventureMirrorRightClickUse);
            p.Write((byte)player.whoAmI);
            p.Write((byte)index);
            p.Send();
        }
    }

    private bool BeginMirrorUse(Player player, int index)
    {
        ResetUseFlags(player);

        player.selectedItem = index;
        player.controlUseItem = true;
        player.releaseUseItem = true;
        player.ItemCheck();

        player.controlUseItem = false;
        player.releaseUseItem = false;
        player.channel = false;

        if (player.HeldItem?.ModItem is not AdventureMirror || player.itemAnimation <= 0)
            return false;

        StartUseCountdown(player);

        Log.Debug($"[Mirror] begin {player.name} slot={index} ticks={player.itemTime}");
        return true;
    }

    #endregion

    public override bool CanUseItem(Player player)
    {
        if (player.ghost)
            return false;

        if (ModContent.GetInstance<GameManager>().CurrentPhase != GameManager.Phase.Playing)
        {
            Warning(player, "Mods.PvPAdventure.AdventureMirror.GameNotStarted");
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
        ResetUseState(player);
    }

    public override void UseStyle(Player player, Rectangle heldItemFrame)
    {
        base.UseStyle(player, heldItemFrame);

        player.controlUseItem = false;
        player.channel = false;

        int framesLeft = player.itemTime - 2;
        if (framesLeft < 0) framesLeft = 0;

        bool finishedUse = framesLeft == 0;
        SpawnPlayer sp = player.GetModPlayer<SpawnPlayer>();

        // Initialize on the first frame of the animation
        if (player.itemAnimation == player.itemAnimationMax - 1)
        {
            sp.SpawnedPortalThisUse = false;
            sp.AdventureMirrorHadCountdownThisUse = false;
        }

        // If this player moves, cancel their use and reset
        if (!finishedUse && player.velocity.LengthSquared() > 0)
        {
            Warning(player, "Mods.PvPAdventure.AdventureMirror.Cancelled");
            CancelItemUse(player);
            return;
        }

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

        bool hadSelectionAtFinish = finishedUse && sp.AdventureMirrorHadCountdownThisUse && sp.SelectedType != SpawnType.None;

        if (hadSelectionAtFinish)
        {
            sp.RequestExecute();

            if (SpawnSystem.TryExecuteSelection(player, sp))
                return;
        }

        bool shouldCreatePortal = finishedUse && sp.AdventureMirrorHadCountdownThisUse && !sp.SpawnedPortalThisUse && !hadSelectionAtFinish;

        // Create portal

        if (framesLeft > 0)
            sp.AdventureMirrorHadCountdownThisUse = true;

        if (finishedUse && !shouldCreatePortal)
            Log.Debug($"[Mirror] no portal {player.name} spawned={sp.SpawnedPortalThisUse} countdown={sp.AdventureMirrorHadCountdownThisUse}");

        if (shouldCreatePortal)
        {
            Log.Debug($"[Mirror] create {player.name} pos={player.Bottom}");
            sp.SpawnedPortalThisUse = true;
            PortalSystem.CreatePortalAtPosition(player, player.Bottom);
            ResetUseState(player);
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

        if (color == default)
            color = PortalDrawer.GetPortalColor(player);

        ShowPopup(player, Language.GetTextValue(localizationKey), color);
    }

    private static void ShowPopup(Player player, string text, Color color)
    {
        PopupText.NewText(new AdvancedPopupRequest
        {
            Color = color,
            Text = text,
            Velocity = new Vector2(0f, -4f),
            DurationInFrames = 120
        }, player.Top + new Vector2(0, -4));
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        string controlsText = Keybinds.UseAdventureMirrorLabel == "assign a keybind in Controls"
            ? "Right click to use, or assign a keybind in Controls"
            : $"Right click or press {Keybinds.UseAdventureMirrorLabel} to use";

        int controlsIndex = tooltips.FindIndex(static line =>
            line.Mod == "Terraria" &&
            line.Text.Contains("Right click or press", StringComparison.OrdinalIgnoreCase));

        if (controlsIndex >= 0)
        {
            tooltips[controlsIndex].Text = controlsText;
            return;
        }

        int insertIndex = tooltips.FindLastIndex(static line =>
            line.Mod == "Terraria" &&
            line.Name.StartsWith("Tooltip", StringComparison.Ordinal));

        tooltips.Insert(insertIndex + 1, new TooltipLine(Mod, "AdventureMirrorControls", controlsText));
    }
}
