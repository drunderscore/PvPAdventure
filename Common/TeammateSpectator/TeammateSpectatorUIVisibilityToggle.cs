using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Utilities;
using System;
using Terraria;
using Terraria.UI;

namespace PvPAdventure.Common.TeammateSpectator;

internal sealed class TeammateSpectatorUIVisibilityToggle : UIElement
{
    public const float SlotSize = 32f;

    private readonly float scale;
    private readonly Func<bool> getCardsVisible;
    private readonly Action toggleCardsVisible;

    internal TeammateSpectatorUIVisibilityToggle(float scale, Func<bool> getCardsVisible, Action toggleCardsVisible)
    {
        this.scale = scale;
        this.getCardsVisible = getCardsVisible;
        this.toggleCardsVisible = toggleCardsVisible;

        Width.Set(SlotSize * scale, 0f);
        Height.Set(SlotSize * scale, 0f);
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);
        toggleCardsVisible?.Invoke();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (IsMouseHovering)
            Main.LocalPlayer.mouseInterface = true;
    }

    protected override void DrawSelf(SpriteBatch sb)
    {
        Rectangle rect = GetDimensions().ToRectangle();

        Texture2D back = Main.Assets.Request<Texture2D>("Images/UI/CharCreation/SmallPanel").Value;
        Texture2D border = Main.Assets.Request<Texture2D>("Images/UI/CharCreation/SmallPanelBorder").Value;

        Vector2 center = rect.Center.ToVector2();
        float panelScale = rect.Width / (float)back.Width;
        Color backColor = Color.White * (IsMouseHovering ? 1f : 0.85f);
        Color iconColor = getCardsVisible() ? Color.White : Color.White * 0.45f;

        sb.Draw(back, center, null, backColor, 0f, back.Size() * 0.5f, panelScale, SpriteEffects.None, 0f);

        if (IsMouseHovering)
            sb.Draw(border, center, null, Color.White, 0f, border.Size() * 0.5f, panelScale, SpriteEffects.None, 0f);

        DrawEyeIcon(sb, rect, iconColor);
    }

    private static void DrawEyeIcon(SpriteBatch sb, Rectangle rect, Color color)
    {
        Texture2D texture = Ass.Icon_Eye.Value;

        Rectangle iconRect = rect;
        iconRect.Inflate(-6, -6);

        sb.Draw(texture, iconRect, color);
    }
}