//using Microsoft.Xna.Framework;
//using Terraria;
//using Terraria.UI;

//namespace PvPAdventure.Core.SSC.UI;

///// <summary>
///// Drags this element (and therefore all children) by converting alignment-based layout to pixel layout on drag start.
///// </summary>
//public class DraggableElement : UIElement
//{
//    bool moving;
//    Vector2 offset;

//    public void BeginDrag(UIMouseEvent evt)
//    {
//        moving = true;

//        // Freeze current layout into pixel-based Left/Top so HAlign/VAlign don't fight dragging.
//        var dims = GetDimensions();
//        var parentDims = Parent?.GetDimensions();

//        if (parentDims != null)
//        {
//            HAlign = 0f;
//            VAlign = 0f;

//            Left.Set(dims.X - parentDims.Value.X, 0f);
//            Top.Set(dims.Y - parentDims.Value.Y, 0f);
//            Recalculate();
//        }

//        // Offset is always computed in screen-space.
//        offset = evt.MousePosition - dims.Position();
//    }

//    public void EndDrag(UIMouseEvent evt)
//    {
//        moving = false;
//        UpdatePositionFromMouse(Main.MouseScreen);
//    }

//    public override void Update(GameTime gameTime)
//    {
//        base.Update(gameTime);

//        if (ContainsPoint(Main.MouseScreen))
//        {
//            Main.LocalPlayer.mouseInterface = true;
//        }

//        if (moving)
//        {
//            UpdatePositionFromMouse(Main.MouseScreen);
//        }

//        if (moving && !Main.mouseLeft)
//        {
//            moving = false;
//            UpdatePositionFromMouse(Main.MouseScreen);
//        }

//        // Clamp to parent view
//        //var space = Parent?.GetViewCullingArea();
//        //var area = GetViewCullingArea();
//        //if (space == null)
//        //{
//        //    return;
//        //}

//        //if (!space.Value.Contains(area))
//        //{
//        //    if (area.X < space.Value.X)
//        //    {
//        //        Left.Pixels += space.Value.X - area.X;
//        //    }

//        //    if (area.X + area.Width > space.Value.Width)
//        //    {
//        //        Left.Pixels -= area.X + area.Width - space.Value.Width;
//        //    }

//        //    if (area.Y < space.Value.Y)
//        //    {
//        //        Top.Pixels += space.Value.Y - area.Y;
//        //    }

//        //    if (area.Y + area.Height > space.Value.Height)
//        //    {
//        //        Top.Pixels -= area.Y + area.Height - space.Value.Height;
//        //    }

//        //    Recalculate();
//        //}
//    }

//    private void UpdatePositionFromMouse(Vector2 mouseScreen)
//    {
//        var parentDims = Parent?.GetDimensions();
//        if (parentDims == null)
//        {
//            return;
//        }

//        Left.Set(mouseScreen.X - parentDims.Value.X - offset.X, 0f);
//        Top.Set(mouseScreen.Y - parentDims.Value.Y - offset.Y, 0f);
//        Recalculate();
//    }
//}
