using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Skins;
using PvPAdventure.Content.Items;
using PvPAdventure.Core.Utilities;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Default;
using Terraria.ModLoader.UI;
using Terraria.UI;
using Terraria.UI.Chat;

namespace PvPAdventure.Common.MainMenu.Shop.UI;

internal sealed class SkinUICard : UIElement
{
    private static Asset<Texture2D>? Back;
    private static Asset<Texture2D>? Border;
    private static Asset<Texture2D>? Highlight;

    private readonly SkinDefinition def;

    private readonly UISlicedImage back;
    private readonly UISlicedImage highlight;
    private readonly UISlicedImage border;

    public SkinUICard(SkinDefinition def, float cardW)
    {
        this.def = def;

        // 44x44 textures
        Back ??= Main.Assets.Request<Texture2D>("Images/UI/CharCreation/SmallPanel");
        Border ??= Main.Assets.Request<Texture2D>("Images/UI/CharCreation/SmallPanelBorder");
        Highlight ??= Main.Assets.Request<Texture2D>("Images/UI/CharCreation/CategoryPanelHighlight");

        Width = StyleDimension.FromPixels(cardW);

        back = new UISlicedImage(Back) { Width = StyleDimension.Fill, Height = StyleDimension.Fill, IgnoresMouseInteraction = true };
        highlight = new UISlicedImage(Highlight) { Width = StyleDimension.Fill, Height = StyleDimension.Fill, IgnoresMouseInteraction = true };
        border = new UISlicedImage(Border) { Width = StyleDimension.Fill, Height = StyleDimension.Fill, IgnoresMouseInteraction = true };

        back.SetSliceDepths(10);
        highlight.SetSliceDepths(10);
        border.SetSliceDepths(10);

        // Highlight inset
        float inset = 2f;
        highlight.Left = StyleDimension.FromPixels(inset);
        highlight.Top = StyleDimension.FromPixels(inset);
        highlight.Width = StyleDimension.FromPixelsAndPercent(-inset * 2f, 1f);
        highlight.Height = StyleDimension.FromPixelsAndPercent(-inset * 2f, 1f);

        Append(back);
        //Append(highlight);
        Append(border);

        OnMouseOver += (_,_) => SoundEngine.PlaySound(SoundID.MenuTick);
    }

    public override void MouseOver(UIMouseEvent evt)
    {
        base.MouseOver(evt);
        SoundEngine.PlaySound(12);
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);

        //ProfileStorage.EnsureLoaded();

        //bool hasSkin = ProfileStorage.HasSkin(def.Id);
        //bool hasOtherSkin = ProfileStorage.TryGetSelectedSkinForItem(def.ItemType, out SkinDefinition existing) && existing.Id != def.Id;

        //if (!hasSkin && hasOtherSkin)
        //{
        //    SoundEngine.PlaySound(SoundID.MenuClose);
        //    return;
        //}

        //if (!hasSkin && ProfileStorage.Gems < def.Price)
        //{
        //    SoundEngine.PlaySound(SoundID.MenuClose);
        //    return;
        //}

        //var result = ProfileStorage.ToggleSkin(def);

        //if (result == ProfileStorage.SkinToggleResult.Sold)
        //{
        //    SoundEngine.PlaySound(SoundID.Coins);
        //    return;
        //}

