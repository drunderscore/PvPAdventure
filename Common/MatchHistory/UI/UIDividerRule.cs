using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace PvPAdventure.Common.MatchHistory.UI;

public sealed class UIDividerRule : UIElement
{
    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        CalculatedStyle dim = GetDimensions();

        Texture2D tex = UICommon.DividerTexture.Value;

        float scaleX = (dim.Width - 10f) / 8f;
        if (scaleX < 0f)
            scaleX = 0f;

        Vector2 pos = new(dim.X + 5f, dim.Y + (dim.Height - tex.Height) * 0.5f);

        spriteBatch.Draw(
            tex,
            pos,
            null,
            Color.White,
            0f,
            Vector2.Zero,
            new Vector2(scaleX, 1f),
            SpriteEffects.None,
            0f);
    }

    public override bool ContainsPoint(Vector2 point) => false;
}

