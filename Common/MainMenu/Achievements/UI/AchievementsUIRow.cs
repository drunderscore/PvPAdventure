using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Utilities;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;
using Terraria.UI.Chat;

namespace PvPAdventure.Common.MainMenu.Achievements.UI;

internal sealed class AchievementsUIRow : UIPanel
{
    private const int IconSize = 64;
    private const int CollectWidth = 150;
    private const int CollectPaddingLeft = 8;

    private static Asset<Texture2D>? Borders;
    private static Asset<Texture2D>? InnerTop;
    private static Asset<Texture2D>? InnerBottom;

    private readonly string id;
    private readonly AchievementDefinition def;

    public AchievementsUIRow(string id, AchievementDefinition def)
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

    protected override void DrawSelf(SpriteBatch sb)
    {
        // Layout
        CalculatedStyle inner = GetInnerDimensions();
        float panelWidth = inner.Width - (IconSize + 8f) - CollectWidth;
        float collectW = CollectWidth - CollectPaddingLeft;

        Vector2 iconPos = new(inner.X, inner.Y);
        Vector2 textOrigin = iconPos + new Vector2(IconSize + 11f, 0f);
        Vector2 topPos = textOrigin - Vector2.UnitY * 2f;
        Vector2 bottomPos = textOrigin + Vector2.UnitY * 24f;
        Vector2 collectBottomPos = bottomPos + new Vector2(panelWidth + CollectPaddingLeft, 0f);

        // Hover state
        //Rectangle collectRect = new((int)collectBottomPos.X, (int)collectBottomPos.Y, (int)collectW, InnerBottom!.Value.Height);
        //bool collectHover = collectRect.Contains(Main.MouseScreen.ToPoint());
        bool rowHover = IsMouseHovering;

        ApplyRowColors(rowHover);
        base.DrawSelf(sb);

        GetProgress(out int target, out int progress, out bool completed);

        // Draw achievement icon
        DrawAchievementIcon(sb, iconPos, completed, rowHover);

        // Draw title bar
        Color panelTint = rowHover ? Color.White : Color.Gray;
        DrawInner(sb, InnerTop!.Value, topPos, panelWidth + CollectWidth, panelTint, 2, 2, 2);

        Color titleColor = completed ? Color.Gold : Color.Silver;
        Vector2 titlePos = topPos + new Vector2(8f, 2f);
        ChatManager.DrawColorCodedStringWithShadow(sb, FontAssets.ItemStack.Value, def.Title, titlePos, titleColor, 0f, Vector2.Zero, new Vector2(0.92f), panelWidth);

        DrawRewardText(sb, topPos, panelWidth, completed, rowHover);

        // Draw progress info
        if (!completed && progress > 0)
            DrawProgressInfo(sb, topPos, titlePos, panelWidth, rowHover, progress, target, titleColor);

        // Draw bottom panels
        DrawInner(sb, InnerBottom!.Value, bottomPos, panelWidth, panelTint, 6, 7, 6);
        DrawCollectPanel(sb, collectBottomPos, collectW, completed);

        // Draw description
        DrawDescription(sb, bottomPos, panelWidth, completed, rowHover);
    }

    private void ApplyRowColors(bool rowHover)
    {
        BackgroundColor = rowHover ? new Color(46, 60, 119) : new Color(26, 40, 89) * 0.8f;
        BorderColor = rowHover ? new Color(20, 30, 56) : new Color(13, 20, 44) * 0.8f;
    }

    private void GetProgress(out int target, out int progress, out bool completed)
    {
        target = Math.Max(def.Target, 1);
        progress = Math.Clamp(AchievementStorage.Data.Get(id), 0, target);
        completed = progress >= target;
    }

    private void DrawAchievementIcon(SpriteBatch sb, Vector2 iconPos, bool completed, bool rowHover)
    {
        Rectangle iconRect = new((int)iconPos.X, (int)iconPos.Y, IconSize, IconSize);

        Color iconTint = completed ? Color.White : new Color(180, 180, 180);
        if (!completed && rowHover) iconTint = Color.Lerp(iconTint, Color.White, 0.25f);

        int idx = def.IconIndex + 8;
        Rectangle frame = new(idx % 8 * 66, idx / 8 * 66, 64, 64);
        if (!completed) frame.X += 8 * 66;

        sb.Draw(Ass.Achievements.Value, iconRect, frame, iconTint);
        sb.Draw(Borders!.Value, iconPos + new Vector2(-4f, -4f), iconTint);
    }

    private void DrawRewardText(SpriteBatch sb, Vector2 topPos, float panelWidth, bool completed, bool rowHover)
    {
        string rewardText = $"Reward: {def.GemsReward} Gems";
        Vector2 rewardScale = new(1f);
        Vector2 rewardSize = ChatManager.GetStringSize(FontAssets.ItemStack.Value, rewardText, rewardScale);

        float padR = 8f;
        float right = topPos.X + panelWidth + CollectWidth - padR;
        Color rewardColor = completed ? Color.Gold : Color.DarkGray;
        if (rowHover) rewardColor = Color.Lerp(rewardColor, Color.White, 0.4f);

        ChatManager.DrawColorCodedStringWithShadow(sb, FontAssets.ItemStack.Value, rewardText, new Vector2(right, topPos.Y + 3f), rewardColor, 0f, new Vector2(rewardSize.X, 0f), rewardScale, rewardSize.X);
    }

