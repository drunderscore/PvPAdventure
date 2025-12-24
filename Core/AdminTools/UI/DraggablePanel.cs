using Microsoft.Xna.Framework;
using PvPAdventure.Common;
using System;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace PvPAdventure.Core.AdminTools.UI;

/// <summary>
/// Class used to define common properties for admin tool window panels,
/// such as 
/// <see cref="PointsSetter"/> 
/// <seealso cref="GameStarter"/>, etc.
/// </summary>
public abstract class DraggablePanel : UIElement
{
    // Dragging
    private bool dragging;
    private Vector2 dragOffset;

    // Content
    protected UIPanel TitlePanel;
    protected UIPanel ContentPanel;
    protected UIPanel RefreshPanel;
    protected UIPanel ClosePanel;
    protected ResizeButton ResizeButton;

    // Overridable properties
    protected abstract void OnClosePanelLeftClick();
    protected virtual void OnRefreshPanelLeftClick() { }
    protected virtual float MinResizeW => 350f;
    protected virtual float MinResizeH => 210f;
    protected virtual float MaxResizeW => 1000f;
    protected virtual float MaxResizeH => 1000f;

    // Constructor sets the style of this panel.
    public DraggablePanel(string title)
    {
        // Size and position
        Width.Set(350, 0);
        Height.Set(460, 0);
        Top.Set(0, 0);
        Left.Set(0, 0);
        VAlign = 0.7f;
        HAlign = 0.9f;
        SetPadding(0);

        TitlePanel = new();
        TitlePanel.Height.Set(40, 0);
        TitlePanel.Width.Set(0, 1);
        TitlePanel.SetPadding(0);
        TitlePanel.BackgroundColor = new Color(63, 82, 151) * 1f;

        UIText titleText = new(title, large: true, textScale: 0.7f)
        {
            HAlign = 0.5f,
            VAlign = 0.5f
        };
        TitlePanel.Append(titleText);

        ContentPanel = new UIPanel
        {
            Top = new StyleDimension(40, 0),
            Width = new StyleDimension(0, 1),
            Height = new StyleDimension(-40, 1),
            BackgroundColor = new Color(20, 20, 60) * 0.7f,
            BorderColor = Color.Black
        };
        ContentPanel.SetPadding(0);
        Append(ContentPanel);

        ClosePanel = new UIPanel
        {
            Height = new StyleDimension(0, 1),
            Width = new StyleDimension(40, 0),
            HAlign = 1f,
            VAlign = 0.5f
        };
        ClosePanel.OnLeftClick += (_, _) => OnClosePanelLeftClick();
        ClosePanel.OnMouseOver += (_, _) => ClosePanel.BorderColor = Color.Yellow;
        ClosePanel.OnMouseOut += (_, _) => ClosePanel.BorderColor = Color.Black;

        var closeText = new UIText("X", large: true, textScale: 0.55f)
        {
            HAlign = 0.5f,
            VAlign = 0.5f
        };

        ClosePanel.Append(closeText);
        TitlePanel.Append(ClosePanel);
        Append(TitlePanel);

        // Refresh panel
        RefreshPanel = new()
        {
            Height = new StyleDimension(0, 1),
            Width = new StyleDimension(40, 0),
            VAlign = 0.5f
        };
        RefreshPanel.OnLeftClick += (_, _) => OnRefreshPanelLeftClick();
        RefreshPanel.OnMouseOver += (_, _) =>
        {
            RefreshPanel.BorderColor = Color.Yellow;
        };
        RefreshPanel.OnMouseOut += (_, _) => RefreshPanel.BorderColor = Color.Black;
        RefreshPanel.SetPadding(0);
        RefreshPanel.Append(new UIImage(Ass.Reset.Value)
        {
            HAlign = 0.5f,
            VAlign = 0.5f
        });
        TitlePanel.Append(RefreshPanel);

        // Resize
        ResizeButton = new(Ass.Resize);

        ResizeButton.OnDragX += dx =>
        {
            if (Parent == null)
                return;

            if (HAlign != 0f || VAlign != 0f || Left.Percent != 0f || Top.Percent != 0f)
            {
                var p = Parent.GetDimensions();
                var d = GetDimensions();

                HAlign = 0f;
                VAlign = 0f;
                Left.Percent = 0f;
                Top.Percent = 0f;

                Left.Pixels = d.X - p.X;
                Top.Pixels = d.Y - p.Y;

                Recalculate();
            }

            var parent = Parent.GetDimensions();

            float maxW = Math.Min(MaxResizeW, parent.Width - Left.Pixels);
            float w = Utils.Clamp(Width.Pixels + dx, MinResizeW, maxW);

            Width.Set(w, 0f);
            Recalculate();
        };

        ResizeButton.OnDragY += dy =>
        {
            if (Parent == null)
                return;

            if (HAlign != 0f || VAlign != 0f || Left.Percent != 0f || Top.Percent != 0f)
            {
                var p = Parent.GetDimensions();
                var d = GetDimensions();

                HAlign = 0f;
                VAlign = 0f;
                Left.Percent = 0f;
                Top.Percent = 0f;

                Left.Pixels = d.X - p.X;
                Top.Pixels = d.Y - p.Y;

                Recalculate();
            }

            var parent = Parent.GetDimensions();

            float maxH = Math.Min(MaxResizeH, parent.Height - Top.Pixels);
            float h = Utils.Clamp(Height.Pixels + dy, MinResizeH, Math.Max(MinResizeH, maxH));

            Height.Set(h, 0f);
            Recalculate();
        };

        Append(ResizeButton);
    }

