//using Microsoft.Xna.Framework;
//using Microsoft.Xna.Framework.Graphics;
//using PvPAdventure.Common.MainMenu.Profile;
//using PvPAdventure.Common.Skins;
//using PvPAdventure.Core.Utilities;
//using ReLogic.Graphics;
//using System;
//using Terraria;
//using Terraria.Audio;
//using Terraria.GameContent;
//using Terraria.GameContent.UI.Elements;
//using Terraria.ID;
//using Terraria.Localization;
//using Terraria.ModLoader.UI;
//using Terraria.UI;
//using Terraria.UI.Chat;

//namespace PvPAdventure.Common.MainMenu.Shop.UI;

//internal sealed class ShopUICard : UIPanel
//{
//    private readonly ItemSkinDefinition def;
//    private readonly UITextPanel<string> buyButton;
//    private readonly UIPanel useSkinRow;
//    private readonly UIText useSkinLabel;
//    private readonly UIImage useSkinIcon;

//    public ShopUICard(ItemSkinDefinition def)
//    {
//        this.def = def;

//        BackgroundColor = new Color(26, 40, 89) * 0.8f;
//        BorderColor = new Color(13, 20, 44) * 0.8f;
//        SetPadding(10f);

//        buyButton = new UITextPanel<string>("Buy")
//        {
//            BackgroundColor = new Color(63, 82, 151) * 0.85f,
//            BorderColor = Color.Black,
//        };
//        buyButton.Width.Set(0f, 1f);
//        buyButton.Height.Set(42f, 0f);
//        buyButton.Top.Set(-8f - 26f - 6f - 42f, 1f);
//        buyButton.OnLeftClick += OnBuyClicked;
//        Append(buyButton);

//        useSkinRow = new UIPanel
//        {
//            BackgroundColor = new Color(20, 24, 45) * 0.65f,
//            BorderColor = new Color(10, 10, 10) * 0.9f,
//        };
//        useSkinRow.Width.Set(0f, 1f);
//        useSkinRow.Height.Set(26f, 0f);
//        useSkinRow.Top.Set(-8f - 26f, 1f);
//        useSkinRow.SetPadding(0f);
//        useSkinRow.OnLeftClick += OnUseSkinClicked;

//        useSkinLabel = new UIText("Use Skin", 0.85f);
//        useSkinLabel.VAlign = 0.5f;
//        useSkinLabel.Left.Set(8f, 0f);
//        useSkinRow.Append(useSkinLabel);

//        useSkinIcon = new UIImage(Ass.Icon_Off);
//        useSkinIcon.Width.Set(20f, 0f);
//        useSkinIcon.Height.Set(20f, 0f);
//        useSkinIcon.Left.Set(-28f, 1f);
//        useSkinIcon.VAlign = 0.5f;
//        useSkinRow.Append(useSkinIcon);

//        Append(useSkinRow);
//    }

//    private void OnBuyClicked(UIMouseEvent evt, UIElement el)
//    {
//        ProfileStorage.EnsureLoaded();

//        if (ProfileStorage.IsUnlocked(def.Id))
//            return;

//        bool ok = ProfileStorage.SpendGems(def.Price);
//        if (!ok)
//        {
//            SoundEngine.PlaySound(SoundID.MenuClose);
//            return;
//        }

//        ProfileStorage.TryUnlock(def.Id);
//        SoundEngine.PlaySound(SoundID.Unlock);
//    }

//    private void OnUseSkinClicked(UIMouseEvent evt, UIElement el)
//    {
//        ProfileStorage.EnsureLoaded();

//        if (!ProfileStorage.IsUnlocked(def.Id))
//            return;

//        bool enabled = SkinPreference.IsEnabled(def.ItemType, def.Id);
//        SkinPreference.SetEnabled(def.ItemType, def.Id, !enabled);

//        SoundEngine.PlaySound(12);
//    }

//    public override void Update(GameTime gameTime)
//    {
//        base.Update(gameTime);

//        ProfileStorage.EnsureLoaded();

//        bool owned = ProfileStorage.IsUnlocked(def.Id);
//        bool useSkin = owned && SkinPreference.IsEnabled(def.ItemType, def.Id);
//        bool canAfford = ProfileStorage.Gems >= def.Price;

//        buyButton.SetText(owned ? "Owned" : "Buy");

//        bool buyHover = buyButton.IsMouseHovering;

//        if (owned)
//            buyButton.BackgroundColor = new Color(40, 40, 40) * 0.9f;
//        else if (buyHover)
//            buyButton.BackgroundColor = new Color(73, 94, 171);
//        else if (!canAfford)
//            buyButton.BackgroundColor = Color.Lerp(new Color(63, 82, 151) * 0.85f, Color.Black, 0.35f);
//        else
//            buyButton.BackgroundColor = new Color(63, 82, 151) * 0.85f;

//        if (owned)
//            buyButton.BorderColor = new Color(20, 20, 20) * 0.9f;
//        else if (buyHover)
//            buyButton.BorderColor = Colors.FancyUIFatButtonMouseOver;
//        else
//            buyButton.BorderColor = Color.Black;

//        bool useHover = useSkinRow.IsMouseHovering;
//        if (useHover && owned)
//            useSkinRow.BackgroundColor = Color.Lerp(new Color(20, 24, 45) * 0.65f, Color.White, 0.2f);
//        else
//            useSkinRow.BackgroundColor = new Color(20, 24, 45) * 0.65f;

