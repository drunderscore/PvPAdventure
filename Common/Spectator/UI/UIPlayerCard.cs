using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Misc.DeadSystems;
using PvPAdventure.Common.Spectator.Drawers;
using PvPAdventure.Common.Spectator.Drawers.Inventory;
using PvPAdventure.Common.Travel.UI;
using PvPAdventure.Core.Utilities;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace PvPAdventure.Common.Spectator.UI;

internal sealed class UIPlayerCard : UIPanel
{
    internal static int CardWidth => 115*2; // biome BG is 115 width
    internal static int CardHeight => 65*2; // biome BG is 65 height

    public int PlayerIndex { get; }
    public int ListIndex { get; }

    private readonly SpectatorControlsPanel owner;
    private readonly float scale;

    public UIPlayerCard(int playerIndex, int listIndex, SpectatorControlsPanel owner, float scale = 1f)
    {
        PlayerIndex = playerIndex;
        ListIndex = listIndex;
        this.owner = owner;
        this.scale = scale;

        SetPadding(0f);

        float buttonSize = 32f * scale;
        float buttonGap = 2f * scale;
        float buttonTop = CardHeight * scale - 5f * scale - buttonSize;
        float buttonLeft = 5f * scale;

        AddActionButtons(GetPlayerCardActions());
    }

    protected override void DrawSelf(SpriteBatch sb)
    {
        // Update border and background color if this player card is selected
        bool isSelected = PlayerIndex >= 0 &&
            PlayerIndex < Main.maxPlayers &&
            Main.player[PlayerIndex]?.active == true &&
            SpectatorTargetSystem.IsLockedTargeting(Main.player[PlayerIndex]);

        if (isSelected)
        {
            BackgroundColor = Color.Yellow;
            BorderColor = Color.Yellow;
        }
        else if (IsMouseHovering)
        {
            BackgroundColor = new Color(63, 82, 151) * 0.45f;
            //BorderColor = Colors.FancyUIFatButtonMouseOver * 0.3f;
            BorderColor = Colors.FancyUIFatButtonMouseOver*0.3f;
        }
        else
        {
            BackgroundColor = new Color(63, 82, 151) * 0.45f;
            BorderColor = Color.Black;
        }

        base.DrawSelf(sb);

        // Null checks
        if (PlayerIndex < 0 || PlayerIndex >= Main.maxPlayers)
            return;

        Player player = Main.player[PlayerIndex];

        if (player is null || !player.active)
            return;

        Rectangle rect = GetDimensions().ToRectangle();

        if (rect.Width <= 0 || rect.Height <= 0)
            return;

        // Layout
        int shrink = (int)MathF.Round(5f * scale);
        int buttonSize = (int)MathF.Round(32f * scale);
        int buttonGap = (int)MathF.Round(2f * scale);
        int buttonCount = 3;
        int buttonRowHeight = buttonSize;
        int buttonRowGap = (int)MathF.Round(3f * scale);
        int buttonContentWidth = buttonSize * buttonCount + buttonGap * (buttonCount - 1);
        int infoGap = (int)MathF.Round(6f * scale);
        int infoTopPadding = (int)MathF.Round(6f * scale);
        int infoRightPadding = (int)MathF.Round(8f * scale);

        Rectangle backgroundRect = rect;
        Rectangle contentRect = new(rect.X + shrink, rect.Y + shrink, rect.Width - shrink * 2, rect.Height - shrink * 2);

        Rectangle playerPreviewRect = new(
            contentRect.X,
            contentRect.Y,
            buttonContentWidth,
            contentRect.Height - buttonRowHeight - buttonRowGap);

        Rectangle infoRect = new(
            playerPreviewRect.Right + infoGap,
            contentRect.Y + infoTopPadding,
            contentRect.Right - playerPreviewRect.Right - infoGap - infoRightPadding,
            contentRect.Height - infoTopPadding * 2);

        Rectangle nameRect = new(infoRect.X, infoRect.Y - 2, infoRect.Width, (int)MathF.Round(24f * scale));

        // Draw biome BG
        BiomeBackgroundDrawer.DrawMapFullscreenBackground(sb, backgroundRect, player.Center, shrinkPadding: shrink);

        // Draw player preview background + player preview
        EntityDrawer.DrawEntityBackground(sb, playerPreviewRect);
        EntityDrawer.DrawPlayerCardPreview(sb, player, playerPreviewRect);

        // Draw player info to the right of the preview
        Rectangle healthRect = new(infoRect.X, nameRect.Bottom + (int)MathF.Round(2f * scale), infoRect.Width, (int)MathF.Round(27f * scale));

        // Draw name
        string name = PlayerIndex == Main.myPlayer ? "You" : player.name;
        float textScale = 1.0f;
        string displayName = StatDrawer.Truncate(FontAssets.MouseText.Value, name, nameRect.Width, textScale);
        Vector2 nameSize = FontAssets.MouseText.Value.MeasureString(displayName) * textScale;
        Vector2 namePosition = new(nameRect.X, nameRect.Y + (nameRect.Height - nameSize.Y) * 0.5f + 4f);

        Utils.DrawBorderString(sb, displayName, namePosition, Color.White, textScale);

        // --- Draw player stats ---
        int statH = (int)MathF.Round(27f * scale);
        int statG = (int)MathF.Round(3f * scale);

        // Stat 1: Life
        Rectangle stat1Rect = new(infoRect.X, nameRect.Bottom + (int)MathF.Round(2f * scale), infoRect.Width, statH);
        StatDrawer.DrawPlayerStat(sb, stat1Rect, PlayerStats.Life.Build(player), scale);

        // Stat 2: Mana
        Rectangle stat2Rect = new(infoRect.X, stat1Rect.Bottom + statG, infoRect.Width, statH);
        StatDrawer.DrawPlayerStat(sb, stat2Rect, PlayerStats.Mana.Build(player), scale);

        // Stat 3: Defense
        Rectangle stat3Rect = new(infoRect.X, stat2Rect.Bottom + statG, infoRect.Width, statH);
        StatDrawer.DrawPlayerStat(sb, stat3Rect, PlayerStats.Biome.Build(player), scale);

        // Draw health stat
        //StatDrawer.DrawBack(sb, healthRect);

        //string hp = $"{player.statLife}/{player.statLifeMax2}";
        //Texture2D heart = TextureAssets.Heart.Value;
        //Rectangle heartFrame = heart.Frame();
        //Rectangle heartArea = new(healthRect.X + 6, healthRect.Y + 5, 16, 16);

        //float heartScale = Math.Min(heartArea.Width / (float)heartFrame.Width, heartArea.Height / (float)heartFrame.Height);
        //Vector2 heartPosition = heartArea.Center.ToVector2();

        //sb.Draw(heart, heartPosition, heartFrame, Color.White, 0f, heartFrame.Size() * 0.5f, heartScale, SpriteEffects.None, 0f);

        //Rectangle hpTextArea = new(healthRect.X + 26, healthRect.Y + 4, healthRect.Width - 30, healthRect.Height - 8);
        //string displayHp = StatDrawer.Truncate(FontAssets.MouseText.Value, hp, hpTextArea.Width, 0.8f);

        //Utils.DrawBorderString(sb, displayHp, new Vector2(hpTextArea.X, hpTextArea.Y), Color.White, 0.8f);

        // Debug draw rectangles
        //DebugDrawer.DrawRectangle(playerPreviewRect, drawSize: true);
        //DebugDrawer.DrawRectangle(nameRect, drawSize: true);
        //DebugDrawer.DrawRectangle(stat1Rect, drawSize: true);
    }

