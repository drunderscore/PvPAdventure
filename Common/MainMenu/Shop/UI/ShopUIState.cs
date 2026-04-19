using Microsoft.Xna.Framework;
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
        list.Clear();
        scrollbar.ViewPosition = 0f;

        //if (!string.IsNullOrWhiteSpace(stateMessage))
        //{
        //    list.Add(MainMenuPageUIState.CreateWrappedMessageElement(stateMessage, 0.9f, 140f));
        //    list.Recalculate();
        //    return;
        //}

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

        var products = Products.All;
        int count = products.Count;
        int rows = count / cols + (count % cols != 0 ? 1 : 0);

        // Debug log
        Log.Info($"Loaded {products.Count} shop products");
        for (int i = 0; i < products.Count; i++)
            Log.Info($"Product[{i}] Prototype='{products[i].Identity.Prototype}' Name='{products[i].Identity.Name}' DisplayName='{products[i].DisplayName}' Price={products[i].Price} ItemType={products[i].ItemType}");

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