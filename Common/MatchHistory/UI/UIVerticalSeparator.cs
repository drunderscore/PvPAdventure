using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.UI;

namespace PvPAdventure.Common.MatchHistory.UI;

public class UIVerticalSeparator : UIElement
{
    private readonly Asset<Texture2D> _texture;

    public Color Color;
    public int EdgeWidth;

    public UIVerticalSeparator(int edgeWidth = 2, bool highlightSideLeft = true)
    {
        Color = Color.White;
        EdgeWidth = 4;

        _texture = Main.Assets.Request<Texture2D>(
            highlightSideLeft
                ? "Images/UI/CharCreation/Separator1"
                : "Images/UI/CharCreation/Separator2");

        Width.Set(4, 0f);              
        Height.Set(_texture.Height(), 0f);     
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        CalculatedStyle dim = GetDimensions();

        Texture2D tex = _texture.Value;
        Rectangle src = new(0, 0, tex.Width, tex.Height);
        float scaleX = dim.Height / tex.Width;
        float scaleY = dim.Width / tex.Height;
        Vector2 scale = new(scaleX, scaleY);
        Vector2 pos = new(dim.X + dim.Width, dim.Y);

        spriteBatch.Draw(tex, pos, src, Color, MathHelper.PiOver2, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    public override bool ContainsPoint(Vector2 point) => false;
}
