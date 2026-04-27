using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Spectator.Drawers;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace PvPAdventure.Common.Spectator.UI.Tabs;

internal abstract class SpectatorEntityEntry : UIBrowserEntry
{
    private const int ButtonSize = 27;
    private const int ButtonGap = 4;
    private const int ButtonCount = 3;
    private const int ButtonContentWidth = ButtonSize * ButtonCount + ButtonGap * (ButtonCount - 1);
    private const int ButtonBottomPadding = 4;
    private const int NameHeight = 18;

    protected abstract string EntityName { get; }

    private readonly UIElement listChrome;
    private readonly UIElement buttonRow;
    private readonly List<SpectatorEntityButton> buttons = [];

    private bool initialized;
    private string hoveredStatText;

    protected abstract void DrawListPreview(SpriteBatch spriteBatch, Rectangle area);
    protected abstract string DrawListStats(SpriteBatch spriteBatch, Rectangle area);
    protected abstract string DrawHeadStat(SpriteBatch spriteBatch, Rectangle area);
    protected abstract string DrawGridStats(SpriteBatch spriteBatch, Rectangle area, int columns, int rows, int statHeight, int statSpacing);

    protected SpectatorEntityEntry()
    {
        listChrome = new UIElement();
        listChrome.Width.Set(0f, 1f);
        listChrome.Height.Set(0f, 1f);
        Append(listChrome);

        buttonRow = new UIElement();
        buttonRow.Height.Set(ButtonSize, 0f);
        listChrome.Append(buttonRow);
    }

    public override void SetListMode(bool value)
    {
        listMode = value;

        if (initialized)
            ApplyLayout();
    }

    public override void SetEntrySize(int size)
    {
        entrySize = size;

        if (initialized)
            ApplyLayout();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
    }

    protected void FinishSetup()
    {
        initialized = true;
        ApplyLayout();
    }

    protected void AddEntityButton(Asset<Texture2D> texture, ref float leftOffset, string label, UIElement.MouseEvent click = null, Func<bool> selected = null)
    {
        SpectatorEntityButton button = new(texture, label, selected);
        button.Width.Set(ButtonSize, 0f);
        button.Height.Set(ButtonSize, 0f);
        button.Left.Set(leftOffset, 0f);
        button.Top.Set(0f, 0f);

        if (click != null)
            button.OnLeftClick += click;

        buttonRow.Append(button);
        buttons.Add(button);

        leftOffset += ButtonSize + ButtonGap;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        Rectangle oldScissor = spriteBatch.GraphicsDevice.ScissorRectangle;
        RasterizerState oldRasterizer = spriteBatch.GraphicsDevice.RasterizerState;
        Rectangle clip = Rectangle.Intersect(oldScissor, GetClippingRectangle(spriteBatch));

        if (clip.Width <= 0 || clip.Height <= 0)
            return;

        spriteBatch.End();
        spriteBatch.GraphicsDevice.ScissorRectangle = clip;
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, UIElement.OverflowHiddenRasterizerState, null, Main.UIScaleMatrix);

        base.Draw(spriteBatch);

