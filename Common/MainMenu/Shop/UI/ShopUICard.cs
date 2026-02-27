using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.MainMenu.Gems;
using PvPAdventure.Common.MainMenu.Shop;
using PvPAdventure.Core.Utilities;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;
using Terraria.UI.Chat;

internal sealed class ShopUICard : UIPanel
{
    private static Asset<Texture2D>? Borders;

    private readonly ShopItemDefinition _def;
    private UITextPanel<string> buyButton = null!;

    public ShopUICard(ShopItemDefinition def)
    {
        _def = def;

        BackgroundColor = new Color(26, 40, 89) * 0.8f;
        BorderColor = new Color(13, 20, 44) * 0.8f;

        SetPadding(10f);

        Borders ??= Main.Assets.Request<Texture2D>("Images/UI/Achievement_Borders");

        buyButton = new UITextPanel<string>("Buy", 0.55f, true)
        {
            HAlign = 1f,
            VAlign = 1f,
            Width = new StyleDimension(110f, 0f),
            Height = new StyleDimension(40f, 0f),
            BackgroundColor = new Color(63, 82, 151) * 0.8f,
            BorderColor = Color.Black
        };
        buyButton.SetPadding(0f);
        buyButton.PaddingTop = 8f;
        buyButton.PaddingBottom = 0f;

        buyButton.OnLeftClick += (_, _) =>
        {
            if (UnlockedStorage.IsUnlocked(_def.Id))
                return;

            if (!GemStorage.TrySpend(_def.CostGems))
            {
                SoundEngine.PlaySound(SoundID.MenuClose);
                return;
            }

            UnlockedStorage.TryUnlock(_def.Id);
            SoundEngine.PlaySound(SoundID.Unlock);
            UpdateBuyButton();
        };

        buyButton.OnMouseOver += (evt, _) =>
        {
            if (UnlockedStorage.IsUnlocked(_def.Id))
                return;

            SoundEngine.PlaySound(12);
            if (evt.Target is UIPanel p)
            {
                p.BackgroundColor = new Color(73, 94, 171);
                p.BorderColor = Colors.FancyUIFatButtonMouseOver;
            }
        };

        buyButton.OnMouseOut += (evt, _) =>
        {
            if (UnlockedStorage.IsUnlocked(_def.Id))
            {
                UpdateBuyButton();
                return;
            }

            if (evt.Target is UIPanel p)
            {
                p.BackgroundColor = new Color(63, 82, 151) * 0.8f;
                p.BorderColor = Color.Black;
            }
        };

        Append(buyButton);
        UpdateBuyButton();
    }

    private void UpdateBuyButton()
    {
        if (UnlockedStorage.IsUnlocked(_def.Id))
        {
            buyButton.SetText("Owned");
            buyButton.BackgroundColor = new Color(40, 40, 40) * 0.9f;
            buyButton.BorderColor = new Color(20, 20, 20) * 0.9f;
            return;
        }

        buyButton.SetText("Buy");
        buyButton.BackgroundColor = new Color(63, 82, 151) * 0.8f;
        buyButton.BorderColor = Color.Black;
    }

    protected override void DrawSelf(SpriteBatch sb)
    {
        base.DrawSelf(sb);

        CalculatedStyle inner = GetInnerDimensions();
        Vector2 pos = inner.Position();

        float hover = IsMouseHovering ? 1f : 0f;

        Color titleColor = Color.Lerp(Color.LightGray, Color.White, hover);
        ChatManager.DrawColorCodedStringWithShadow(
            sb,
            FontAssets.ItemStack.Value,
            _def.Title,
            pos + new Vector2(2f, -2f),
            titleColor,
            0f,
            Vector2.Zero,
            new Vector2(1.05f),
            inner.Width - 6f
        );

        float cardW = inner.Width;

        float iconBoxSize = 62f;
        float iconSize = 56f;

        Vector2 iconBoxPos = pos + new Vector2(cardW * 0.5f - iconBoxSize * 0.5f, 30f);
        Rectangle iconBox = new((int)iconBoxPos.X, (int)iconBoxPos.Y, (int)iconBoxSize, (int)iconBoxSize);

        sb.Draw(TextureAssets.MagicPixel.Value, iconBox, new Color(14, 20, 44) * 0.65f);

        Vector2 iconPos = iconBoxPos + new Vector2(iconBoxSize * 0.5f, iconBoxSize * 0.5f);
        Texture2D iconTex = _def.Icon.Value;
        Vector2 iconOrigin = new(iconTex.Width * 0.5f, iconTex.Height * 0.5f);

        float iconScale = iconSize / Math.Max(iconTex.Width, iconTex.Height);

        Color iconTint = Color.Lerp(new Color(220, 220, 220), Color.White, hover);
        sb.Draw(iconTex, iconPos, null, iconTint, 0f, iconOrigin, iconScale, SpriteEffects.None, 0f);

        sb.Draw(Borders!.Value, iconBoxPos + new Vector2(-4f, -4f), iconTint);

        float descScale = 0.82f;
        float descMaxW = inner.Width - 16f;
        string wrapped = FontAssets.ItemStack.Value.CreateWrappedText(
            _def.Description,
            descMaxW / descScale,
            Language.ActiveCulture.CultureInfo
        );

        Color descColor = Color.Lerp(Color.LightGray, Color.White, hover);

        ChatManager.DrawColorCodedStringWithShadow(
            sb,
            FontAssets.ItemStack.Value,
            wrapped,
            pos + new Vector2(4f, 106f),
            descColor,
            0f,
            Vector2.Zero,
            new Vector2(descScale),
            descMaxW
        );

        string costText = _def.CostGems.ToString();
        Vector2 costScale = new(1.0f);
        float gemIconSize = 22f;

        Vector2 bottomLeft = pos + new Vector2(6f, inner.Height - 40f);
        sb.Draw(Ass.Icon_Gem.Value, bottomLeft, null, Color.White, 0f, Vector2.Zero, gemIconSize / Ass.Icon_Gem.Value.Width, SpriteEffects.None, 0f);

        ChatManager.DrawColorCodedStringWithShadow(
            sb,
            FontAssets.ItemStack.Value,
            costText,
            bottomLeft + new Vector2(gemIconSize + 8f, 2f),
            Color.White,
            0f,
            Vector2.Zero,
            costScale
        );
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