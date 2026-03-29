using Microsoft.Xna.Framework;
using PvPAdventure.Common.MainMenu.Profile;
using PvPAdventure.Common.MainMenu.Shop.UI;
using PvPAdventure.Common.MainMenu.State;
using PvPAdventure.Common.MainMenu.UI;
using System;
using System.Threading.Tasks;
using Terraria;
using Terraria.Localization;
using Terraria.UI;
using Terraria.UI.Gamepad;

namespace PvPAdventure.Common.MainMenu.Shop;

public sealed class ShopUIState : ResizableUIState
{
    private ShopUIPanel shopPanel = null!;

    public override void OnInitialize()
    {
        var root = new UIElement
        {
            Width = new StyleDimension(0f, 0.55f),
            Height = new StyleDimension(0f, 0.95f),
            Top = new StyleDimension(190, 0f),
            HAlign = 0.5f,
            MinWidth = new StyleDimension(650f, 0f),
        };
        Append(root);

        var baseElement = new UIElement
        {
            Width = StyleDimension.Fill,
            Height = new StyleDimension(-160f * 1.75f, 1f),
        };
        root.Append(baseElement);

        shopPanel = new ShopUIPanel
        {
            Width = StyleDimension.Fill,
            Height = StyleDimension.Fill,
            Top = new StyleDimension(6f, 0f),
        };
        shopPanel.SetPadding(12);
        shopPanel.PaddingLeft = 4;
        baseElement.Append(shopPanel);

        root.Append(new UIBackButton<LocalizedText>(Language.GetText("UI.Back"), GoBackToTPVPABrowserState)
        {
            Top = new StyleDimension(-(160f + 50f), 0f),
            VAlign = 1f,
            HAlign = 0f,
            Left = new StyleDimension(0f, 0f),
            Width = StyleDimension.Fill
        });
    }

    public override void OnActivate()
    {
        _ = LoadShopAndProfileAsync();
        shopPanel.Refresh();
    }

    private async Task LoadShopAndProfileAsync()
    {
        try
        {
            Task<string> shopTask = ShopApi.GetShopJsonAsync();
            Task stateTask = ShopApi.RefreshProfileStateAsync();

            string shopJson = await shopTask;
            await stateTask;

            Main.QueueMainThreadAction(() =>
            {
                Products.LoadFromApiJson(shopJson);
                shopPanel.Refresh();
            });
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load shop/profile/inventory: {e}");
        }
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        UILinkPointNavigator.Shortcuts.BackButtonCommand = 7;
    }
}