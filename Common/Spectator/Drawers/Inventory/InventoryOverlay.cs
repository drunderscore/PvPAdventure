using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Spectator.SpectatorMode;
using ReLogic.Graphics;
using System;
using Terraria;
using Terraria.GameContent;

namespace PvPAdventure.Common.Spectator.Drawers.Inventory;

internal static class InventoryOverlay
{
    private static int playerIndex = -1;
    private static bool releaseInventory = true;

    // Hotfix to prevent logging the inventory disabled text if we closed settings menu with escape.
    private static bool optionsWindowWasOpen;

    public static void Update()
    { 
        //bool optionsWindowIsOpen = Main.ingameOptionsWindow;
        //bool optionsWindowWasOpenLastFrame = optionsWindowWasOpen;
        //optionsWindowWasOpen = optionsWindowIsOpen;
        Player local = Main.LocalPlayer;

        if (local?.active != true || !SpectatorModeSystem.IsInSpectateMode(local))
        {
            Clear();
            releaseInventory = true;
            return;
        }

        if (!local.controlInv)
        {
            releaseInventory = true;
            return;
        }

        if (!releaseInventory)
            return;

        releaseInventory = false;

        Player target = SpectatorTargetSystem.GetPlayerTarget();

        if (target?.active != true)
        {
            Main.NewText("Inventory is disabled as a spectator unless you are spectating another player.", Color.Yellow);
            return;
        }

        Toggle(target);
    }

    public static bool IsOpen(Player player)
    {
        return player?.active == true && playerIndex == player.whoAmI;
    }

    public static bool IsOpen(int targetPlayerIndex)
    {
        return IsValidPlayerIndex(targetPlayerIndex) && playerIndex == targetPlayerIndex;
    }

    public static void Toggle(Player player)
    {
        if (player?.active != true)
        {
            Clear();
            return;
        }

        playerIndex = IsOpen(player) ? -1 : player.whoAmI;
    }

    public static void Toggle(int targetPlayerIndex)
    {
        if (!IsValidPlayerIndex(targetPlayerIndex))
        {
            Clear();
            return;
        }

        playerIndex = IsOpen(targetPlayerIndex) ? -1 : targetPlayerIndex;
    }

    public static void Clear()
    {
        playerIndex = -1;
    }

    public static void Draw(SpriteBatch sb)
    {
        Player player = GetDrawTarget();

        if (player?.active != true)
        {
            Clear();
            return;
        }

        Rectangle viewport = new(0, 0, Main.screenWidth, Main.screenHeight);

        if (IsOpen(player))
        {
            InventoryDrawer.DrawInventory(sb, new Vector2(20f, 20f), player, viewport);
            DrawResourceBars(sb, player);
            return;
        }

        DrawHotbar(sb, player);
        DrawResourceBars(sb, player);
    }

    private static Player GetDrawTarget()
    {
        if (IsValidPlayerIndex(playerIndex))
            return Main.player[playerIndex];

        return SpectatorTargetSystem.GetPlayerTarget();
    }

    private static void DrawHotbar(SpriteBatch sb, Player player)
    {
        string text = $"{player.name}'s Hotbar";
        //Vector2 position = new(4f, 2f);
        //Utils.DrawBorderString(sb, text, position, Color.White, 0.9f);

        sb.DrawString(FontAssets.MouseText.Value, text, new Vector2(4f, 0f), new Color(Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor), 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

        HotbarDrawer.DrawHotbar(player);
    }

    private static void DrawBuffs(SpriteBatch spriteBatch, Player player)
    {

    }

    private static void DrawResourceBars(SpriteBatch spriteBatch, Player player)
    {
        ResourceBarsDrawer.DrawResourceBarsLikeVanilla(player);
    }

    private static bool IsValidPlayerIndex(int targetPlayerIndex)
    {
        return targetPlayerIndex >= 0 && targetPlayerIndex < Main.maxPlayers && Main.player[targetPlayerIndex]?.active == true;
    }
}
