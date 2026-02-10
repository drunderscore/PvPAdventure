using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Utilities;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;
using Terraria.UI.Chat;

namespace PvPAdventure.Common.MatchHistory.Achievements;

internal sealed class UIAchievementRow : UIPanel
{
    private const int IconSize = 64;

    private static Asset<Texture2D>? Borders;
    private static Asset<Texture2D>? InnerTop;
    private static Asset<Texture2D>? InnerBottom;

    private readonly string id;
    private readonly Achievement def;

    public UIAchievementRow(string id, Achievement def)
    {
        this.id = id;
        this.def = def;

        BackgroundColor = new Color(26, 40, 89) * 0.8f;
        BorderColor = new Color(13, 20, 44) * 0.8f;

        Height.Set(82f, 0f);
        Width.Set(0f, 1f);

        PaddingTop = 8f;
        PaddingLeft = 9f;

        Borders ??= Main.Assets.Request<Texture2D>("Images/UI/Achievement_Borders");
        InnerTop ??= Main.Assets.Request<Texture2D>("Images/UI/Achievement_InnerPanelTop");
        InnerBottom ??= Main.Assets.Request<Texture2D>("Images/UI/Achievement_InnerPanelBottom");
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);

        int target = Math.Max(def.Target, 1);
        int progress = Math.Clamp(AchievementStorage.Data.Get(id), 0, target);
        bool completed = progress >= target;

        CalculatedStyle inner = GetInnerDimensions();

        Vector2 iconPos = new(inner.X, inner.Y);
        Rectangle iconRect = new((int)iconPos.X, (int)iconPos.Y, IconSize, IconSize);

        Color iconTint = completed ? Color.White : new Color(180, 180, 180);
        if (!completed && IsMouseHovering) iconTint = Color.Lerp(iconTint, Color.White, 0.25f);

        int idx = 87-10 + def.IconIndex;
        //int idx = 0 + def.IconIndex;
        Rectangle frame = new(idx % 8 * 66, idx / 8 * 66, 64, 64);
        if (!completed) frame.X += 8 * 66;

        spriteBatch.Draw(Ass.Achievements.Value, iconRect, frame, iconTint);
        spriteBatch.Draw(Borders!.Value, iconPos + new Vector2(-4f, -4f), iconTint);

        float panelWidth = inner.Width - (IconSize + 8f) + 1f;
        Vector2 textOrigin = iconPos + new Vector2(IconSize + 11f, 0f);

        Color panelTint = IsMouseHovering ? Color.White : Color.Gray;

        Vector2 topPos = textOrigin - Vector2.UnitY * 2f;
        DrawInner(spriteBatch, InnerTop!.Value, topPos, panelWidth, panelTint, 2, 2, 2);

        Color titleColor = completed ? Color.Gold : Color.Silver;
        titleColor = Color.Lerp(titleColor, Color.White, IsMouseHovering ? 0.5f : 0f);