    #region Dragging
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (IsMouseHovering)
            Main.LocalPlayer.mouseInterface = true;

        if (RefreshPanel.IsMouseHovering)
            Main.instance.MouseText("Refresh");

        if (Parent == null)
            return;

        if (ClosePanel.IsMouseHovering || RefreshPanel.IsMouseHovering || ResizeButton.IsMouseHovering)
            return;

        if (dragging)
        {
            var parent = Parent.GetDimensions();

            Left.Pixels = Main.mouseX - dragOffset.X - parent.X;
            Top.Pixels = Main.mouseY - dragOffset.Y - parent.Y;

            // Clamp to screen
            var dims = GetDimensions();
            Left.Pixels = Utils.Clamp(Left.Pixels, 0f, Math.Max(0f, parent.Width - dims.Width));
            Top.Pixels = Utils.Clamp(Top.Pixels, 0f, Math.Max(0f, parent.Height - dims.Height));
            Recalculate();
        }

        // Ensure panel stays on screen
        var parentSpace = Parent.GetDimensions().ToRectangle();
        if (!GetDimensions().ToRectangle().Intersects(parentSpace))
        {
            Left.Pixels = Utils.Clamp(Left.Pixels, 0, parentSpace.Right - Width.Pixels);
            Top.Pixels = Utils.Clamp(Top.Pixels, 0, parentSpace.Bottom - Height.Pixels);
            Recalculate();
        }
    }

    public override void LeftMouseDown(UIMouseEvent evt)
    {
        base.LeftMouseDown(evt);

        if (ClosePanel.IsMouseHovering || RefreshPanel.IsMouseHovering || ResizeButton.IsMouseHovering)
            return;

        if (TitlePanel == null || !TitlePanel.ContainsPoint(evt.MousePosition) || Parent == null)
            return;

        if (HAlign != 0f || VAlign != 0f || Left.Percent != 0f || Top.Percent != 0f)
        {
            var p = Parent.GetDimensions();
            var d = GetDimensions();

            HAlign = 0f;
            VAlign = 0f;
            Left.Percent = 0f;
            Top.Percent = 0f;

            Left.Pixels = d.X - p.X;
            Top.Pixels = d.Y - p.Y;

            Recalculate();
        }

        dragging = true;
        dragOffset = evt.MousePosition - GetDimensions().Position();
    }

    public override void LeftMouseUp(UIMouseEvent evt)
    {
        base.LeftMouseUp(evt);
        dragging = false;
    }
    #endregion
}
