using Microsoft.Xna.Framework;
using PvPAdventure.Common.MainMenu.API;
using PvPAdventure.Common.MainMenu.State;
using System;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Gamepad;

namespace PvPAdventure.Common.MainMenu.Shop.UI;

public sealed class ShopUIState : MainMenuPageUIState
{
    private UIScrollbar scrollbar = null!;
    private UIList list = null!;
    private string? stateMessage;
    private int loadVersion;

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

        panel.Append(new GemsPanel
        {
            Top = new StyleDimension(topPad, 0f),
            Width = new StyleDimension(0f, 0.2f),
            Height = new StyleDimension(gemsH, 0f),
            Left = new StyleDimension(10f, 0f),
        });

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

    //public override void Update(GameTime gameTime)
    //{
    //    base.Update(gameTime);
    //    UILinkPointNavigator.Shortcuts.BackButtonCommand = 7;

    //    bool hover = list.IsMouseHovering
    //        || list.GetDimensions().ToRectangle().Contains(Main.MouseScreen.ToPoint())
    //        || scrollbar.IsMouseHovering;

    //    if (hover)
    //        PlayerInput.LockVanillaMouseScroll("PvPAdventure/ShopList");
    //}

    protected override void RefreshContent()
    {
        int version = ++loadVersion;
        SetCurrentAsyncState(AsyncProviderState.Loading);

        if (Products.All.Count == 0)
            SetStateMessage(MainMenuPageUIState.FormatLoadingMessage("shop items"));

        _ = LoadShopAndProfileAsync(version);
    }

    private void RefreshList()
    {
        list.Clear();
        scrollbar.ViewPosition = 0f;

        if (!string.IsNullOrWhiteSpace(stateMessage))
        {
            list.Add(MainMenuPageUIState.CreateWrappedMessageElement(stateMessage, 0.9f, 140f));
            list.Recalculate();
            return;
        }

        var products = Products.All;
        if (products.Count == 0)
        {
            list.Add(new UIText("No shop items available.\nDebug: Is the TPVPA API running?", 0.9f)
            {
                HAlign = 0.5f,
                Top = new StyleDimension(24f, 0f),
                TextColor = Color.LightGray
            });

            list.Recalculate();
            return;
        }

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
        int rows = count / cols + (count % cols != 0 ? 1 : 0);

        UIElement content = new()
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

            SkinUICard tile = new(products[i], cardW)
            {
                Height = StyleDimension.FromPixels(cardH),
                Left = new StyleDimension(startX + col * (cardW + gap), 0f),
                Top = new StyleDimension(row * (cardH + gap), 0f)
            };
            content.Append(tile);
        }

        list.Recalculate();
    }

    private void SetStateMessage(string? message)
    {
        if (stateMessage == message)
            return;

        stateMessage = message;
        RefreshList();
    }

    private async Task LoadShopAndProfileAsync(int version)
    {
        void Fail(string error)
        {
            Main.QueueMainThreadAction(() =>
            {
                if (version != loadVersion)
                    return;

                SetStateMessage(error);
                SetCurrentAsyncState(AsyncProviderState.Aborted);
            });
        }

        try
        {
            Task<ApiResult<string>> shopTask = ShopApi.GetShopJsonAsync();
            Task<ApiResult<bool>> profileTask = ProfileApi.RefreshProfileStateAsync();

            await Task.WhenAll(shopTask, profileTask).ConfigureAwait(false);

            ApiResult<string> shopResult = await shopTask.ConfigureAwait(false);
            ApiResult<bool> profileResult = await profileTask.ConfigureAwait(false);

            if (!shopResult.IsSuccess || string.IsNullOrWhiteSpace(shopResult.Data))
            {
                Fail(MainMenuPageUIState.FormatErrorMessage("shop items", shopResult.ErrorMessage));
                return;
            }

            if (!profileResult.IsSuccess)
            {
                Fail(MainMenuPageUIState.FormatErrorMessage("shop items", profileResult.ErrorMessage));
                return;
            }

            Products.LoadFromApiJson(shopResult.Data);

            Main.QueueMainThreadAction(() =>
            {
                if (version != loadVersion)
                    return;

                SetStateMessage(null);
                RefreshList();
                SetCurrentAsyncState(AsyncProviderState.Completed);
            });
        }
        catch (Exception ex)
        {
            Log.Error($"[ShopUIState] Failed to load shop/profile: {ex}");
            Fail(MainMenuPageUIState.FormatErrorMessage("shop items", ex.Message));
        }
    }
}