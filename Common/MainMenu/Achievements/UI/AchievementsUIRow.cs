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

namespace PvPAdventure.Common.MainMenu.Achievements.UI;

internal sealed class AchievementsUIRow : UIPanel
{
    private const int IconSize = 64;
    private const int CollectWidth = 150;
    private const int CollectPaddingLeft = 8;

    private static Asset<Texture2D>? borders;
    private static Asset<Texture2D>? innerTop;
    private static Asset<Texture2D>? innerBottom;

    private readonly AchievementUIEntry content;

    public AchievementsUIRow(AchievementUIEntry content)
    {
        this.content = content;

        BackgroundColor = new Color(26, 40, 89) * 0.8f;
        BorderColor = new Color(13, 20, 44) * 0.8f;

        Height.Set(82f, 0f);
        Width.Set(0f, 1f);

        PaddingTop = 8f;
        PaddingLeft = 9f;

        borders ??= Main.Assets.Request<Texture2D>("Images/UI/Achievement_Borders");
        innerTop ??= Main.Assets.Request<Texture2D>("Images/UI/Achievement_InnerPanelTop");
        innerBottom ??= Main.Assets.Request<Texture2D>("Images/UI/Achievement_InnerPanelBottom");
    }

    protected override void DrawSelf(SpriteBatch sb)
    {
        CalculatedStyle inner = GetInnerDimensions();
        float panelWidth = inner.Width - (IconSize + 8f) - CollectWidth;
        float collectW = CollectWidth - CollectPaddingLeft;

        Vector2 iconPos = new(inner.X, inner.Y);
        Vector2 textOrigin = iconPos + new Vector2(IconSize + 11f, 0f);
        Vector2 topPos = textOrigin - Vector2.UnitY * 2f;
        Vector2 bottomPos = textOrigin + Vector2.UnitY * 24f;
        Vector2 collectBottomPos = bottomPos + new Vector2(panelWidth + CollectPaddingLeft, 0f);

        bool rowHover = IsMouseHovering;

        ApplyRowColors(rowHover);
        base.DrawSelf(sb);

        int target = content.ClampedTarget;
        int progress = content.ClampedProgress;
        bool completed = content.IsCompleted;
        bool collected = content.IsCollected;

        DrawAchievementIcon(sb, iconPos, completed, rowHover);

        Color panelTint = rowHover ? Color.White : Color.Gray;
        DrawInner(sb, innerTop!.Value, topPos, panelWidth + CollectWidth, panelTint, 2, 2, 2);

        Color titleColor = completed ? Color.Gold : Color.Silver;
        Vector2 titlePos = topPos + new Vector2(8f, 2f);
        ChatManager.DrawColorCodedStringWithShadow(sb, FontAssets.ItemStack.Value, content.Title, titlePos, titleColor, 0f, Vector2.Zero, new Vector2(0.92f), panelWidth);

        DrawRewardText(sb, topPos, panelWidth, completed, collected);

        if (!completed && progress > 0)
            DrawProgressInfo(sb, topPos, titlePos, panelWidth, rowHover, progress, target, titleColor);

        DrawInner(sb, innerBottom!.Value, bottomPos, panelWidth, panelTint, 6, 7, 6);
        DrawCollectPanel(sb, collectBottomPos, collectW, completed);
        DrawDescription(sb, bottomPos, panelWidth, completed, rowHover);
    }

    private void ApplyRowColors(bool rowHover)
    {
        if (rowHover)
        {
            BackgroundColor = new Color(46, 60, 119);
            BorderColor = new Color(20, 30, 56);
            return;
        }

        BackgroundColor = new Color(26, 40, 89) * 0.8f;
        BorderColor = new Color(13, 20, 44) * 0.8f;
    }

    private Rectangle GetCollectBounds()
    {
        CalculatedStyle inner = GetInnerDimensions();
        float panelWidth = inner.Width - (IconSize + 8f) - CollectWidth;
        float collectW = CollectWidth - CollectPaddingLeft;

        Vector2 textOrigin = new(inner.X + IconSize + 11f, inner.Y);
        Vector2 bottomPos = textOrigin + Vector2.UnitY * 24f;
        Vector2 collectPos = bottomPos + new Vector2(panelWidth + CollectPaddingLeft, 0f);

        int height = innerBottom?.Value.Height ?? 26;
        return new Rectangle((int)collectPos.X, (int)collectPos.Y, (int)collectW, height);
    }

