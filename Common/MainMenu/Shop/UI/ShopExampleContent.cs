using System.Linq;

namespace PvPAdventure.Common.MainMenu.Shop.UI;

internal static class ShopExampleContent
{
    public static ShopUIContent Create()
    {
        ProductDefinition[] products = ProductCatalog.All
            .Select((product, index) => product with { Price = 50 + index * 25 })
            .ToArray();

        return new ShopUIContent(245, products);
    }
}
