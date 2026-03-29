using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.Localization;
using Terraria.ModLoader.UI;
using Terraria.ModLoader.UI.Elements;
using Terraria.UI;

namespace PvPAdventure.Common.MainMenu.Shop.UI;

internal sealed class ShopUIPanel : UIPanel
{
    private readonly UIScrollbar scrollbar;
    private readonly UIList list;

    public ShopUIPanel()
    {
        float scrollbarW = 20f;
        float gap = 6f;
        float topPad = 4f;
        float sepPad = 6f;
        float gemsH = 42f;

        BorderColor = Color.Black;
        BackgroundColor = new Color(33, 43, 79) * 0.8f;

        Append(new UITextPanel<string>(Language.GetTextValue("Mods.PvPAdventure.MainMenu.Shop"), 0.9f, true)
        {
            Top = new StyleDimension(-48f, 0f),
            HAlign = 0.5f,
            BackgroundColor = UICommon.DefaultUIBlue
        });

        Append(new GemsPanel
        {
            Top = new StyleDimension(topPad, 0f),
            Width = new StyleDimension(0f, 0.2f),
            Height = new StyleDimension(gemsH, 0f),
            Left = new StyleDimension(10, 0f),
        });

        Append(new UIHorizontalSeparator
        {
            Width = StyleDimension.FromPixelsAndPercent(-20f, 1f),
            Top = StyleDimension.FromPixels(gemsH + gap + topPad + sepPad),
            HAlign = 0.5f,
            Color = Color.Lerp(Color.White, new Color(63, 65, 151, 255), 0.85f) * 0.9f
        });

        float listTop = gemsH + gap + topPad + sepPad + 16f;

        scrollbar = new UIScrollbar
        {
            Top = new StyleDimension(listTop + 8f, 0f),
            Height = new StyleDimension(-listTop - 20f, 1f),
            Width = new StyleDimension(scrollbarW, 0f),
            HAlign = 1f
        };
        scrollbar.SetView(100f, 1000f);
        Append(scrollbar);

        var listHost = new UIElement
        {
            Top = new StyleDimension(listTop, 0f),
            Width = new StyleDimension(-scrollbarW, 1f),
            Height = new StyleDimension(-listTop - 6f, 1f)
        };
        listHost.SetPadding(0f);
        Append(listHost);

        list = new UIList
        {
            Width = StyleDimension.Fill,
            Height = StyleDimension.Fill
        };
        list.SetPadding(6f);
        list.SetScrollbar(scrollbar);
        listHost.Append(list);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        bool hover = list.IsMouseHovering
            || list.GetDimensions().ToRectangle().Contains(Main.MouseScreen.ToPoint())
            || scrollbar.IsMouseHovering;

        if (hover)
            PlayerInput.LockVanillaMouseScroll("PvPAdventure/ShopList");
    }

    public void Refresh()
    {
        list.Clear();
        scrollbar.ViewPosition = 0f;

        IReadOnlyList<ProductDefinition> products = Products.All;

        float cardW = 120f;
        float cardH = 120f;
        float gap = 4f;

        list.Recalculate();

        float innerW = list.GetInnerDimensions().Width;
        if (innerW <= 0f)
            innerW = cardW * 3f + gap * 2f;

        int cols = Math.Max(1, (int)((innerW + gap) / (cardW + gap)));
        float totalW = cols * cardW + (cols - 1) * gap;
        float startX = Math.Max(0f, (innerW - totalW) * 0.5f);

        int count = products.Count;
        int rows = (count / cols) + (count % cols != 0 ? 1 : 0);

        var content = new UIElement
        {
            Width = StyleDimension.Fill,
            Height = new StyleDimension(rows * (cardH + gap), 0f)
        };
        content.SetPadding(4f);

        list.Add(content);

        for (int i = 0; i < count; i++)
        {
            int col = i % cols;
            int row = i / cols;

            var tile = new SkinUICard(products[i], cardW)
            {
                Height = StyleDimension.FromPixels(cardH),
                Left = new StyleDimension(startX + col * (cardW + gap), 0f),
                Top = new StyleDimension(row * (cardH + gap), 0f)
            };

            content.Append(tile);
        }

        list.Recalculate();
    }
}