        //if (result != ProfileStorage.SkinToggleResult.None)
        //    SoundEngine.PlaySound(SoundID.Unlock);
    }

    private void DrawItemTooltip(int itemId, bool hasSkin, bool canAfford)
    {
        Color badRed = new(148, 39, 39);

        Main.LocalPlayer.mouseInterface = true;

        Item item = new(itemId);

        string original = Lang.GetItemNameValue(itemId);
        string buyOrSell = hasSkin ? "Sell " : "Buy ";
        if (!canAfford) buyOrSell = "";
        item.SetNameOverride($"{buyOrSell}{def.Name} ({original})");

        if (!hasSkin && !canAfford)
            item.SetNameOverride($"{buyOrSell}{def.Name} ({original})\n{C(badRed, "Not enough gems")}");

        if (Main.gameMenu)
        {
            item.damage = 0;
            item.knockBack = 0f;
            item.crit = 0;
        }

        Main.HoverItem = item;
        Main.hoverItemName = item.Name;

        Main.instance.MouseText("", 0, 0);
        Main.mouseText = true;
    }

    private void DrawTooltip()
    {
        //ProfileStorage.EnsureLoaded();
        //Main.LocalPlayer.mouseInterface = true;

        //static string C(Color c, string text) => $"[c/{c.R:X2}{c.G:X2}{c.B:X2}:{text}]";

        //bool hasSkin = ProfileStorage.HasSkin(def.Id);
        //bool hasOtherSkin = ProfileStorage.TryGetSelectedSkinForItem(def.ItemType, out SkinDefinition existing) && existing.Id != def.Id;
        //bool canAfford = ProfileStorage.Gems >= def.Price;

        //Color badRed = new(148, 39, 39);

        //string original = Lang.GetItemNameValue(def.ItemType);
        //string display = $"{def.Name} ({original})";

        //if (hasSkin)
        //    UICommon.TooltipMouseText(C(Color.LimeGreen, $"Sell {display}") + "\n" + C(Color.White, $"Value: {def.Price} gems"));
        //else if (hasOtherSkin)
        //    UICommon.TooltipMouseText(C(badRed, $"You already have a {existing.Name}"));
        //else if (!canAfford)
        //    UICommon.TooltipMouseText(C(Color.LimeGreen, $"Buy {display}") + "\n" + C(badRed, "Not enough gems"));
        //else
        //    UICommon.TooltipMouseText(C(Color.LimeGreen, $"Buy {display}") + "\n" + C(Color.White, $"Value: {def.Price} gems"));
    }

    

    public override void Draw(SpriteBatch sb)
    {
        //base.Draw(sb);

        //CalculatedStyle d = GetDimensions();

        //ProfileStorage.EnsureLoaded();

        //bool hasSkin = ProfileStorage.HasSkin(def.Id);
        //bool canAfford = ProfileStorage.Gems >= def.Price;
        //bool hover = IsMouseHovering;

        //back.Color = hasSkin
        //    ? new Color(100, 255, 30) * (hover ? 0.9f : 1.0f)
        //    : Color.LightGreen * (hover ? 1f : 0.5f);

        //highlight.Color = hasSkin ? Color.White * 0.0f : Color.Transparent;
        //border.Color = hover ? Color.White : Color.Transparent;

        //base.DrawSelf(sb);

        //// Debug: draw full card size; do not delete!
        ////sb.Draw(TextureAssets.MagicPixel.Value, d.ToRectangle(), Color.Red * 0.35f);

        //float contentAlpha = hasSkin ? 0.5f : 1f;

        //// Draw name (top); keep this comment!
        //string name = def.Name;

        //float t = name.Length > 18 ? MathHelper.Clamp((name.Length - 18) / 12f, 0f, 1f) : 0f;
        //float titleScale = MathHelper.Lerp(0.75f, 0.55f, t);

        //Vector2 nameSize = ChatManager.GetStringSize(FontAssets.MouseText.Value, name, Vector2.One * titleScale);
        //Vector2 namePos = new(d.X + d.Width * 0.5f - nameSize.X * 0.5f, d.Y + 8f);
        //ChatManager.DrawColorCodedStringWithShadow(sb, FontAssets.MouseText.Value, name, namePos, Color.White, 0f, Vector2.Zero, Vector2.One * titleScale, d.Width - 6f);


        //// Draw item (center)
        //Vector2 iconCenter = new(d.X + d.Width * 0.5f, d.Y);
        //iconCenter.Y += 52f;

        //bool unloaded = def.Texture == null || !def.Texture.IsLoaded;

        //Texture2D tex = unloaded ? TextureAssets.Item[ModContent.ItemType<UnloadedItem>()].Value : def.Texture.Value;
        //float maxIcon = 44f;
        //float iconScale = maxIcon / Math.Max(tex.Width, tex.Height);

        //if (!unloaded && def.ItemType == ItemID.SniperRifle)
        //    iconScale *= 1.25f;

        //if (!unloaded && def.ItemType == ModContent.ItemType<AdventureMirror>())
        //    iconScale *= 0.75f;

        //sb.Draw(tex, iconCenter, null, Color.White, 0f, tex.Size() * 0.5f, iconScale, SpriteEffects.None, 0f);

        //// Draw price (bottom)
        //string priceText = def.Price.ToString();
        //float priceScale = 1.05f;

        //Vector2 pSize = ChatManager.GetStringSize(FontAssets.MouseText.Value, priceText, Vector2.One * priceScale);

        //float py = d.Y + d.Height - pSize.Y - 6f;
        //float px = d.X + d.Width * 0.5f - pSize.X * 0.5f + 8f;

        //Color priceColor = (hasSkin || canAfford) ? (Color.White * contentAlpha) : (new Color(148, 39, 39) * contentAlpha);

        //// Draw badge
        //Vector2 badgeAnchor = new(px - 14f, py + 12f);
        //Vector2 gemOffset = new(0f, 0f);
        //Vector2 checkOffset = new(0f, -1f);

        //Texture2D badgeTex = hasSkin ? Ass.Icon_Checkmark.Value : Ass.Icon_Gem.Value;
        //float badgeScale = hasSkin ? 1.5f : 0.9f;

        //Vector2 offset = hasSkin ? checkOffset : gemOffset;
        //Vector2 badgePos = badgeAnchor + offset;

        //sb.Draw(badgeTex, badgePos, null, Color.White, 0f, badgeTex.Size() * 0.5f, badgeScale, SpriteEffects.None, 0f);

        //// Draw price
        //ChatManager.DrawColorCodedStringWithShadow(sb, FontAssets.MouseText.Value, priceText, new Vector2(px+2, py), priceColor, 0f, Vector2.Zero, Vector2.One * priceScale);

        //// Draw tooltip
        //if (hover)
        //{
        //    if (unloaded)
        //    {
        //        string msg = string.IsNullOrEmpty(def.Name) ? "Unloaded" : $"Unloaded: {def.Name}";
        //        UICommon.TooltipMouseText(C(new Color(148, 39, 39), msg));
        //        return;
        //    }

        //    bool hasOtherSkin = ProfileStorage.TryGetSelectedSkinForItem(def.ItemType, out SkinDefinition existing) && existing.Id != def.Id;

        //    Color badRed = new(148, 39, 39);

        //    string original = Lang.GetItemNameValue(def.ItemType);
        //    string display = $"{def.Name} ({original})";

        //    if (hasOtherSkin)
        //        UICommon.TooltipMouseText(C(badRed, $"You already have a {existing.Name}"));
        //    else
        //        DrawItemTooltip(def.ItemType, hasSkin, canAfford);
        //}
    }

    private string C(Color c, string text) => $"[c/{c.R:X2}{c.G:X2}{c.B:X2}:{text}]";

    protected override void DrawSelf(SpriteBatch sb)
    {
        base.DrawSelf(sb);
    }
}