using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Spectator.Drawers;
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

internal sealed class UINPCCard : UIPanel
{
    internal static int CardWidth => UIPlayerCard.CardWidth;
    internal static int CardHeight => UIPlayerCard.CardHeight;

    public int NPCIndex { get; }
    public int ListIndex { get; }

    private readonly float scale;

    public UINPCCard(int npcIndex, int listIndex, float scale = 1f)
    {
        NPCIndex = npcIndex;
        ListIndex = listIndex;
        this.scale = scale;

        SetPadding(0f);
        OnLeftClick += (evt, _) =>
        {
            if (evt.Target != this)
                return;

            SpectatorTargetSystem.ToggleNPCTarget(NPCIndex);
        };

        AddActionButtons(GetNPCCardActions());
    }

    protected override void DrawSelf(SpriteBatch sb)
    {
        bool isSelected = IsValidNPC(NPCIndex) && SpectatorTargetSystem.IsLockedTargeting(Main.npc[NPCIndex]);

        if (isSelected)
        {
            BackgroundColor = Color.Yellow;
            BorderColor = Color.Yellow;
        }
        else if (IsMouseHovering)
        {
            BackgroundColor = new Color(63, 82, 151) * 0.45f;
            BorderColor = Colors.FancyUIFatButtonMouseOver * 0.3f;
        }
        else
        {
            BackgroundColor = new Color(63, 82, 151) * 0.45f;
            BorderColor = Color.Black;
        }

        base.DrawSelf(sb);

        if (!IsValidNPC(NPCIndex))
            return;

        NPC npc = Main.npc[NPCIndex];
        Rectangle rect = GetDimensions().ToRectangle();

        if (rect.Width <= 0 || rect.Height <= 0)
            return;

        int shrink = (int)MathF.Round(5f * scale);
        int buttonSize = (int)MathF.Round(32f * scale);
        int buttonGap = (int)MathF.Round(2f * scale);
        int buttonCountForLayout = 3;
        int buttonRowHeight = buttonSize;
        int buttonRowGap = (int)MathF.Round(3f * scale);
        int buttonContentWidth = buttonSize * buttonCountForLayout + buttonGap * (buttonCountForLayout - 1);
        int infoGap = (int)MathF.Round(6f * scale);
        int infoTopPadding = (int)MathF.Round(6f * scale);
        int infoRightPadding = (int)MathF.Round(8f * scale);

        Rectangle backgroundRect = rect;
        Rectangle contentRect = new(rect.X + shrink, rect.Y + shrink, rect.Width - shrink * 2, rect.Height - shrink * 2);

        Rectangle npcPreviewRect = new(
            contentRect.X,
            contentRect.Y,
            buttonContentWidth,
            contentRect.Height - buttonRowHeight - buttonRowGap);

        Rectangle infoRect = new(
            npcPreviewRect.Right + infoGap,
            contentRect.Y + infoTopPadding,
            contentRect.Right - npcPreviewRect.Right - infoGap - infoRightPadding,
            contentRect.Height - infoTopPadding * 2);

        Rectangle nameRect = new(infoRect.X, infoRect.Y - 2, infoRect.Width, (int)MathF.Round(24f * scale));

        BiomeBackgroundDrawer.DrawMapFullscreenBackground(sb, backgroundRect, npc.Center, shrinkPadding: shrink);
        EntityDrawer.DrawEntityBackground(sb, npcPreviewRect);
        EntityDrawer.DrawNPCPreview(sb, npc, npcPreviewRect);

        string displayName = StatDrawer.Truncate(FontAssets.MouseText.Value, npc.FullName, nameRect.Width, 1f);
        Vector2 nameSize = FontAssets.MouseText.Value.MeasureString(displayName);
        Vector2 namePosition = new(nameRect.X, nameRect.Y + (nameRect.Height - nameSize.Y) * 0.5f + 4f);

        Utils.DrawBorderString(sb, displayName, namePosition, Color.White);

        int statH = (int)MathF.Round(27f * scale);
        int statG = (int)MathF.Round(3f * scale);

        Rectangle stat1Rect = new(infoRect.X, nameRect.Bottom + (int)MathF.Round(2f * scale), infoRect.Width, statH);
        StatDrawer.DrawNPCStat(sb, stat1Rect, NPCStats.Life.Build(npc), scale);

        Rectangle stat2Rect = new(infoRect.X, stat1Rect.Bottom + statG, infoRect.Width, statH);
        StatDrawer.DrawNPCStat(sb, stat2Rect, NPCStats.WhoAmI.Build(npc), scale);

        Rectangle stat3Rect = new(infoRect.X, stat2Rect.Bottom + statG, infoRect.Width, statH);
        StatDrawer.DrawNPCStat(sb, stat3Rect, NPCStats.Defense.Build(npc), scale);
    }