    private void DrawAchievementIcon(SpriteBatch sb, Vector2 iconPos, bool completed, bool rowHover)
    {
        Rectangle iconRect = new((int)iconPos.X, (int)iconPos.Y, IconSize, IconSize);

        Color iconTint = completed ? Color.White : new Color(180, 180, 180);
        if (!completed && rowHover)
            iconTint = Color.Lerp(iconTint, Color.White, 0.25f);

        int idx = content.IconIndex + 8;
        Rectangle frame = new(idx % 8 * 66, idx / 8 * 66, 64, 64);
        if (!completed)
            frame.X += 8 * 66;

        sb.Draw(Ass.Achievements.Value, iconRect, frame, iconTint);
        sb.Draw(borders!.Value, iconPos + new Vector2(-4f, -4f), iconTint);
    }

    private void DrawRewardText(SpriteBatch sb, Vector2 topPos, float panelWidth, bool completed, bool collected)
    {
        string rewardText = $"Reward: {content.GemsReward} Gems";
        Vector2 scale = Vector2.One;
        Vector2 size = ChatManager.GetStringSize(FontAssets.ItemStack.Value, rewardText, scale);

        float right = topPos.X + panelWidth + CollectWidth - 8f;
        Color rewardColor = completed && !collected ? Color.Gold : Color.DarkGray;

        ChatManager.DrawColorCodedStringWithShadow(sb, FontAssets.ItemStack.Value, rewardText, new Vector2(right, topPos.Y + 3f), rewardColor, 0f, new Vector2(size.X, 0f), scale, size.X);
    }

    private void DrawProgressInfo(SpriteBatch sb, Vector2 topPos, Vector2 titlePos, float panelWidth, bool rowHover, int progress, int target, Color titleColor)
    {
        string brand = "PvPAdventure";
        Vector2 brandScale = new(0.75f);
        Vector2 brandSize = ChatManager.GetStringSize(FontAssets.ItemStack.Value, brand, brandScale);
        Color brandColor = rowHover ? Color.White : Color.Gold;

        string progText = $"{progress}/{target}";
        Vector2 progScale = new(0.75f);
        Vector2 progSize = ChatManager.GetStringSize(FontAssets.ItemStack.Value, progText, progScale);

        float barWidth = 80f;
        float barGap = 10f;
        float rightPad = 8f;

        float brandX = topPos.X + panelWidth - rightPad - brandSize.X;
        float barX = brandX - barGap - barWidth;
        float progX = barX - barGap - progSize.X;

        ChatManager.DrawColorCodedStringWithShadow(sb, FontAssets.ItemStack.Value, content.Title, titlePos, titleColor, 0f, Vector2.Zero, new Vector2(0.92f), Math.Max(0f, progX - titlePos.X - 6f));

        float p = (float)progress / target;

        Color fill = new Color(100, 255, 100);
        Color back = Color.White;

        if (!rowHover)
        {
            fill = Color.Lerp(fill, Color.Black, 0.25f);
            back = Color.Lerp(back, Color.Black, 0.25f);
        }

        DrawProgressBar(sb, p, new Vector2(barX + barWidth * 0.5f, topPos.Y + 2f), barWidth, back, fill, fill.MultiplyRGBA(new Color(new Vector4(1f, 1f, 1f, 0.5f))));
        ChatManager.DrawColorCodedStringWithShadow(sb, FontAssets.ItemStack.Value, progText, new Vector2(progX, topPos.Y + 3f), titleColor, 0f, Vector2.Zero, progScale, progSize.X);
        ChatManager.DrawColorCodedStringWithShadow(sb, FontAssets.ItemStack.Value, brand, new Vector2(brandX, topPos.Y + 3f), brandColor, 0f, Vector2.Zero, brandScale, brandSize.X);
    }

    private void DrawDescription(SpriteBatch sb, Vector2 bottomPos, float panelWidth, bool completed, bool rowHover)
    {
        Color descColor = completed ? Color.Silver : Color.DarkGray;
        if (rowHover)
            descColor = Color.White;

        float descScale = 0.8f;
        string wrapped = FontAssets.ItemStack.Value.CreateWrappedText(content.Description, (panelWidth - 20f) / descScale, Language.ActiveCulture.CultureInfo);
        ChatManager.DrawColorCodedStringWithShadow(sb, FontAssets.ItemStack.Value, wrapped, bottomPos + new Vector2(8f, 3.5f), descColor, 0f, Vector2.Zero, new Vector2(descScale));
    }

