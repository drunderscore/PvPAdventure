using Microsoft.Xna.Framework;
using PvPAdventure.Common.GameTimer;
using PvPAdventure.Common.SpawnSelector;
using PvPAdventure.Core.Config;
using PvPAdventure.Core.Input;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PvPAdventure.Content.Items;

/// <summary>
/// An item that spawns a portal at the end of the use time.
/// Starts spawning the portal instantly, fully formed after the end of use time.
/// </summary>
internal class AdventureMirror : ModItem
{
    public override string Texture => $"PvPAdventure/Assets/Items/AdventureMirror";

    internal static int GetRecallFrames() =>
        Math.Max(0, ModContent.GetInstance<ServerConfig>().AdventureMirrorRecallSeconds * 60);

    internal static void PrepareUseTimings(Item item)
    {
        int recallFrames = GetRecallFrames();
        int animationFrames = Math.Max(1, recallFrames + 3);

        item.useTime = animationFrames;
        item.useAnimation = animationFrames;
    }

    public override void SetDefaults()
    {
        //Item.CloneDefaults(ItemID.MagicMirror);

        PrepareUseTimings(Item);
        Item.useStyle = ItemUseStyleID.HoldUp;
        Item.UseSound = SoundID.Item6;
        Item.rare = ItemRarityID.Blue;
        Item.value = Item.buyPrice(gold: 0);
        Item.noUseGraphic = false;
    }
    public override bool ConsumeItem(Player player) => false;
    public override bool? UseItem(Player player)
    {
        SpawnPlayer sp = player.GetModPlayer<SpawnPlayer>();
        if (!sp.AdventureMirrorCountdownStartedThisUse)
            sp.StartAdventureMirrorUse();

        return true;
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

        player.GetModPlayer<SpawnPlayer>().ResetAdventureMirrorUse();
    }

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
                mirror.TryStartUse(player, i);
                return;
            }
        }
    }

    private void TryStartUse(Player player, int index)
    {
        SpawnPlayer sp = player.GetModPlayer<SpawnPlayer>();
        Log.Debug($"[Mirror] try start use anim={player.itemAnimation} spawned={sp.SpawnedPortalThisUse}");

        PrepareUseTimings(player.inventory[index]);

        if (sp.SelectedType == SpawnType.MyPortal)
            sp.ClearSelection();

        if (!CanUseItem(player))
        {
            Log.Debug($"[Mirror] blocked");
            return;
        }

        if (player.CCed || player.itemAnimation > 0 || player.reuseDelay > 0)
        {
            Log.Debug($"[Mirror] blocked state anim={player.itemAnimation} reuse={player.reuseDelay}");
            return;
        }

        BeginMirrorUse(player, index);
    }

    private bool BeginMirrorUse(Player player, int index)
    {
        player.GetModPlayer<SpawnPlayer>().ResetAdventureMirrorUse();

        player.selectedItem = index;
        player.controlUseItem = true;
        player.releaseUseItem = true;

        if (Main.netMode == NetmodeID.MultiplayerClient)
            NetMessage.SendData(MessageID.PlayerControls, -1, -1, null, player.whoAmI);

        player.ItemCheck();

        player.controlUseItem = false;
        player.releaseUseItem = false;
        player.channel = false;

        if (player.HeldItem?.ModItem is not AdventureMirror || player.itemAnimation <= 0)
            return false;

        SpawnPlayer sp = player.GetModPlayer<SpawnPlayer>();
        if (!sp.AdventureMirrorCountdownStartedThisUse)
            sp.StartAdventureMirrorUse();

        Log.Debug($"[Mirror] begin {player.name} slot={index} ticks={player.itemTime}");
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

        SpawnPlayer sp = player.GetModPlayer<SpawnPlayer>();
        if (!sp.AdventureMirrorCountdownStartedThisUse)
            return;

        DrawMirrorUseVisuals(player, sp.AdventureMirrorTicksLeft);
    }

    #region Misc hooks that dont affect the item use state
    // Visual popup text and dust effects for the countdown
    private static void DrawMirrorUseVisuals(Player player, int framesLeft)
    {
        if (framesLeft <= 0)
            return;

        if (Main.rand.NextBool())
            Dust.NewDust(player.position, player.width, player.height, DustID.MagicMirror, player.velocity.X * 0.5f, player.velocity.Y * 0.5f, 150, default, 1.5f);

        if (player.itemTime % 60 != 0)
            return;

        int secondsLeft = (framesLeft + 59) / 60;

        if (secondsLeft <= 0)
            return;

        PopupText.NewText(new AdvancedPopupRequest
        {
            Color = Main.teamColor[(int)player.team],
            Text = secondsLeft.ToString(),
            Velocity = new Vector2(0f, -4f),
            DurationInFrames = 120
        }, player.Top + new Vector2(0f, -4f));
    }

    public override bool CanUseItem(Player player)
    {
        PrepareUseTimings(Item);

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

    public override void UpdateInventory(Player player)
    {
        // Force favorite every tick
        // A little redundant now that we disallowed unfavorite in ItemSlotHooks left click hook
        Item.favorited = true;
    }

    // Helper warning popup text
    internal static void Warning(Player player, string localizationKey, Color color = default)
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
    #endregion
}
