using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.MainMenu.Profile;
using PvPAdventure.Core.Utilities;
using ReLogic.Content;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader.UI;
using Terraria.UI;
using Terraria.UI.Chat;

namespace PvPAdventure.Common.MainMenu.Shop.UI;

internal sealed class SkinUICard : UIElement
{
    private static Asset<Texture2D>? Back;
    private static Asset<Texture2D>? Border;
    private static Asset<Texture2D>? Highlight;

    private readonly ProductDefinition _def;
    private readonly UISlicedImage _back;
    private readonly UISlicedImage _highlight;
    private readonly UISlicedImage _border;
    private static Asset<Texture2D>? EquippedFill;

    public SkinUICard(ProductDefinition def, float cardW)
    {
        _def = def;

        Back ??= Main.Assets.Request<Texture2D>("Images/UI/CharCreation/SmallPanel");
        Border ??= Main.Assets.Request<Texture2D>("Images/UI/CharCreation/SmallPanelBorder");
        Highlight ??= Main.Assets.Request<Texture2D>("Images/UI/CharCreation/CategoryPanelHighlight");

        EquippedFill ??= Main.Assets.Request<Texture2D>("Images/MagicPixel");

        Width = StyleDimension.FromPixels(cardW);

        _back = new UISlicedImage(Back)
        {
            Width = StyleDimension.Fill,
            Height = StyleDimension.Fill,
            IgnoresMouseInteraction = true
        };
        //_highlight = new UISlicedImage(Highlight)
        //{
        //    Width = StyleDimension.Fill,
        //    Height = StyleDimension.Fill,
        //    IgnoresMouseInteraction = true
        //};
        _border = new UISlicedImage(Border)
        {
            Width = StyleDimension.Fill,
            Height = StyleDimension.Fill,
            IgnoresMouseInteraction = true
        };

        _back.SetSliceDepths(10);
        _border.SetSliceDepths(10);


        //float inset = 2f;
        //_highlight.SetSliceDepths(10);
        //_highlight.Left = StyleDimension.FromPixels(inset);
        //_highlight.Top = StyleDimension.FromPixels(inset);
        //_highlight.Width = StyleDimension.FromPixelsAndPercent(-inset * 2f, 1f);
        //_highlight.Height = StyleDimension.FromPixelsAndPercent(-inset * 2f, 1f);
        //Append(_highlight);

        Append(_back);
        Append(_border);
    }

    public override void MouseOver(UIMouseEvent evt)
    {
        base.MouseOver(evt);
        SoundEngine.PlaySound(SoundID.MenuTick);
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);