        Vector2 titlePos = topPos + new Vector2(8f, 2f);
        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.ItemStack.Value, def.Title, titlePos, titleColor, 0f, Vector2.Zero, new Vector2(0.92f), panelWidth);

        if (!completed && progress > 0)
        {
            Vector2 rightEdge = topPos + Vector2.UnitX * panelWidth + Vector2.UnitY;

            string text2 = progress + "/" + target;
            Vector2 baseScale3 = new(0.75f);
            Vector2 stringSize2 = ChatManager.GetStringSize(FontAssets.ItemStack.Value, text2, baseScale3);

            float prog = (float)progress / target;
            float num5 = 80f;

            Color fill = new Color(100, 255, 100);
            if (!IsMouseHovering) fill = Color.Lerp(fill, Color.Black, 0.25f);

            Color back = new Color(255, 255, 255);
            if (!IsMouseHovering) back = Color.Lerp(back, Color.Black, 0.25f);

            Vector2 baseSpot = rightEdge - Vector2.UnitX * num5 * 0.7f;
            Vector2 barSpot = baseSpot;
            barSpot.X += 12f;

            DrawProgressBar(spriteBatch, prog, barSpot, num5, back, fill, fill.MultiplyRGBA(new Color(new Vector4(1f, 1f, 1f, 0.5f))));

            Vector2 textPos = new(barSpot.X - stringSize2.X * 0.5f, barSpot.Y + 14f);
            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.ItemStack.Value, text2, textPos, titleColor, 0f, Vector2.Zero, baseScale3, 90f);
        }

        Vector2 bottomPos = textOrigin + Vector2.UnitY * 24f;
        DrawInner(spriteBatch, InnerBottom!.Value, bottomPos, panelWidth, panelTint, 6, 7, 6);

        Color descColor = completed ? Color.Silver : Color.DarkGray;
        descColor = Color.Lerp(descColor, Color.White, IsMouseHovering ? 1f : 0f);

        float descScale = 0.8f;
        string wrapped = FontAssets.ItemStack.Value.CreateWrappedText(def.Description, (panelWidth - 20f) / descScale, Language.ActiveCulture.CultureInfo);
        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.ItemStack.Value, wrapped, bottomPos + new Vector2(8f, 3.5f), descColor, 0f, Vector2.Zero, new Vector2(descScale));
    }

    private static void DrawInner(SpriteBatch spriteBatch, Texture2D tex, Vector2 pos, float width, Color color, int leftW, int midW, int rightW)
    {
        int h = tex.Height;
        spriteBatch.Draw(tex, pos, new Rectangle(0, 0, leftW, h), color);

        float midScale = Math.Max(0f, (width - leftW - rightW) / midW);
        spriteBatch.Draw(tex, pos + new Vector2(leftW, 0f), new Rectangle(leftW, 0, midW, h), color, 0f, Vector2.Zero, new Vector2(midScale, 1f), SpriteEffects.None, 0f);

        spriteBatch.Draw(tex, pos + new Vector2(width - rightW, 0f), new Rectangle(leftW + midW, 0, rightW, h), color);
    }

    private void DrawProgressBar(SpriteBatch spriteBatch, float progress, Vector2 spot, float Width = 169f, Color BackColor = default, Color FillingColor = default, Color BlipColor = default)
    {
        if (BlipColor == Color.Transparent) BlipColor = new Color(255, 165, 0, 127);
        if (FillingColor == Color.Transparent) FillingColor = new Color(255, 241, 51);
        if (BackColor == Color.Transparent) FillingColor = new Color(255, 255, 255);

        Texture2D value = TextureAssets.ColorBar.Value;
        _ = TextureAssets.ColorBlip.Value;
        Texture2D value2 = TextureAssets.MagicPixel.Value;

        float num = MathHelper.Clamp(progress, 0f, 1f);
        float num2 = Width * 1f;
        float num3 = 8f;
        float num4 = num2 / 169f;

        Vector2 position = spot + Vector2.UnitY * num3 + Vector2.UnitX * 1f;

        spriteBatch.Draw(value, spot, new Rectangle(5, 0, value.Width - 9, value.Height), BackColor, 0f, new Vector2(84.5f, 0f), new Vector2(num4, 1f), SpriteEffects.None, 0f);
        spriteBatch.Draw(value, spot + new Vector2((0f - num4) * 84.5f - 5f, 0f), new Rectangle(0, 0, 5, value.Height), BackColor, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f);
        spriteBatch.Draw(value, spot + new Vector2(num4 * 84.5f, 0f), new Rectangle(value.Width - 4, 0, 4, value.Height), BackColor, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f);

        position += Vector2.UnitX * (num - 0.5f) * num2;
        position.X -= 1f;

        spriteBatch.Draw(value2, position, new Rectangle(0, 0, 1, 1), FillingColor, 0f, new Vector2(1f, 0.5f), new Vector2(num2 * num, num3), SpriteEffects.None, 0f);
        if (progress != 0f) spriteBatch.Draw(value2, position, new Rectangle(0, 0, 1, 1), BlipColor, 0f, new Vector2(1f, 0.5f), new Vector2(2f, num3), SpriteEffects.None, 0f);
        spriteBatch.Draw(value2, position, new Rectangle(0, 0, 1, 1), Color.Black, 0f, new Vector2(0f, 0.5f), new Vector2(num2 * (1f - num), num3), SpriteEffects.None, 0f);
    }

    public override void MouseOver(UIMouseEvent evt)
    {
        base.MouseOver(evt);
        BackgroundColor = new Color(46, 60, 119);
        BorderColor = new Color(20, 30, 56);
    }

    public override void MouseOut(UIMouseEvent evt)
    {
        base.MouseOut(evt);
        BackgroundColor = new Color(26, 40, 89) * 0.8f;
        BorderColor = new Color(13, 20, 44) * 0.8f;
    }
}
