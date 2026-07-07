using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Map;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Spawnbox;

public sealed class SpawnBoxMap : ModMapLayer
{
    private static readonly Color BlockedColor = new(255, 80, 80);
    private static readonly Color PassableColor = new(70, 226, 158);

    public override void Draw(ref MapOverlayDrawContext context, ref string text)
    {
        if (Main.mapFullscreenScale < 0.5f)
            return;

        SpawnBoxSystem box = ModContent.GetInstance<SpawnBoxSystem>();
        Rectangle area = box.BorderOuterTileArea;
        Vector2 topLeft = (new Vector2(area.X, area.Y) - context.MapPosition) * context.MapScale + context.MapOffset;
        Vector2 size = new(area.Width * context.MapScale, area.Height * context.MapScale);
        Rectangle rect = new((int)topLeft.X, (int)topLeft.Y, (int)size.X, (int)size.Y);

        if (!Main.mapFullscreen && Main.mapStyle == 1)
        {
            rect = Rectangle.Intersect(rect, new Rectangle(Main.miniMapX, Main.miniMapY, Main.miniMapWidth, Main.miniMapHeight));
            if (rect.Width <= 0 || rect.Height <= 0)
                return;
        }

        DrawBorder(rect, GetDrawColor(box), Main.mapFullscreen ? (int)Main.mapFullscreenScale : 2);
    }

    private static Color GetDrawColor(SpawnBoxSystem box)
    {
        bool canCross = box.CanExit && box.TouchesWorldHitbox(Main.LocalPlayer.Hitbox);
        return (canCross ? PassableColor : BlockedColor) * (canCross ? 0.5f : 0.88f);
    }

    private static void DrawBorder(Rectangle r, Color color, int thickness)
    {
        if (thickness < 1)
            thickness = 1;

        if (r.Width <= thickness * 2 || r.Height <= thickness * 2)
            return;

        Texture2D pixel = TextureAssets.MagicPixel.Value;
        Main.spriteBatch.Draw(pixel, new Rectangle(r.X + thickness, r.Y, r.Width - thickness * 2, thickness), color);
        Main.spriteBatch.Draw(pixel, new Rectangle(r.X + thickness, r.Bottom - thickness, r.Width - thickness * 2, thickness), color);
        Main.spriteBatch.Draw(pixel, new Rectangle(r.X, r.Y, thickness, r.Height), color);
        Main.spriteBatch.Draw(pixel, new Rectangle(r.Right - thickness, r.Y, thickness, r.Height), color);
    }
}
