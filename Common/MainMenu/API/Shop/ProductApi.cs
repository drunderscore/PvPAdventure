using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PvPAdventure.Common.MainMenu.API;
using PvPAdventure.Common.MainMenu.Shop;

namespace PvPAdventure.Common.MainMenu.API.Shop;

internal static class ProductApi
{
    public static async Task<ApiResult<List<ProductDefinition>>> GetShopAsync(CancellationToken cancellationToken = default)
    {
        ApiResult<List<ApiProductItem>> result = await ApiClient.GetJsonAsync<List<ApiProductItem>>("shop/v1", cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess || result.Data is null)
            return ApiResult<List<ProductDefinition>>.Error(result.Status, result.ErrorMessage ?? "Failed to load shop.", result.RequestSummary);

        List<ProductDefinition> products = [];

        foreach (ApiProductItem item in result.Data)
        {
            if (ProductCatalog.TryGet(item.Prototype, item.Name, out ProductDefinition definition))
            {
                if (item.Price > 0)
                    definition = definition with { Price = item.Price };

                products.Add(definition);
            }
        }

        return ApiResult<List<ProductDefinition>>.Success(products, result.Status, result.RequestSummary);
    }
}
