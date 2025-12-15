using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Helpers;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace PvPAdventure.Common.Integrations.SharedUI;

public class ResizeButton : UIImageButton
{
    public bool draggingResize;

    private Vector2 _lastMouse;

    public event Action<float> OnDragX;
    public event Action<float> OnDragY;

    public ResizeButton(Asset<Texture2D> texture) : base(texture)
    {
        HAlign = 1f;
        VAlign = 1f;
        Left.Set(-2f, 0f);
        Top.Set(-2f, 0f);

        Width.Set(20f, 0f);
        Height.Set(20f, 0f);
    }

    public override void LeftMouseDown(UIMouseEvent evt)
    {
        base.LeftMouseDown(evt);

        draggingResize = true;
        _lastMouse = Main.MouseScreen;
    }

    public override void LeftMouseUp(UIMouseEvent evt)
    {
        base.LeftMouseUp(evt);
        draggingResize = false;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (!draggingResize)
            return;

        if (!Main.mouseLeft)
        {
            draggingResize = false;
            return;
        }

        Vector2 m = Main.MouseScreen;
        Vector2 delta = m - _lastMouse;
        _lastMouse = m;

        if (delta.X != 0f)
            OnDragX?.Invoke(delta.X);

        if (delta.Y != 0f)
            OnDragY?.Invoke(delta.Y);
    }

    protected override void DrawSelf(SpriteBatch sb)
    {
        //base.DrawSelf(sb);

        // Draw custom 20x20 (WxH) as defined in the constructor
        var rect = GetDimensions().ToRectangle();
        sb.Draw(Ass.Resize.Value, rect, Color.White);

        if (IsMouseHovering)
            Main.instance.MouseText("Resize");
    }
}
