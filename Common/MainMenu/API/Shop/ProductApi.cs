using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PvPAdventure.Common.MainMenu.API;
using PvPAdventure.Common.MainMenu.Shop;

namespace PvPAdventure.Common.MainMenu.API.Shop;

internal static class ProductApi
{
    public static async Task<ApiResult<List<ShopProduct>>> GetShopAsync(CancellationToken cancellationToken = default)
    {
        ApiResult<List<ApiProductItem>> result = await ApiClient.GetJsonAsync<List<ApiProductItem>>("shop/v1", cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess || result.Data is null)
            return ApiResult<List<ShopProduct>>.Error(result.Status, result.ErrorMessage ?? "Failed to load shop.", result.RequestSummary);

        List<ShopProduct> products = [];

        foreach (ApiProductItem item in result.Data)
        {
            if (!ProductCatalog.TryGet(item.Prototype, item.Name, out ShopProduct definition))
                continue;

            products.Add(new ShopProduct(
            Prototype: definition.Prototype,
            Name: definition.Name,
            DisplayName: definition.DisplayName,
            Texture: definition.Texture,
            ItemType: definition.ItemType,
            Price: item.Price));
        }

        return ApiResult<List<ShopProduct>>.Success(products, result.Status, result.RequestSummary);
    }
}