using Microsoft.Xna.Framework;
using PvPAdventure.Core.Helpers;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace PvPAdventure.Common.Integrations.SharedUI;
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
    }

    /// <summary>
    /// Action taken when pressing "close" in the top right corner of the element.
    /// Usually toggling the parent system's state with e.g "ExampleState.setState(null)"
    /// </summary>
    public abstract void OnClosePanelLeftClick();

    /// <summary>
    /// Action taken when pressing "refresh" in the top left corner of the element.
    /// Usually refreshing the ContentPanel.
    /// </summary>
    public virtual void OnRefreshPanelLeftClick() { }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (IsMouseHovering)
            Main.LocalPlayer.mouseInterface = true;

        if (RefreshPanel.IsMouseHovering)
            Main.instance.MouseText("Refresh");

        UpdateDragging();
    }

    private void UpdateDragging()
    {
        if (dragging)
        {
            float x = Main.mouseX - dragOffset.X;
            float y = Main.mouseY - dragOffset.Y;

            var d = GetDimensions();
            float w = d.Width;
            float h = d.Height;

            // Prevent out of bounds when dragging
            //if (x < 0) x = 0;
            //else if (x + w > Main.screenWidth) x = Main.screenWidth - w;

            //if (y < 0) y = 0;
            //else if (y + h > Main.screenHeight) y = Main.screenHeight - h;

            Left.Pixels = x;
            Top.Pixels = y;
            Recalculate();
        }

        // Prevent out of bounds when resizing the window
        Rectangle parentSpace = Parent.GetDimensions().ToRectangle();
        if (!GetDimensions().ToRectangle().Intersects(parentSpace))
        {
            Left.Pixels = Utils.Clamp(Left.Pixels, 0, parentSpace.Right - Width.Pixels);
            Top.Pixels = Utils.Clamp(Top.Pixels, 0, parentSpace.Bottom - Height.Pixels);
            Recalculate();
        }
    }

    public override void LeftMouseDown(UIMouseEvent evt)
    {
        if (TitlePanel != null && TitlePanel.ContainsPoint(evt.MousePosition))
        {
            dragging = true;
            dragOffset = evt.MousePosition - new Vector2(Left.Pixels, Top.Pixels);
        }
    }

    public override void LeftMouseUp(UIMouseEvent evt)
    {
        dragging = false;
        Recalculate();
    }
}
