using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace PvPAdventure.Core.SSC.UI;

public class DraggableElement : UIPanel
{
    bool moving;
    Vector2 offset;

    public void BeginDrag(UIMouseEvent evt)
    {
        moving = true;
        offset = new Vector2(evt.MousePosition.X - Left.Pixels, evt.MousePosition.Y - Top.Pixels);
    }

    public void EndDrag(UIMouseEvent evt)
    {
        moving = false;
        Left.Set(evt.MousePosition.X - offset.X, 0);
        Top.Set(evt.MousePosition.Y - offset.Y, 0);
        Recalculate();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (ContainsPoint(Main.MouseScreen))
        {
            Main.LocalPlayer.mouseInterface = true;
        }

        if (moving)
        {
            Left.Set(Main.mouseX - offset.X, 0f);
            Top.Set(Main.mouseY - offset.Y, 0f);
            Recalculate();
        }

        if (moving && !Main.mouseLeft)
        {
            moving = false;
            Left.Set(Main.mouseX - offset.X, 0f);
            Top.Set(Main.mouseY - offset.Y, 0f);
            Recalculate();
        }

        var space = Parent?.GetViewCullingArea();
        var area = GetViewCullingArea();
        if (space == null)
        {
            return;
        }

        if (!space.Value.Contains(area))
        {
            if (area.X < space.Value.X)
            {
                Left.Pixels += space.Value.X - area.X;
            }

            if (area.X + area.Width > space.Value.Width)
            {
                Left.Pixels -= area.X + area.Width - space.Value.Width;
            }

            if (area.Y < space.Value.Y)
            {
                Top.Pixels += space.Value.Y - area.Y;
            }

            if (area.Y + area.Height > space.Value.Height)
            {
                Top.Pixels -= area.Y + area.Height - space.Value.Height;
            }

            Recalculate();
        }
    }
}