    private static void DrawInner(SpriteBatch sb, Texture2D tex, Vector2 pos, float width, Color color, int leftW, int midW, int rightW)
    {
        int h = tex.Height;

        sb.Draw(tex, pos, new Rectangle(0, 0, leftW, h), color);

        float midScale = Math.Max(0f, (width - leftW - rightW) / midW);
        sb.Draw(tex, pos + new Vector2(leftW, 0f), new Rectangle(leftW, 0, midW, h), color, 0f, Vector2.Zero, new Vector2(midScale, 1f), SpriteEffects.None, 0f);

        sb.Draw(tex, pos + new Vector2(width - rightW, 0f), new Rectangle(leftW + midW, 0, rightW, h), color);
    }

    private void DrawCollectPanel(SpriteBatch sb, Vector2 pos, float width, bool completed)
    {
        Texture2D tex = innerBottom!.Value;
        Texture2D gemTex = Ass.Icon_Gem.Value;
        Texture2D checkmarkTex = Ass.Icon_CheckmarkGreen.Value;

        bool collected = content.IsCollected;
        bool canCollect = completed && !collected;
        bool hover = GetCollectBounds().Contains(Main.MouseScreen.ToPoint());

        Color panelColor = completed ? Color.Gray : new Color(180, 120, 80);
        if (canCollect && hover)
            panelColor = Color.White;

        DrawInner(sb, tex, pos, width, panelColor, 6, 7, 6);

        Vector2 iconPos = new(pos.X + 8f, pos.Y + 6f);
        if (collected)
            sb.Draw(checkmarkTex, iconPos + new Vector2(-1f, -3f), null, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0f);
        else
            sb.Draw(gemTex, iconPos, null, Color.White, 0f, Vector2.Zero, 1.25f, SpriteEffects.None, 0f);

        Color textColor = canCollect ? Color.Gold : Color.DarkGray;
        string label = collected ? "Claimed" : "Claim";
        Vector2 textPos = new(iconPos.X + gemTex.Width + 18f, pos.Y + 9f);
        ChatManager.DrawColorCodedStringWithShadow(sb, FontAssets.ItemStack.Value, label, textPos, textColor, 0f, Vector2.Zero, Vector2.One, 0f);
    }

    private void DrawProgressBar(SpriteBatch spriteBatch, float progress, Vector2 spot, float width = 169f, Color backColor = default, Color fillingColor = default, Color blipColor = default)
    {
        if (blipColor == Color.Transparent)
            blipColor = new Color(255, 165, 0, 127);

        if (fillingColor == Color.Transparent)
            fillingColor = new Color(255, 241, 51);

        if (backColor == Color.Transparent)
            backColor = Color.White;

        Texture2D bar = TextureAssets.ColorBar.Value;
        Texture2D pixel = TextureAssets.MagicPixel.Value;

        float p = MathHelper.Clamp(progress, 0f, 1f);
        float barH = 8f;
        float scaleX = width / 169f;

        Vector2 position = spot + Vector2.UnitY * barH + Vector2.UnitX;

        spriteBatch.Draw(bar, spot, new Rectangle(5, 0, bar.Width - 9, bar.Height), backColor, 0f, new Vector2(84.5f, 0f), new Vector2(scaleX, 1f), SpriteEffects.None, 0f);
        spriteBatch.Draw(bar, spot + new Vector2(-scaleX * 84.5f - 5f, 0f), new Rectangle(0, 0, 5, bar.Height), backColor, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f);
        spriteBatch.Draw(bar, spot + new Vector2(scaleX * 84.5f, 0f), new Rectangle(bar.Width - 4, 0, 4, bar.Height), backColor, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f);

        position += Vector2.UnitX * (p - 0.5f) * width;
        position.X -= 1f;

        spriteBatch.Draw(pixel, position, new Rectangle(0, 0, 1, 1), fillingColor, 0f, new Vector2(1f, 0.5f), new Vector2(width * p, barH), SpriteEffects.None, 0f);

        if (p > 0f)
            spriteBatch.Draw(pixel, position, new Rectangle(0, 0, 1, 1), blipColor, 0f, new Vector2(1f, 0.5f), new Vector2(2f, barH), SpriteEffects.None, 0f);

        spriteBatch.Draw(pixel, position, new Rectangle(0, 0, 1, 1), Color.Black, 0f, new Vector2(0f, 0.5f), new Vector2(width * (1f - p), barH), SpriteEffects.None, 0f);
    }
}
