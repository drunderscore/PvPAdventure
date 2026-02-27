using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.MainMenu.Shop;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader.UI;
using Terraria.ModLoader.UI.Elements;
using Terraria.UI;

namespace PvPAdventure.Common.MainMenu.Shop.UI;

internal sealed class ShopUIPanel : UIPanel
{
    private const float ScrollbarWidth = 20f;

    public ShopUIPanel()
    {
        BorderColor = Color.Black;
        BackgroundColor = new Color(33, 43, 79) * 0.8f;
        Refresh();
    }

    public void Refresh()
    {
        RemoveAllChildren();

        Append(new UITextPanel<string>(Language.GetTextValue("Mods.PvPAdventure.MainMenu.Shop"), 0.9f, true)
        {
            Top = new StyleDimension(-48f, 0f),
            HAlign = 0.5f,
            BackgroundColor = UICommon.DefaultUIBlue
        });

        const float gemsHeight = 42f;
        const float gap = 6f;
        const float topPad = 10f;
        const float separatorPad = 16f;

        var gems = new ShopGemsUIPanel
        {
            Top = new StyleDimension(topPad, 0f),
            Width = new StyleDimension(0, 0.2f),
            Height = new StyleDimension(gemsHeight, 0f),
        };
        Append(gems);

        Append(new UIHorizontalSeparator()
        {
            Width = StyleDimension.FromPixelsAndPercent(-20f, 1f),
            Top = StyleDimension.FromPixels(gemsHeight + gap + topPad + separatorPad),
            HAlign = 0.5f,
            Color = Color.Lerp(Color.White, new Color(63, 65, 151, 255), 0.85f) * 0.9f
        });

        float gridTop = gemsHeight + gap + topPad + separatorPad + 20f;

        var gridContainer = new UIElement
        {
            Top = new StyleDimension(gridTop, 0f),
            Width = new StyleDimension(-ScrollbarWidth - 6f, 1f),
            Height = new StyleDimension(-gridTop - 6f, 1f)
        };
        gridContainer.SetPadding(0f);
        Append(gridContainer);

        var scrollbar = new UIScrollbar
        {
            Top = new StyleDimension(gridTop, 0f),
            Height = new StyleDimension(-gridTop - 6f, 1f),
            Width = new StyleDimension(ScrollbarWidth, 0f),
            Left = new StyleDimension(-ScrollbarWidth, 1f)
        };
        Append(scrollbar);

        var grid = new UIGrid
        {
            Width = StyleDimension.Fill,
            Height = StyleDimension.Fill,
            ListPadding = 10f
        };
        grid.SetScrollbar(scrollbar);
        gridContainer.Append(grid);

        const int columns = 3;

        for (int i = 0; i < ShopItems.All.Length; i++)
        {
            var card = new ShopUICard(ShopItems.All[i])
            {
                Width = StyleDimension.FromPixelsAndPercent(-8f, 1f / columns),
                Height = StyleDimension.FromPixels(210f)
            };
            grid.Add(card);
        }

        grid.Recalculate();
    }
}

