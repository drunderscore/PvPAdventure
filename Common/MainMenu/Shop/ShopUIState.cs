using Microsoft.Xna.Framework;
using PvPAdventure.Common.MainMenu.API;
using PvPAdventure.Common.MainMenu.Shop.UI;
using PvPAdventure.Common.MainMenu.State;
using PvPAdventure.Common.MainMenu.UI;
using System.Threading.Tasks;
using Terraria;
using Terraria.Localization;
using Terraria.UI;
using Terraria.UI.Gamepad;

namespace PvPAdventure.Common.MainMenu.Shop;

public sealed class ShopUIState : ResizableUIState
{
    private ShopUIPanel shopPanel = null!;
    private bool loading;

    public override void OnInitialize()
    {
        var root = new UIElement
        {
            Width = new StyleDimension(0f, 0.55f),
            Height = new StyleDimension(0f, 0.95f),
            Top = new StyleDimension(190f, 0f),
            HAlign = 0.5f,
            MinWidth = new StyleDimension(650f, 0f)
        };
        Append(root);

        var baseElement = new UIElement
        {
            Width = StyleDimension.Fill,
            Height = new StyleDimension(-160f * 1.75f, 1f)
        };
        root.Append(baseElement);

        shopPanel = new ShopUIPanel
        {
            Width = StyleDimension.Fill,
            Height = StyleDimension.Fill,
            Top = new StyleDimension(6f, 0f)
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
        shopPanel.Refresh();

        if (!loading)
            _ = LoadShopAndProfileAsync();
    }

    private async Task LoadShopAndProfileAsync()
    {
        if (loading)
            return;

        loading = true;

        try
        {
            Task<ApiResult<string>> shopTask = ShopApi.GetShopJsonAsync();
            Task<ApiResult<bool>> profileTask = ProfileApi.RefreshProfileStateAsync();

            await Task.WhenAll(shopTask, profileTask);

            ApiResult<string> shopResult = await shopTask;
            ApiResult<bool> profileResult = await profileTask;

            if (!shopResult.IsSuccess || string.IsNullOrWhiteSpace(shopResult.Data))
            {
                Log.Error($"[ShopUIState] Failed to load shop. Status={(int)shopResult.Status}, Error={shopResult.ErrorMessage}");
                return;
            }

            if (!profileResult.IsSuccess)
            {
                Log.Error($"[ShopUIState] Failed to refresh profile state. Status={(int)profileResult.Status}, Error={profileResult.ErrorMessage}");
                return;
            }

            Main.QueueMainThreadAction(() =>
            {
                Products.LoadFromApiJson(shopResult.Data);
                shopPanel.Refresh();
            });
        }
        catch (System.Exception ex)
        {
            Log.Error($"[ShopUIState] Failed to load shop/profile: {ex}");
        }
        finally
        {
            loading = false;
        }
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        UILinkPointNavigator.Shortcuts.BackButtonCommand = 7;
    }
}