//        useSkinLabel.TextColor = owned ? Color.Silver : Color.DarkGray;

//        useSkinIcon.SetImage(useSkin ? Ass.Icon_On : Ass.Icon_Off);
//        useSkinIcon.Color = owned ? Color.White : Color.DarkGray;
//    }

//    protected override void DrawSelf(SpriteBatch sb)
//    {
//        bool hover = IsMouseHovering;

//        if (hover)
//        {
//            BackgroundColor = new Color(46, 60, 119);
//            BorderColor = new Color(20, 30, 56);
//        }
//        else
//        {
//            BackgroundColor = new Color(26, 40, 89) * 0.8f;
//            BorderColor = new Color(13, 20, 44) * 0.8f;
//        }

//        base.DrawSelf(sb);

//        var inner = GetInnerDimensions();
//        float cx = inner.X + inner.Width * 0.5f;
//        float w = inner.Width - 10f;

//        int slotSize = 64;
//        var slotRect = new Rectangle((int)(cx - slotSize / 2f), (int)inner.Y, slotSize, slotSize);
//        bool slotHover = slotRect.Contains(Main.MouseScreen.ToPoint());

//        if (def.Texture != null)
//        {
//            DrawSlot(sb, slotRect, def.Texture.Value, slotHover);
//        }

//        var font = FontAssets.ItemStack.Value;
//        Color textColor = hover ? Color.White : Color.LightGray;

//        float y = slotRect.Bottom + 8f;
//        y = DrawCenteredText(sb, font, def.Name, cx, y, new Vector2(0.90f), textColor, inner.Width) + 2f;

//        string wrapped = font.CreateWrappedText(def.Description, w / 0.82f, Language.ActiveCulture.CultureInfo);
//        string[] lines = wrapped.Split('\n');
//        float lineH = ChatManager.GetStringSize(font, "A", new Vector2(0.82f)).Y;

//        //foreach (var (line, i) in lines.Select((l, i) => (l.TrimEnd('\r'), i)))
//            //DrawCenteredText(sb, font, line, cx, y + i * lineH, new Vector2(0.82f), textColor, w);

//        //y += lines.Length * lineH + 24f;

//        DrawPrice(sb, font, cx, y, inner.Width, hover);

//        if (slotHover)
//        {
//            // FIXME: Prevent Player.GetWeaponDamage() path in the main menu (crashes sometimes).
//            //UICommon.TooltipMouseText("");

//            //Main.LocalPlayer.mouseInterface = true;
//            //Main.mouseText = true;

//            //Item item = new();
//            //item.SetDefaults(def.ItemType);
//            //item.stack = 1;

//            //Main.HoverItem = item;
//            //Main.hoverItemName = item.Name;
//        }

//        if (buyButton.IsMouseHovering)
//        {
//            ProfileStorage.EnsureLoaded();

//            if (ProfileStorage.IsUnlocked(def.Id))
//                UICommon.TooltipMouseText("You already own this skin.");
//            else
//                UICommon.TooltipMouseText($"Buy for {def.Price} Gems.");
//        }

//        if (useSkinRow.IsMouseHovering)
//        {
//            ProfileStorage.EnsureLoaded();

//            if (ProfileStorage.IsUnlocked(def.Id))
//                UICommon.TooltipMouseText("Toggle whether this skin should be used.");
//            else
//                UICommon.TooltipMouseText("Buy the skin to enable it.");
//        }
//    }

//    private static void DrawSlot(SpriteBatch sb, Rectangle rect, Texture2D icon, bool hover)
//    {
//        Color back = hover ? Color.White : Color.Lerp(Color.White, Color.Black, 0.25f);
//        sb.Draw(TextureAssets.InventoryBack3.Value, rect, back);

//        float scale = (rect.Width * 0.80f) / Math.Max(icon.Width, icon.Height);
//        Color iconTint = hover ? Color.White : new Color(220, 220, 220);
//        sb.Draw(icon, rect.Center.ToVector2(), null, iconTint, 0f, icon.Size() * 0.5f, scale, SpriteEffects.None, 0f);
//    }

//    private static float DrawCenteredText(SpriteBatch sb, DynamicSpriteFont font, string text, float cx, float y, Vector2 scale, Color color, float maxWidth)
//    {
//        var size = ChatManager.GetStringSize(font, text, scale);
//        ChatManager.DrawColorCodedStringWithShadow(sb, font, text, new Vector2(cx - size.X * 0.5f, y), color, 0f, Vector2.Zero, scale, maxWidth);
//        return y + size.Y;
//    }

//    private void DrawPrice(SpriteBatch sb, DynamicSpriteFont font, float cx, float y, float maxWidth, bool hover)
//    {
//        string text = $"{def.Price} Gems";
//        var textSize = ChatManager.GetStringSize(font, text, Vector2.One);

//        float x = cx - (22f + 8f + textSize.X) * 0.5f - 2f;

//        sb.Draw(Ass.Icon_Gem.Value, new Vector2(x, y + 1f), null, Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);

//        Color c = hover ? Color.White : Color.Lerp(Color.White, Color.Black, 0.25f);
//        ChatManager.DrawColorCodedStringWithShadow(sb, font, text, new Vector2(x + 30f, y), c, 0f, Vector2.Zero, Vector2.One, maxWidth);
//    }
//}