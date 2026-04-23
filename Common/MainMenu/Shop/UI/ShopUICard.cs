using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.MainMenu.API;
using PvPAdventure.Common.MainMenu.API.Profile;
using PvPAdventure.Common.MainMenu.API.Shop;
using PvPAdventure.Common.MainMenu.Profile;
using PvPAdventure.Core.Utilities;
using ReLogic.Content;
using System;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader.UI;
using Terraria.UI;
using Terraria.UI.Chat;

namespace PvPAdventure.Common.MainMenu.Shop.UI;

internal sealed class ShopUICard : UIElement
{
    private static Asset<Texture2D>? Back;
    private static Asset<Texture2D>? Border;
    private static Asset<Texture2D>? EquippedFill;

    private readonly ShopProduct def;
    private readonly UISlicedImage back;
    private readonly UISlicedImage border;

    private bool busy;

    public ShopUICard(ShopProduct def, float cardW)
    {
        this.def = def;

        Back ??= Main.Assets.Request<Texture2D>("Images/UI/CharCreation/SmallPanel");
        Border ??= Main.Assets.Request<Texture2D>("Images/UI/CharCreation/SmallPanelBorder");
        EquippedFill ??= Main.Assets.Request<Texture2D>("Images/MagicPixel");

        Width = StyleDimension.FromPixels(cardW);

        back = new UISlicedImage(Back)
        {
            Width = StyleDimension.Fill,
            Height = StyleDimension.Fill,
            IgnoresMouseInteraction = true
        };

        border = new UISlicedImage(Border)
        {
            Width = StyleDimension.Fill,
            Height = StyleDimension.Fill,
            IgnoresMouseInteraction = true
        };

        back.SetSliceDepths(10);
        border.SetSliceDepths(10);

        Append(back);
        Append(border);
    }

    public override void MouseOver(UIMouseEvent evt)
    {
        base.MouseOver(evt);
        SoundEngine.PlaySound(SoundID.MenuTick);
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);

        if (busy)
            return;