    public static void TeleportToNPC(int npcIndex)
    {
        if (!IsValidNPC(npcIndex))
            return;

        NPC npc = Main.npc[npcIndex];
        Player local = Main.LocalPlayer;

        if (local?.active != true)
            return;

        local.Center = npc.Center - new Vector2(0f, local.height);
        local.velocity = Vector2.Zero;
        local.fallStart = (int)(local.position.Y / 16f);

        if (Main.netMode == NetmodeID.MultiplayerClient)
            NetMessage.SendData(MessageID.PlayerControls, -1, -1, null, local.whoAmI);
    }

    private readonly record struct NPCCardAction(
        Asset<Texture2D> Icon,
        Asset<Texture2D> SelectedIcon,
        string HoverText,
        string SelectedHoverText,
        Action<int> Click,
        Func<int, bool> Selected
    );

    private static NPCCardAction[] GetNPCCardActions()
    {
        return
        [
            new NPCCardAction(
                Ass.Icon_GhostTeleport,
                Ass.Icon_GhostTeleport,
                "Teleport to NPC",
                "Teleport to NPC",
                TeleportToNPC,
                static _ => false),

            new NPCCardAction(
                Ass.Icon_Eye,
                Ass.Icon_Eye,
                "Spectate NPC",
                "Stop spectating",
                SpectatorTargetSystem.ToggleNPCTarget,
                static npcIndex => IsValidNPC(npcIndex) && SpectatorTargetSystem.IsLockedTargeting(Main.npc[npcIndex]))
        ];
    }

    private void AddActionButtons(NPCCardAction[] actions)
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

    private void AddActionButton(NPCCardAction action, float left, float top, float size)
    {
        NPCCardActionButton button = new(NPCIndex, action);
        button.Left.Set(left, 0f);
        button.Top.Set(top, 0f);
        button.Width.Set(size, 0f);
        button.Height.Set(size, 0f);
        Append(button);
    }

    private static bool IsValidNPC(int npcIndex)
    {
        return npcIndex >= 0 && npcIndex < Main.maxNPCs && Main.npc[npcIndex]?.active == true;
    }

    private sealed class NPCCardActionButton : UIElement
    {
        private readonly int npcIndex;
        private readonly NPCCardAction action;

        public NPCCardActionButton(int npcIndex, NPCCardAction action)
        {
            this.npcIndex = npcIndex;
            this.action = action;

            OnLeftClick += (_, _) =>
            {
                if (IsValidNPC(npcIndex))
                    action.Click(npcIndex);
            };
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (IsMouseHovering)
            {
                Main.LocalPlayer.mouseInterface = true;
                Main.instance.MouseText(IsSelected() ? action.SelectedHoverText : action.HoverText);
            }
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
            float iconScale = Math.Min((box.Width - 8f) / icon.Width, (box.Height - 8f) / icon.Height);
            Color color = isSelected || IsMouseHovering ? Color.White : Color.White * 0.8f;

            sb.Draw(background, box, Color.White * 0.85f);
            sb.Draw(icon, box.Center.ToVector2(), null, color, 0f, icon.Size() * 0.5f, Math.Min(1f, iconScale), SpriteEffects.None, 0f);
        }

        private bool IsSelected()
        {
            return IsValidNPC(npcIndex) && action.Selected(npcIndex);
        }
    }
}