        spriteBatch.End();
        spriteBatch.GraphicsDevice.ScissorRectangle = oldScissor;
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, oldRasterizer, null, Main.UIScaleMatrix);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        Rectangle box = GetDimensions().ToRectangle();
        hoveredStatText = null;

        Utils.DrawInvBG(spriteBatch, box, Color.Black * 0.35f);
        Utils.DrawInvBG(spriteBatch, box, IsMouseHovering ? new Color(73, 94, 171, 185) : new Color(63, 82, 151, 145));

        if (listMode)
        {
            GetListLayout(box, out Rectangle previewBox, out Rectangle nameBox, out Rectangle entityBox, out Rectangle statsBox);

            EntityDrawer.DrawEntityBackground(spriteBatch, previewBox);
            DrawEntityName(spriteBatch, nameBox, previewBox);

            if (previewBox.Height >= 30)
                DrawListPreview(spriteBatch, entityBox);

            if (statsBox.Width > 0 && statsBox.Height > 0)
                hoveredStatText = DrawListStats(spriteBatch, statsBox) ?? hoveredStatText;
        }
        else
        {
            DrawGridMode(spriteBatch, box);
        }

        if (!IsEntityButtonHovered() && !string.IsNullOrEmpty(hoveredStatText))
            UICommon.TooltipMouseText(hoveredStatText);
    }

    private bool IsEntityButtonHovered() => buttons.Exists(static button => button.IsMouseHovering);

    private void DrawEntityName(SpriteBatch spriteBatch, Rectangle area, Rectangle hoverArea)
    {
        const float scale = 1f;
        const float textYOffset = 2f;

        string fullText = EntityName;
        string text = StatDrawer.Truncate(FontAssets.MouseText.Value, fullText, area.Width, scale);
        Vector2 size = FontAssets.MouseText.Value.MeasureString(text) * scale;
        Vector2 position = new(area.X + (area.Width - size.X) * 0.5f, area.Y + (area.Height - size.Y) * 0.5f + textYOffset);

        Utils.DrawBorderString(spriteBatch, text, position, Color.White, scale);

        if (hoverArea.Contains(Main.MouseScreen.ToPoint()))
        {
            Main.LocalPlayer.mouseInterface = true;
            Main.instance.MouseText(fullText);
        }
    }

    private void ApplyLayout()
    {
        if (listMode)
        {
            Width.Set(0f, 1f);
            Height.Set(entrySize, 0f);

            if (listChrome.Parent is null)
                Append(listChrome);

            Point previewSize = GetPreviewSize(GetDimensions().ToRectangle());
            buttonRow.Width.Set(ButtonContentWidth, 0f);
            buttonRow.Height.Set(ButtonSize, 0f);
            buttonRow.Left.Set(4f, 0f);
            buttonRow.Top.Set(4f + previewSize.Y + 2f, 0f);

            if (buttonRow.Parent is null)
                listChrome.Append(buttonRow);
        }
        else
        {
            Width.Set(entrySize, 0f);
            Height.Set(entrySize, 0f);
            listChrome.Remove();
        }

        Recalculate();
    }

    private void GetListLayout(Rectangle box, out Rectangle previewBox, out Rectangle nameBox, out Rectangle entityBox, out Rectangle statsBox)
    {
        Point previewSize = GetPreviewSize(box);

        previewBox = new Rectangle(box.X + 4, box.Y + 4, previewSize.X, previewSize.Y);
        nameBox = new Rectangle(previewBox.X + 2, previewBox.Y + 2, previewBox.Width - 4, NameHeight);
        entityBox = new Rectangle(previewBox.X, nameBox.Bottom + 1, previewBox.Width, Math.Max(0, previewBox.Bottom - nameBox.Bottom - 1));
        statsBox = new Rectangle(previewBox.Right + 5, box.Y + 4, box.Right - previewBox.Right - 14, box.Height - 8);
    }

    private Point GetPreviewSize(Rectangle box)
    {
        int availableHeight = Math.Max(1, entrySize - ButtonSize - ButtonBottomPadding - 10);
        return new Point(ButtonContentWidth, availableHeight);
    }
    private void DrawGridMode(SpriteBatch spriteBatch, Rectangle box)
    {
        const int outerPadding = 6;
        const int statSpacing = 2;
        const int statHeight = 27;

        int totalRows = Math.Max(0, (box.Height - outerPadding * 2 + statSpacing) / (statHeight + statSpacing));

        if (totalRows <= 0)
            return;

        Rectangle headStatBox = new(box.X + outerPadding, box.Y + outerPadding, box.Width - outerPadding * 2, statHeight);
        hoveredStatText = DrawHeadStat(spriteBatch, headStatBox) ?? hoveredStatText;

        int statRows = Math.Max(0, totalRows - 1);

        if (statRows <= 0)
            return;

        Rectangle statArea = new(box.X + outerPadding, headStatBox.Bottom + statSpacing, box.Width - outerPadding * 2, box.Bottom - outerPadding - headStatBox.Bottom - statSpacing);
        hoveredStatText = DrawGridStats(spriteBatch, statArea, StatDrawer.GetGridColumns(statArea), statRows, statHeight, statSpacing) ?? hoveredStatText;
    }

    
    private sealed class SpectatorEntityButton : UIElement
    {
        private readonly Asset<Texture2D> texture;
        private readonly string hoverText;
        private readonly Func<bool> selected;

        public SpectatorEntityButton(Asset<Texture2D> texture, string hoverText, Func<bool> selected)
        {
            this.texture = texture;
            this.hoverText = hoverText;
            this.selected = selected;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            Rectangle box = GetDimensions().ToRectangle();
            bool isSelected = selected?.Invoke() == true;

            spriteBatch.Draw((isSelected ? TextureAssets.InventoryBack14 : TextureAssets.InventoryBack).Value, box, Color.White * 0.8f);

            if (texture == null)
            {
                Log.Warn("Icon not found, returning");
                return;
            }

            Texture2D icon = texture.Value;

            float scale = Math.Min((box.Width - 8f) / icon.Width, (box.Height - 8f) / icon.Height);
            spriteBatch.Draw(icon, box.Center.ToVector2(), null, IsMouseHovering || isSelected ? Color.White : Color.White * 0.8f, 0f, icon.Size() * 0.5f, Math.Min(1f, scale), SpriteEffects.None, 0f);

            if (IsMouseHovering)
                Main.instance.MouseText(hoverText);
        }
    }
}