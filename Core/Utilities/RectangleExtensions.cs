using Microsoft.Xna.Framework;

namespace PvPAdventure.Core.Utilities;

public static class RectangleExtensions
{
    public static Rectangle ToTileRectangle(this Rectangle rectangle) => new(
        rectangle.X / 16,
        rectangle.Y / 16,
        rectangle.Width / 16,
        rectangle.Height / 16
    );
}