using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.UI;
namespace PvPAdventure.Common.Config.Elements;

/// <summary>
/// Used in <see cref="CustomDictionaryElement"/> to display Projectiles and NPC's
/// </summary>
internal sealed class UIDefinitionIcon : UIElement
{
    private readonly Asset<Texture2D> _texture;
    private readonly Rectangle _source;

    public UIDefinitionIcon(Asset<Texture2D> texture, Rectangle source)
    {
        _texture = texture;
        _source = source;
        Width.Set(16f, 0f);
        Height.Set(16f, 0f);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);

        if (_texture == null || !_texture.IsLoaded)
            return;

        var tex = _texture.Value;
        var dims = GetDimensions();

        float scaleX = dims.Width / _source.Width;
        float scaleY = dims.Height / _source.Height;
        float scale = MathHelper.Min(scaleX, scaleY);

        var drawSize = new Vector2(_source.Width, _source.Height) * scale;
        var pos = new Vector2(dims.X, dims.Y) + (new Vector2(dims.Width, dims.Height) - drawSize) * 0.5f;

        spriteBatch.Draw(tex, pos, _source, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }
}

