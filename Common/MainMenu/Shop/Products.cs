using PvPAdventure.Common.MainMenu.Shop;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PvPAdventure.Common.MainMenu._DeprecatedShopStorage;

public sealed class ApiShopProduct
{
    [JsonPropertyName("prototype")]
    public string Prototype { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("price")]
    public int Price { get; set; }
}

public static class Products
{
    private static ProductDefinition[] _all = [];

    public static IReadOnlyList<ProductDefinition> All => _all;

    public static void Clear()
    {
        _all = [];
    }

    public static void LoadFromApiJson(string json)
    {
        List<ApiShopProduct> apiProducts = JsonSerializer.Deserialize<List<ApiShopProduct>>(json) ?? [];
        List<ProductDefinition> loadedProducts = [];
        HashSet<SkinIdentity> seen = [];

        foreach (ApiShopProduct apiProduct in apiProducts)
        {
            SkinIdentity identity = new(apiProduct.Prototype, apiProduct.Name);

            if (!identity.IsValid)
                continue;

            if (!ProductCatalog.TryGet(identity, out ProductDefinition baseDefinition))
                continue;

            if (!seen.Add(identity))
                continue;

            loadedProducts.Add(baseDefinition with
            {
                Price = apiProduct.Price
            });
        }

        _all = [.. loadedProducts];
    }
}