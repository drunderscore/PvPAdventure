using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader.Config.UI;
using Terraria.UI;
using Terraria.UI.Chat;

namespace PvPAdventure.Core.ConfigElements;

/// <summary>
/// A bool config element
/// </summary>
public abstract class BaseBoolConfigElement : ConfigElement<bool>
{
    private Asset<Texture2D> toggleTexture = Asset<Texture2D>.Empty;

    public override void OnBind()
    {
        base.OnBind();
        toggleTexture = Main.Assets.Request<Texture2D>("Images/UI/Settings_Toggle");

        OnLeftClick += delegate
        {
            Value = !Value;
            OnToggled(Value);
        };
    }

    protected abstract void OnToggled(bool newValue);

    public override void Draw(SpriteBatch sb)
    {
        base.Draw(sb);

        DrawPreview(sb);

        sb.End();
        sb.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.LinearClamp,
            DepthStencilState.None,
            Main.Rasterizer,
            null,
            Main.UIScaleMatrix
        );

        DrawToggleTexture(sb);
        DrawOnOffText(sb);
    }

    protected virtual void DrawPreview(SpriteBatch sb)
    {
         // Subclasses can override to draw their preview 
    }

    private void DrawToggleTexture(SpriteBatch sb)
    {
        Rectangle sourceRectangle = new Rectangle(
            Value ? (toggleTexture.Width() - 2) / 2 + 2 : 0,
            0,
            (toggleTexture.Width() - 2) / 2,
            toggleTexture.Height()
        );

        CalculatedStyle dimensions = GetDimensions();

        sb.Draw(
            toggleTexture.Value,
            new Vector2(
                dimensions.X + dimensions.Width - sourceRectangle.Width - 10f,
                dimensions.Y + 8f
            ),
            sourceRectangle,
            Color.White
        );
    }

    private void DrawOnOffText(SpriteBatch sb)
    {
        CalculatedStyle dimensions = GetDimensions();
        string label = Value ? Lang.menu[126].Value : Lang.menu[124].Value;

        Vector2 pos = new Vector2(dimensions.X + dimensions.Width - 60f, dimensions.Y + 8f);
        ChatManager.DrawColorCodedStringWithShadow(
            sb,
            FontAssets.ItemStack.Value,
            label,
            pos,
            Color.White,
            0f,
            Vector2.Zero,
            new Vector2(0.8f)
        );
    }
}
