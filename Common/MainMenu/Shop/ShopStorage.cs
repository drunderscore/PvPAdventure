using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using PvPAdventure.Common.MainMenu.API;
using PvPAdventure.Common.MainMenu.Shop.UI;

namespace PvPAdventure.Common.MainMenu.Shop;

internal static class ShopStorage
{
    private static readonly MainMenuAsyncSnapshot<ShopSnapshot> state = new(CreateInitialSnapshot());

    public static ShopSnapshot Snapshot => state.Current;
    public static bool IsLoading => state.IsLoading;

    public static async Task RefreshAsync()
    {
        int version = state.BeginRefresh();
        ShopSnapshot current = state.Current;
        bool buildExampleContent = false;

        if (buildExampleContent)
        {
            state.TrySetSnapshot(version, ShopExampleContent.CreateSnapshot());
            return;
        }

        Task<ApiResult<List<ApiShopProduct>>> shopTask = ApiClient.GetJsonAsync<List<ApiShopProduct>>("shop/v1");
        Task<ApiResult<ApiProfileResponse>> profileTask = ApiClient.GetJsonAsync<ApiProfileResponse>("profile/v1");
        Task<ApiResult<List<ApiInventoryItem>>> inventoryTask = ApiClient.GetJsonAsync<List<ApiInventoryItem>>("profile/inventory/v1");

        await Task.WhenAll(shopTask, profileTask, inventoryTask).ConfigureAwait(false);

        ApiResult<List<ApiShopProduct>> shopResult = await shopTask.ConfigureAwait(false);
        ApiResult<ApiProfileResponse> profileResult = await profileTask.ConfigureAwait(false);
        ApiResult<List<ApiInventoryItem>> inventoryResult = await inventoryTask.ConfigureAwait(false);

        List<ProductDefinition> products = shopResult.IsSuccess && shopResult.Data != null
            ? BuildProducts(shopResult.Data)
            : current.Products;

        ApiProfileResponse? profile = profileResult.IsSuccess ? profileResult.Data : current.Profile;
        List<ApiInventoryItem>? inventory = inventoryResult.IsSuccess ? inventoryResult.Data ?? [] : current.Inventory;

        bool isAuthenticated = profileResult.IsSuccess && inventoryResult.IsSuccess
            ? true
            : IsForbidden(profileResult) || IsForbidden(inventoryResult)
                ? false
                : current.IsAuthenticated;

        string? errorMessage = BuildErrorMessage(shopResult, profileResult, inventoryResult);

        ShopSnapshot nextSnapshot = current with
        {
            Products = products,
            Profile = profile,
            Inventory = inventory,
            ErrorMessage = errorMessage,
            IsAuthenticated = isAuthenticated
        };

        state.TrySetSnapshot(version, nextSnapshot);
    }

    private static ShopSnapshot CreateInitialSnapshot()
    {
        return new ShopSnapshot(
            Products: [.. ProductCatalog.All],
            Profile: null,
            Inventory: null,
            ErrorMessage: null,
            IsAuthenticated: false);
    }

    private static List<ProductDefinition> BuildProducts(List<ApiShopProduct> apiProducts)
    {
        List<ProductDefinition> products = [];
        HashSet<SkinIdentity> seen = [];

        for (int i = 0; i < apiProducts.Count; i++)
        {
            ApiShopProduct apiProduct = apiProducts[i];
            SkinIdentity identity = new(apiProduct.Prototype, apiProduct.Name);

            if (!identity.IsValid || !seen.Add(identity) || !ProductCatalog.TryGet(identity, out ProductDefinition baseDefinition))
                continue;

            products.Add(baseDefinition with { Price = apiProduct.Price });
        }

        return products;
    }

    private static bool IsForbidden<T>(ApiResult<T> result)
    {
        return !result.IsSuccess && result.Status == HttpStatusCode.Forbidden;
    }

    private static string? BuildErrorMessage(
        ApiResult<List<ApiShopProduct>> shopResult,
        ApiResult<ApiProfileResponse> profileResult,
        ApiResult<List<ApiInventoryItem>> inventoryResult)
    {
        if (shopResult.IsSuccess && profileResult.IsSuccess && inventoryResult.IsSuccess)
            return null;

        return string.Join("\n", shopResult.RequestSummary, profileResult.RequestSummary, inventoryResult.RequestSummary);
    }
}