    private void DrawProgressInfo(SpriteBatch sb, Vector2 topPos, Vector2 titlePos, float panelWidth, bool rowHover, int progress, int target, Color titleColor)
    {
        string brand = "PvPAdventure";
        Vector2 brandScale = new(0.75f);
        Vector2 brandSize = ChatManager.GetStringSize(FontAssets.ItemStack.Value, brand, brandScale);
        Color brandColor = Color.Lerp(Color.Gold, Color.White, rowHover ? 1f : 0f);

        string progText = $"{progress}/{target}";
        Vector2 progScale = new(0.75f);
        Vector2 progSize = ChatManager.GetStringSize(FontAssets.ItemStack.Value, progText, progScale);

        float barWidth = 80f, barGap = 10f, rightPad = 8f;
        float brandX = topPos.X + panelWidth - rightPad - brandSize.X;
        float barX = brandX - barGap - barWidth;
        float progX = barX - barGap - progSize.X;

        ChatManager.DrawColorCodedStringWithShadow(sb, FontAssets.ItemStack.Value, def.Title, titlePos, titleColor, 0f, Vector2.Zero, new Vector2(0.92f), Math.Max(0f, progX - titlePos.X - 6f));

        float p = (float)progress / target;
        Color fill = new Color(100, 255, 100); if (!rowHover) fill = Color.Lerp(fill, Color.Black, 0.25f);
        Color back = new Color(255, 255, 255); if (!rowHover) back = Color.Lerp(back, Color.Black, 0.25f);

        DrawProgressBar(sb, p, new Vector2(barX + barWidth * 0.5f, topPos.Y + 2f), barWidth, back, fill, fill.MultiplyRGBA(new Color(new Vector4(1f, 1f, 1f, 0.5f))));
        ChatManager.DrawColorCodedStringWithShadow(sb, FontAssets.ItemStack.Value, progText, new Vector2(progX, topPos.Y + 3f), titleColor, 0f, Vector2.Zero, progScale, progSize.X);
        ChatManager.DrawColorCodedStringWithShadow(sb, FontAssets.ItemStack.Value, brand, new Vector2(brandX, topPos.Y + 3f), brandColor, 0f, Vector2.Zero, brandScale, brandSize.X);
    }

    private void DrawDescription(SpriteBatch sb, Vector2 bottomPos, float panelWidth, bool completed, bool rowHover)
    {
        Color descColor = completed ? Color.Silver : Color.DarkGray;
        descColor = Color.Lerp(descColor, Color.White, rowHover ? 1f : 0f);

        float descScale = 0.8f;
        string wrapped = FontAssets.ItemStack.Value.CreateWrappedText(def.Description, (panelWidth - 20f) / descScale, Language.ActiveCulture.CultureInfo);
        ChatManager.DrawColorCodedStringWithShadow(sb, FontAssets.ItemStack.Value, wrapped, bottomPos + new Vector2(8f, 3.5f), descColor, 0f, Vector2.Zero, new Vector2(descScale));
    }

    private static void DrawInner(SpriteBatch sb, Texture2D tex, Vector2 pos, float width, Color color, int leftW, int midW, int rightW)
    {
        int h = tex.Height; 

        // Left edge
        sb.Draw(tex, pos, new Rectangle(0, 0, leftW, h), color);
        float midScale = Math.Max(0f, (width - leftW - rightW) / midW);

        // Middle
        sb.Draw(tex, pos + new Vector2(leftW, 0f), new Rectangle(leftW, 0, midW, h), color, 0f, Vector2.Zero, new Vector2(midScale, 1f), SpriteEffects.None, 0f);

        // Right edge
        sb.Draw(tex, pos + new Vector2(width - rightW, 0f), new Rectangle(leftW + midW, 0, rightW, h), color);
    }

    private void DrawCollectPanel(SpriteBatch sb, Vector2 pos, float width, bool completed)
    {
        Texture2D tex = InnerBottom!.Value;
        Texture2D gemTex = Ass.Icon_Gem.Value;
        Rectangle rect = new((int)pos.X, (int)pos.Y, (int)width, tex.Height);
        bool hover = rect.Contains(Main.MouseScreen.ToPoint());
        bool collected = AchievementStorage.Data.IsCollected(id);
        bool canCollect = completed && !collected;

        Color panelColor = Color.Gray;
        if (!completed && hover) 
            panelColor = Color.White;

        if (!completed)
            panelColor = new Color(180, 180, 80);

        DrawInner(sb, tex, pos, width, panelColor, 6, 7, 6);

        Vector2 gemPos = new(pos.X + 8, pos.Y + 6f);
        sb.Draw(gemTex, gemPos, null, Color.White, 0f, Vector2.Zero, 1.25f, SpriteEffects.None, 0f);

        Color text = !completed ? Color.DarkGray : (canCollect ? Color.Gold : Color.Silver);
        if (hover && completed) text = Color.Lerp(text, Color.White, 0.5f);

        string label = collected ? "Collected" : "Collect";
        Vector2 textPos = new(gemPos.X + gemTex.Width + 18f, pos.Y + 8);
        ChatManager.DrawColorCodedStringWithShadow(sb, FontAssets.ItemStack.Value, label, textPos, text, 0f, Vector2.Zero, Vector2.One, 0);

        // Handle input
        if (canCollect && rect.Contains(Main.MouseScreen.ToPoint()) && Main.mouseLeft && Main.mouseLeftRelease)
            AchievementStorage.TryCollect(id);
    }

    private void DrawProgressBar(SpriteBatch spriteBatch, float progress, Vector2 spot, float Width = 169f, Color BackColor = default, Color FillingColor = default, Color BlipColor = default)
    {
        if (BlipColor == Color.Transparent) BlipColor = new Color(255, 165, 0, 127);
        if (FillingColor == Color.Transparent) FillingColor = new Color(255, 241, 51);
        if (BackColor == Color.Transparent) FillingColor = new Color(255, 255, 255);

        Texture2D value = TextureAssets.ColorBar.Value;
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
}