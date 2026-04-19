using Microsoft.Xna.Framework;
using PvPAdventure.Common.MainMenu.State;
using PvPAdventure.UI;
using System;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Gamepad;

namespace PvPAdventure.Common.MainMenu.Shop.UI;

public sealed class ShopUIState : MainMenuPageUIState
{
    private GemsPanel gemsPanel = null!;
    private UIScrollbar scrollbar = null!;
    private UIList list = null!;

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
        RenderSnapshot(ShopStorage.Snapshot);
        SetCurrentAsyncState(AsyncProviderState.Loading);
        _ = RefreshAndRenderAsync();
    }

    private async System.Threading.Tasks.Task RefreshAndRenderAsync()
    {
        await ShopStorage.RefreshAsync().ConfigureAwait(false);

        Main.QueueMainThreadAction(() =>
        {
            ShopSnapshot snapshot = ShopStorage.Snapshot;
            RenderSnapshot(snapshot);
            SetCurrentAsyncState(snapshot.ErrorMessage == null ? AsyncProviderState.Completed : AsyncProviderState.Aborted, snapshot.ErrorMessage);
        });
    }

    private void RenderSnapshot(ShopSnapshot snapshot)
    {
        list.Clear();
        scrollbar.ViewPosition = 0f;
        gemsPanel.SetContent(snapshot.Profile?.Gems ?? 0, snapshot.IsAuthenticated);

        if (!string.IsNullOrWhiteSpace(snapshot.ErrorMessage) && snapshot.Products.Count == 0)
        {
            list.Add(CreateWrappedMessageElement(snapshot.ErrorMessage, 0.9f, 140f));
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

        int count = snapshot.Products.Count;
        int rows = count / cols + (count % cols != 0 ? 1 : 0);

        UIElement contentRoot = new()
        {
            Width = StyleDimension.Fill,
            Height = new StyleDimension(rows * (cardH + gap), 0f)
        };
        contentRoot.SetPadding(4f);
        list.Add(contentRoot);

        for (int i = 0; i < count; i++)
        {
            int col = i % cols;
            int row = i / cols;

            SkinUICard tile = new(snapshot.Products[i], cardW)
            {
                Height = StyleDimension.FromPixels(cardH),
                Left = new StyleDimension(startX + col * (cardW + gap), 0f),
                Top = new StyleDimension(row * (cardH + gap), 0f)
            };
            contentRoot.Append(tile);
        }

        list.Recalculate();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        UILinkPointNavigator.Shortcuts.BackButtonCommand = 7;

        bool hover = list.IsMouseHovering
            || list.GetDimensions().ToRectangle().Contains(Main.MouseScreen.ToPoint())
            || scrollbar.IsMouseHovering;

        if (hover)
            PlayerInput.LockVanillaMouseScroll("PvPAdventure/ShopList");
    }
}
