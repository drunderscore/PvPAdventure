using System.Collections.Generic;
using System.Linq;

namespace PvPAdventure.Common.MainMenu.Shop.UI;

internal static class ShopProductsExampleContent
{
    public static List<ShopProduct> Create()
    {
        return ProductCatalog.All.ToList();
    }
}