    private void AddInventoryButton(float left, float top, float size)
    {
        InventoryButton button = new(PlayerIndex, owner);
        button.Left.Set(left, 0f);
        button.Top.Set(top, 0f);
        button.Width.Set(size, 0f);
        button.Height.Set(size, 0f);
        Append(button);
    }

    public static void TeleportToPlayer(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= Main.maxPlayers)
            return;

        Player target = Main.player[playerIndex];
        Player local = Main.LocalPlayer;

        if (target?.active != true || local?.active != true || target.whoAmI == local.whoAmI)
            return;

        Vector2 teleportPosition = target.Center - new Vector2(local.width, local.height) * 0.5f;

        if (Main.netMode == NetmodeID.SinglePlayer)
            local.Teleport(teleportPosition, TeleportationStyleID.RodOfDiscord);
        else if (Main.netMode == NetmodeID.MultiplayerClient)
            NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 2, local.whoAmI, teleportPosition.X, teleportPosition.Y, TeleportationStyleID.PotionOfReturn);
    }

    #region Action buttons
    private readonly record struct PlayerCardAction(
        Asset<Texture2D> Icon,
        Asset<Texture2D> SelectedIcon,
        string HoverText,
        string SelectedHoverText,
        Action<int> Click,
        Func<int, bool> Selected
    );
    private static PlayerCardAction[] GetPlayerCardActions()
    {
        return
        [
            new PlayerCardAction(
                Ass.Icon_GhostTeleport,
                Ass.Icon_GhostTeleport,
                "Teleport to player",
                "Teleport to player",
                TeleportToPlayer,
                static _ => false),
        new PlayerCardAction(
            Ass.Icon_Eye,
            Ass.Icon_Eye,
            "Spectate player",
            "Stop spectating",
            SpectatorTargetSystem.TogglePlayerTarget,
            static playerIndex => Main.player[playerIndex]?.active == true && SpectatorTargetSystem.IsLockedTargeting(Main.player[playerIndex])),

        new PlayerCardAction(
            Ass.Icon_InventoryClosed,
            Ass.Icon_InventoryOpen,
            "View inventory",
            "Close inventory",
            PlayerHudOverlay.Toggle,
            PlayerHudOverlay.IsOpen)
        ];
    }

    private void AddActionButtons(PlayerCardAction[] actions)
    {
        float buttonSize = 32f * scale;
        float buttonGap = 2f * scale;
        float buttonTop = CardHeight * scale - 5f * scale - buttonSize;
        float buttonLeft = 5f * scale;

        for (int i = 0; i < actions.Length; i++)
        {
            AddActionButton(actions[i], buttonLeft, buttonTop, buttonSize);
            buttonLeft += buttonSize + buttonGap;
        }
    }

    private void AddActionButton(PlayerCardAction action, float left, float top, float size)
    {
        PlayerCardActionButton button = new(PlayerIndex, owner, action);
        button.Left.Set(left, 0f);
        button.Top.Set(top, 0f);
        button.Width.Set(size, 0f);
        button.Height.Set(size, 0f);
        Append(button);
    }

    private sealed class PlayerCardActionButton : UIElement
    {
        private readonly int playerIndex;
        private readonly SpectatorControlsPanel owner;
        private readonly PlayerCardAction action;

        public PlayerCardActionButton(int playerIndex, SpectatorControlsPanel owner, PlayerCardAction action)
        {
            this.playerIndex = playerIndex;
            this.owner = owner;
            this.action = action;

            OnLeftClick += (evt, element) =>
            {
                if (IsValidPlayer())
                    action.Click(playerIndex);
            };

            OnMouseOver += (evt, element) => owner.SetStatusText(IsSelected() ? action.SelectedHoverText : action.HoverText);
            OnMouseOut += (evt, element) => owner.ResetStatusText();
        }

        protected override void DrawSelf(SpriteBatch sb)
        {
            Rectangle box = GetDimensions().ToRectangle();
            bool isSelected = IsSelected();

            Texture2D background = isSelected
                ? TextureAssets.InventoryBack14.Value
                : IsMouseHovering
                    ? TextureAssets.InventoryBack7.Value
                    : TextureAssets.InventoryBack.Value;

            Asset<Texture2D> iconAsset = isSelected && action.SelectedIcon is not null ? action.SelectedIcon : action.Icon;

            if (iconAsset is null)
                return;

            Texture2D icon = iconAsset.Value;
            float scale = Math.Min((box.Width - 8f) / icon.Width, (box.Height - 8f) / icon.Height);
            Color color = isSelected || IsMouseHovering ? Color.White : Color.White * 0.8f;

            sb.Draw(background, box, Color.White * 0.85f);
            sb.Draw(icon, box.Center.ToVector2(), null, color, 0f, icon.Size() * 0.5f, Math.Min(1f, scale), SpriteEffects.None, 0f);
        }

        private bool IsSelected()
        {
            return IsValidPlayer() && action.Selected(playerIndex);
        }

        private bool IsValidPlayer()
        {
            return playerIndex >= 0 && playerIndex < Main.maxPlayers && Main.player[playerIndex]?.active == true;
        }
    }

    #endregion

    private sealed class InventoryButton : UIElement
    {
        private readonly int playerIndex;
        private readonly SpectatorControlsPanel owner;

        public InventoryButton(int playerIndex, SpectatorControlsPanel owner)
        {
            this.playerIndex = playerIndex;
            this.owner = owner;

            OnLeftClick += (evt, element) =>
            {
                if (IsValidPlayer())
                    PlayerHudOverlay.Toggle(playerIndex);
            };

            OnMouseOver += (evt, element) => owner.SetStatusText(PlayerHudOverlay.IsOpen(playerIndex) ? "Close inventory" : "View inventory");
            OnMouseOut += (evt, element) => owner.ResetStatusText();
        }

        protected override void DrawSelf(SpriteBatch sb)
        {
            Rectangle box = GetDimensions().ToRectangle();
            bool isSelected = PlayerHudOverlay.IsOpen(playerIndex);

            Texture2D background = isSelected
                ? TextureAssets.InventoryBack14.Value
                : IsMouseHovering
                    ? TextureAssets.InventoryBack7.Value
                    : TextureAssets.InventoryBack.Value;

            Asset<Texture2D> iconAsset = isSelected ? Ass.Icon_InventoryOpen : Ass.Icon_InventoryClosed;
            Texture2D icon = iconAsset.Value;

            float scale = Math.Min((box.Width - 10f) / icon.Width, (box.Height - 4f) / icon.Height);
            Color color = isSelected || IsMouseHovering ? Color.White : Color.White * 0.8f;

            sb.Draw(background, box, Color.White * 0.85f);
            sb.Draw(icon, box.Center.ToVector2(), null, color, 0f, icon.Size() * 0.5f, Math.Min(1f, scale), SpriteEffects.None, 0f);
        }

        private bool IsValidPlayer()
        {
            return playerIndex >= 0 && playerIndex < Main.maxPlayers && Main.player[playerIndex]?.active == true;
        }
    }
}