        _ = HandleClickSafeAsync();
    }

    private async Task HandleClickSafeAsync()
    {
        busy = true;

        try
        {
            await HandleClickAsync();
        }
        catch (Exception ex)
        {
            Log.Error($"[SkinUICard] Unexpected error while handling click for '{def.Prototype}:{def.Name}': {ex}");
        }
        finally
        {
            busy = false;
        }
    }

    private async Task HandleClickAsync()
    {
        MainMenuProfileState state = MainMenuProfileState.Instance;

        if (!state.HasSyncedFromBackend)
        {
            SoundEngine.PlaySound(SoundID.MenuClose);
            return;
        }

        bool owned = state.HasSkin(def);
        bool equipped = state.IsEquipped(def);
        bool canAfford = state.CanAfford(def);

        if (!owned)
        {
            if (!canAfford)
            {
                SoundEngine.PlaySound(SoundID.MenuClose);
                return;
            }

            SoundEngine.PlaySound(SoundID.Coins);

            ApiResult<PurchaseResult> purchaseResult = await ShopApi.PurchaseProductAsync(def.Prototype, def.Name).ConfigureAwait(false);
            if (!purchaseResult.IsSuccess)
            {
                Log.Error($"[SkinUICard] Purchase failed for '{def.Prototype}:{def.Name}'. Status={(int)purchaseResult.Status}, Error={purchaseResult.ErrorMessage}");
                await RefreshProfileStateSafeAsync().ConfigureAwait(false);
                return;
            }

            ApiResult<bool> equipResult = await ProfileApi.UpdateEquipmentAsync(def.Prototype, def.Name).ConfigureAwait(false);
            if (!equipResult.IsSuccess)
                Log.Error($"[SkinUICard] Failed to equip '{def.Prototype}:{def.Name}' after purchase. Status={(int)equipResult.Status}, Error={equipResult.ErrorMessage}");

            await RefreshProfileStateSafeAsync().ConfigureAwait(false);
            return;
        }

        SoundEngine.PlaySound(SoundID.Unlock);

        string nextName = equipped ? "" : def.Name;
        ApiResult<bool> toggleResult = await ProfileApi.UpdateEquipmentAsync(def.Prototype, nextName).ConfigureAwait(false);
        if (!toggleResult.IsSuccess)
            Log.Error($"[SkinUICard] Failed to update equip state for '{def.Prototype}:{def.Name}'. Status={(int)toggleResult.Status}, Error={toggleResult.ErrorMessage}");

        await RefreshProfileStateSafeAsync().ConfigureAwait(false);
    }

    private static async Task RefreshProfileStateSafeAsync()
    {
        ApiResult<bool> result = await ProfileApi.RefreshProfileStateAsync().ConfigureAwait(false);
        if (!result.IsSuccess)
            Log.Error($"[SkinUICard] Failed to refresh profile state. Status={(int)result.Status}, Error={result.ErrorMessage}");
    }

    public override void Draw(SpriteBatch sb)
    {
        bool hover = IsMouseHovering;
        MainMenuProfileState state = MainMenuProfileState.Instance;
        bool owned = state.HasSkin(def);
        bool equipped = state.IsEquipped(def);
        bool canAfford = state.CanAfford(def);

        back.Color = equipped
            ? new Color(80, 255, 80) * (hover ? 1f : 0.95f)
            : owned
                ? new Color(60, 120, 60) * (hover ? 0.75f : 0.6f)
                : new Color(63, 82, 151) * (hover ? 0.85f : 0.7f);

        border.Color = hover || equipped ? Color.White : Color.Transparent;

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

        Rectangle cardRect = new((int)d.X, (int)d.Y, (int)d.Width, (int)d.Height);
        Rectangle textRect = new(cardRect.X + 6, cardRect.Y + 7, cardRect.Width - 12, 30);

        string name = def.DisplayName;
        float titleScale = 0.7f;
        string[] titleLines = GetTitleLines(name, textRect.Width, titleScale);
        float lineHeight = FontAssets.MouseText.Value.MeasureString("A").Y * titleScale;
        float titleTop = textRect.Y + Math.Max(0f, (textRect.Height - lineHeight * titleLines.Length) * 0.5f);

        for (int i = 0; i < titleLines.Length; i++)
        {
            Vector2 lineSize = ChatManager.GetStringSize(FontAssets.MouseText.Value, titleLines[i], Vector2.One * titleScale);
            Vector2 linePos = new(textRect.X + textRect.Width * 0.5f - lineSize.X * 0.5f, titleTop + i * lineHeight);

            ChatManager.DrawColorCodedStringWithShadow(sb, FontAssets.MouseText.Value, titleLines[i], linePos, Color.White, 0f, Vector2.Zero, Vector2.One * titleScale, textRect.Width);
        }

        Texture2D tex = def.Texture?.Value ?? TextureAssets.Item[def.ItemType].Value;
        float maxIcon = 48f;
        float iconScale = maxIcon / Math.Max(tex.Width, tex.Height);
        Rectangle itemSlotRect = new((int)(d.X + d.Width * 0.5f - maxIcon * 0.5f), (int)d.Y + 34, (int)maxIcon, (int)maxIcon);
        Vector2 iconCenter = itemSlotRect.Center.ToVector2();

        sb.Draw(tex, iconCenter, null, Color.White, 0f, tex.Size() * 0.5f, iconScale, SpriteEffects.None, 0f);

        string priceText = def.Price.ToString();
        float priceScale = 1.05f;
        Vector2 priceSize = ChatManager.GetStringSize(FontAssets.MouseText.Value, priceText, Vector2.One * priceScale);

        float py = d.Y + d.Height - priceSize.Y - 6f;
        float px = d.X + d.Width * 0.5f - priceSize.X * 0.5f + 8f;

        Color priceColor = owned || canAfford
            ? Color.White * contentAlpha
            : new Color(148, 39, 39) * contentAlpha;

        Texture2D badgeTex = equipped
            ? Ass.Icon_CheckmarkGreen.Value
            : owned
                ? Ass.Icon_CheckmarkGray.Value
                : Ass.Icon_Gem.Value;

        float badgeScale = equipped || owned ? 1.1f : 0.9f;
        Vector2 badgePos = new(px - 14f, py + 12f);

        if (owned)
            badgePos += new Vector2(0f, -1f);

        sb.Draw(badgeTex, badgePos, null, Color.White, 0f, badgeTex.Size() * 0.5f, badgeScale, SpriteEffects.None, 0f);
        ChatManager.DrawColorCodedStringWithShadow(sb, FontAssets.MouseText.Value, priceText, new Vector2(px + 2f, py), priceColor, 0f, Vector2.Zero, Vector2.One * priceScale);

#if DEBUG
        //DebugMainMenuDrawer.DrawSkinUICard(sb, cardRect, textRect, itemSlotRect, gemRect);
#endif

        if (hover)
            DrawTooltip(owned, equipped, canAfford);
    }

    private static string[] GetTitleLines(string text, float width, float scale)
    {
        string wrapped = FontAssets.MouseText.Value.CreateWrappedText(text, width / scale, Language.ActiveCulture.CultureInfo);
        string[] lines = wrapped.Replace("\r", "").Split('\n', StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length == 0)
            return [text];

        if (lines.Length <= 2)
            return lines;

        string overflow = lines[1];
        for (int i = 2; i < lines.Length; i++)
            overflow += " " + lines[i];

        return [lines[0], TrimToWidth(overflow, width, scale)];
    }

    private static string TrimToWidth(string text, float width, float scale)
    {
        const string suffix = "...";

        while (text.Length > 0 && ChatManager.GetStringSize(FontAssets.MouseText.Value, text + suffix, Vector2.One * scale).X > width)
            text = text[..^1];

        return text.Length == 0 ? suffix : text + suffix;
    }

    private void DrawTooltip(bool owned, bool equipped, bool canAfford)
    {
        Main.LocalPlayer.mouseInterface = true;

        string original = Lang.GetItemNameValue(def.ItemType);
        //string display = $"{def.Name} ({original})";
        string display = $"{def.DisplayName} \n(Original: {original})";
        Color badRed = new(148, 39, 39);

        if (busy)
        {
            UICommon.TooltipMouseText(C(Color.Yellow, "Loading..."));
            return;
        }

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
            UICommon.TooltipMouseText(C(Color.LimeGreen, $"Buy {display}") + "\n" + C(Color.White, $"Price: {def.Price} gems"));
            return;
        }

        UICommon.TooltipMouseText(C(Color.LimeGreen, $"Buy {display}") + "\n" + C(badRed, "Not enough gems"));
    }

    private string C(Color c, string text)
    {
        return $"[c/{c.R:X2}{c.G:X2}{c.B:X2}:{text}]";
    }
}