        _ = HandleClickAsync();
    }

    private async Task HandleClickAsync()
    {
        MainMenuProfileState state = MainMenuProfileState.Instance;

        bool owned = state.HasSkin(_def);
        bool equipped = state.IsEquipped(_def);
        bool canAfford = state.CanAfford(_def);

        if (!owned)
        {
            if (!canAfford)
            {
                SoundEngine.PlaySound(SoundID.MenuClose);
                return;
            }

            SoundEngine.PlaySound(SoundID.Coins);

            PurchaseResult purchaseResult = await ShopApi.PurchaseProductAsync(_def.Identity.Prototype, _def.Identity.Name);
            if (purchaseResult == PurchaseResult.Failed)
            {
                Log.Error($"Backend rejected purchase of {_def.Name}.");
                await ShopApi.RefreshProfileStateAsync();
                return;
            }

            bool equippedResult = await ShopApi.UpdateEquipmentAsync(_def.Identity.Prototype, _def.Identity.Name);
            if (!equippedResult)
                Log.Error($"Failed to equip {_def.Name} after purchase.");

            await ShopApi.RefreshProfileStateAsync();
            return;
        }

        SoundEngine.PlaySound(SoundID.Unlock);

        bool success = equipped
            ? await ShopApi.UpdateEquipmentAsync(_def.Identity.Prototype, null)
            : await ShopApi.UpdateEquipmentAsync(_def.Identity.Prototype, _def.Identity.Name);

        if (!success)
            Log.Error($"Failed to update equip state for {_def.Name}.");

        await ShopApi.RefreshProfileStateAsync();
    }

    public override void Draw(SpriteBatch sb)
    {
        MainMenuProfileState state = MainMenuProfileState.Instance;

        bool owned = state.HasSkin(_def);
        bool equipped = state.IsEquipped(_def);
        bool canAfford = state.CanAfford(_def);
        bool hover = IsMouseHovering;

        _back.Color = equipped
            ? new Color(80, 255, 80) * (hover ? 1f : 0.95f)
            : owned
                ? new Color(60, 120, 60) * (hover ? 0.75f : 0.6f)
                : new Color(63, 82, 151) * (hover ? 0.85f : 0.7f);

        _border.Color = hover || equipped ? Color.White : Color.Transparent;
        //_highlight.Color = equipped ? Color.White * 0.3f : Color.Transparent;

        base.Draw(sb);

        if (equipped)
        {
            CalculatedStyle dims = GetDimensions();
            Rectangle fillRect = dims.ToRectangle();
            fillRect.Inflate(-4, -4);

            sb.Draw(EquippedFill!.Value, fillRect, new Color(60, 255, 60) * (hover ? 0.18f : 0.12f));
        }

        CalculatedStyle d = GetDimensions();
        float contentAlpha = owned ? 0.65f : 1f;

        string name = _def.Name;
        float t = name.Length > 18 ? MathHelper.Clamp((name.Length - 18) / 12f, 0f, 1f) : 0f;
        float titleScale = MathHelper.Lerp(0.75f, 0.55f, t);

        Vector2 nameSize = ChatManager.GetStringSize(FontAssets.MouseText.Value, name, Vector2.One * titleScale);
        Vector2 namePos = new(d.X + d.Width * 0.5f - nameSize.X * 0.5f, d.Y + 8f);
        ChatManager.DrawColorCodedStringWithShadow(sb, FontAssets.MouseText.Value, name, namePos, Color.White, 0f, Vector2.Zero, Vector2.One * titleScale, d.Width - 6f);

        Texture2D tex = _def.Texture?.Value ?? TextureAssets.Item[_def.ItemType].Value;
        float maxIcon = 48f;
        float iconScale = maxIcon / System.Math.Max(tex.Width, tex.Height);
        Vector2 iconCenter = new(d.X + d.Width * 0.5f, d.Y + 52f);

        sb.Draw(tex, iconCenter, null, Color.White, 0f, tex.Size() * 0.5f, iconScale, SpriteEffects.None, 0f);

        string priceText = _def.Price.ToString();
        float priceScale = 1.05f;
        Vector2 priceSize = ChatManager.GetStringSize(FontAssets.MouseText.Value, priceText, Vector2.One * priceScale);

        float py = d.Y + d.Height - priceSize.Y - 6f;
        float px = d.X + d.Width * 0.5f - priceSize.X * 0.5f + 8f;

        Color priceColor = (owned || canAfford)
            ? Color.White * contentAlpha
            : new Color(148, 39, 39) * contentAlpha;

        Texture2D badgeTex = equipped
        ? Ass.Icon_CheckmarkGreenBox.Value
        : owned
            ? Ass.Icon_CheckmarkGray.Value
            : Ass.Icon_Gem.Value;

            float badgeScale = equipped
                ? 1.1f
                : owned
                    ? 1.1f
                    : 0.9f;
        Vector2 badgePos = new Vector2(px - 14f, py + 12f) + (owned ? new Vector2(0f, -1f) : Vector2.Zero);

        sb.Draw(badgeTex, badgePos, null, Color.White, 0f, badgeTex.Size() * 0.5f, badgeScale, SpriteEffects.None, 0f);
        ChatManager.DrawColorCodedStringWithShadow(sb, FontAssets.MouseText.Value, priceText, new Vector2(px + 2f, py), priceColor, 0f, Vector2.Zero, Vector2.One * priceScale);

        if (hover)
            DrawTooltip(owned, equipped, canAfford);
    }

    private void DrawTooltip(bool owned, bool equipped, bool canAfford)
    {
        Main.LocalPlayer.mouseInterface = true;

        string original = Lang.GetItemNameValue(_def.ItemType);
        string display = $"{_def.Name} ({original})";
        Color badRed = new(148, 39, 39);

        if (equipped)
        {
            UICommon.TooltipMouseText(C(Color.LimeGreen, $"Unequip {display}"));
            return;
        }

        if (owned)
        {
            UICommon.TooltipMouseText(C(Color.LimeGreen, $"Equip {display}"));
            return;
        }

        if (canAfford)
        {
            UICommon.TooltipMouseText(C(Color.LimeGreen, $"Buy {display}") + "\n" + C(Color.White, $"Price: {_def.Price} gems"));
            return;
        }

        UICommon.TooltipMouseText(C(Color.LimeGreen, $"Buy {display}") + "\n" + C(badRed, "Not enough gems"));
    }

    private string C(Color c, string text)
    {
        return $"[c/{c.R:X2}{c.G:X2}{c.B:X2}:{text}]";
    }
}