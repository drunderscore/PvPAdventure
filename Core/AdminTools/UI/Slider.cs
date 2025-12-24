using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;

namespace PvPAdventure.Core.AdminTools.UI;

public class Slider : UIElement
{
    public Asset<Texture2D> InnerTexture;
    public Asset<Texture2D> OuterTexture;
    public bool IsHeld;
    public float Ratio;
    public event Action<float> OnDrag;

    public Slider()
    {
        Width.Set(0, 1f);
        Height.Set(16, 0f);
        InnerTexture = Ass.SliderGradient;
        OuterTexture = Ass.SliderHighlight;
    }

    public override void LeftMouseDown(UIMouseEvent evt)
    {
        base.LeftMouseDown(evt);
        if (evt.Target == this)
            IsHeld = true;
    }

    public override void MouseOver(UIMouseEvent evt)
    {
        base.MouseOver(evt);
        SoundEngine.PlaySound(SoundID.MenuTick);
    }

    public override void LeftMouseUp(UIMouseEvent evt)
    {
        base.LeftMouseUp(evt);
        IsHeld = false;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        if (IsHeld)
        {
            var dims = GetDimensions();
            float num = Main.MouseScreen.X - dims.X;
            float newRatio = MathHelper.Clamp(num / dims.Width, 0f, 1f);
            if (Math.Abs(newRatio - Ratio) > float.Epsilon)
            {
                Ratio = newRatio;
                OnDrag?.Invoke(Ratio);
            }
        }
    }

    protected override void DrawSelf(SpriteBatch sb)
    {
        Rectangle rect = GetDimensions().ToRectangle();
        DrawBar(sb, Ass.Slider.Value, rect, Color.White);
        if (IsHeld || IsMouseHovering)
            DrawBar(sb, OuterTexture.Value, rect, Main.OurFavoriteColor);
        Rectangle innerBarArea = rect;
        innerBarArea.Inflate(-4, -4);
        sb.Draw(InnerTexture.Value, innerBarArea, Color.White);
        Texture2D blip = TextureAssets.ColorSlider.Value;
        Vector2 blipOrigin = blip.Size() * 0.5f;
        Vector2 blipPosition = new(innerBarArea.X + Ratio * innerBarArea.Width, innerBarArea.Center.Y);
        sb.Draw(blip, blipPosition, null, Color.White, 0f, blipOrigin, 1f, SpriteEffects.None, 0f);
    }

    public static void DrawBar(SpriteBatch spriteBatch, Texture2D texture, Rectangle dimensions, Color color)
    {
        if (texture == null) return;
        spriteBatch.Draw(texture, new Rectangle(dimensions.X, dimensions.Y, 6, dimensions.Height), new Rectangle(0, 0, 6, texture.Height), color);
        spriteBatch.Draw(texture, new Rectangle(dimensions.X + 6, dimensions.Y, dimensions.Width - 12, dimensions.Height), new Rectangle(6, 0, 2, texture.Height), color);
        spriteBatch.Draw(texture, new Rectangle(dimensions.X + dimensions.Width - 6, dimensions.Y, 6, dimensions.Height), new Rectangle(8, 0, 6, texture.Height), color);
    }
}
