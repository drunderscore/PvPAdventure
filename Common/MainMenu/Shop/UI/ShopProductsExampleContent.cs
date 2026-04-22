using System.Collections.Generic;
using System.Linq;
using PvPAdventure.Common.MainMenu.Shop;

namespace PvPAdventure.Common.MainMenu.Shop.UI;

internal static class ShopProductsExampleContent
{
    public static List<ProductDefinition> Create()
    {
        return ProductCatalog.All.ToList();
    }
}