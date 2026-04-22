using Microsoft.Xna.Framework;
using PvPAdventure.Common.MainMenu.API;
using PvPAdventure.Common.MainMenu.API.Shop;
using PvPAdventure.Common.MainMenu.Shop;
using PvPAdventure.Common.MainMenu.State;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.MainMenu.Shop.UI;

public sealed class ShopUIState : MainMenuPageUIState
{
    private GemsPanel gemsPanel = null!;
    private UIScrollbar scrollbar = null!;
    private UIList list = null!;

    private List<ProductDefinition> products = [];
    private bool loading;
    private string? error;

    protected override string HeaderLocalizationKey => "Mods.PvPAdventure.MainMenu.Shop";

    protected override void Populate(UIPanel panel)
    {
        panel.SetPadding(12f);
        panel.PaddingLeft = 4f;

        float scrollbarW = 20f;
        float gap = 6f;
        float topPad = 4f;
        float sepPad = 6f;
        float gemsH = 42f;

        gemsPanel = new GemsPanel
        {
            Top = new StyleDimension(topPad, 0f),
            Width = new StyleDimension(0f, 0.2f),
            Height = new StyleDimension(gemsH, 0f),
            Left = new StyleDimension(10f, 0f),
        };
        panel.Append(gemsPanel);

        panel.Append(new UIHorizontalSeparator
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
        panel.Append(scrollbar);

        UIElement listHost = new()
        {
            Top = new StyleDimension(listTop, 0f),
            Width = new StyleDimension(-scrollbarW, 1f),
            Height = new StyleDimension(-listTop - 6f, 1f)
        };
        listHost.SetPadding(0f);
        panel.Append(listHost);

        list = new UIList
        {
            Width = StyleDimension.Fill,
            Height = StyleDimension.Fill
        };
        list.SetPadding(6f);
        list.SetScrollbar(scrollbar);
        listHost.Append(list);
    }

    protected override void RefreshContent()
    {
        loading = true;
        error = null;
        products.Clear();

#if DEBUG
        bool buildExampleContent = false; // Flip to true to test UI without API.
        if (buildExampleContent)
        {
            products = ShopProductsExampleContent.Create();
            loading = false;
            RebuildList();
            SetCurrentAsyncState(AsyncProviderState.Completed, $"Loaded {products.Count} example shop products.");
            return;
        }
#endif

        RebuildList();
        SetCurrentAsyncState(AsyncProviderState.Loading, FormatLoadingMessage("shop products"));
        _ = RefreshShopAsync();
    }

    private async Task RefreshShopAsync()
    {
        ApiResult<List<ProductDefinition>> result = await ProductApi.GetShopAsync().ConfigureAwait(false);

        Main.QueueMainThreadAction(() =>
        {
            loading = false;

            if (result.IsSuccess)
            {
                products = result.Data!;
                error = null;
                RebuildList();
                SetCurrentAsyncState(AsyncProviderState.Completed, $"Loaded {products.Count} shop products.");
            }
            else
            {
                products = [];
                error = result.ErrorMessage;
                RebuildList();
                SetCurrentAsyncState(AsyncProviderState.Aborted, FormatErrorMessage("shop products", error));
            }
        });
    }

    private void RebuildList()
    {
        list.Clear();
        scrollbar.ViewPosition = 0f;

        if (loading)
        {
            list.Add(CreateWrappedMessageElement(FormatLoadingMessage("shop products"), 0.9f, 80f));
            list.Recalculate();
            return;
        }

        if (!string.IsNullOrWhiteSpace(error))
        {
            list.Add(CreateWrappedMessageElement(FormatErrorMessage("shop products", error), 0.9f, 120f));
            list.Recalculate();
            return;
        }

        if (products.Count == 0)
        {
            list.Add(CreateWrappedMessageElement("No shop products found.", 0.9f, 80f));
            list.Recalculate();
            return;
        }

        const float cardWidth = 120f;
        const float cardHeight = 120f;
        const float gap = 4f;

        list.Recalculate();

        float innerWidth = list.GetInnerDimensions().Width;
        if (innerWidth <= 0f)
            innerWidth = cardWidth * 3f + gap * 2f;

        int columns = Math.Max(1, (int)((innerWidth + gap) / (cardWidth + gap)));
        float totalWidth = columns * cardWidth + (columns - 1) * gap;
        float startX = Math.Max(0f, (innerWidth - totalWidth) * 0.5f);

        int count = products.Count;
        int rows = count / columns + (count % columns != 0 ? 1 : 0);

        UIElement contentRoot = new()
        {
            Width = StyleDimension.Fill,
            Height = StyleDimension.FromPixels(rows * (cardHeight + gap))
        };
        contentRoot.SetPadding(4f);
        list.Add(contentRoot);

        for (int i = 0; i < count; i++)
        {
            int column = i % columns;
            int row = i / columns;

            SkinUICard card = new(products[i], cardWidth)
            {
                Height = StyleDimension.FromPixels(cardHeight),
                Left = StyleDimension.FromPixels(startX + column * (cardWidth + gap)),
                Top = StyleDimension.FromPixels(row * (cardHeight + gap))
            };
            contentRoot.Append(card);
        }

        list.Recalculate();
    }

    #region Update
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        UpdateHotfixScrollbar();
    }

    private void UpdateHotfixScrollbar()
    {
        bool hover = list.IsMouseHovering
            || list.GetDimensions().ToRectangle().Contains(Main.MouseScreen.ToPoint())
            || scrollbar.IsMouseHovering;

        if (hover)
            PlayerInput.LockVanillaMouseScroll("PvPAdventure/ShopList");
    }
    #endregion
}