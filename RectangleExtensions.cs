using Microsoft.Xna.Framework;

namespace PvPAdventure;

public static class RectangleExtensions
{
    public static Rectangle ToTileRectangle(this Rectangle r)
    {
        int leftTile = r.Left / 16;
        int topTile = r.Top / 16;
        int rightTile = (r.Right - 1) / 16;
        int bottomTile = (r.Bottom - 1) / 16;

        return new Rectangle(
            leftTile,
            topTile,
            rightTile - leftTile + 1,
            bottomTile - topTile + 1
        );
    }
}