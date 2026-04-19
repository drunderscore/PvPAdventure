using System.Collections.Generic;
using System.Linq;
using PvPAdventure.Common.MainMenu.API;

namespace PvPAdventure.Common.MainMenu.Shop.UI;

internal static class ShopExampleContent
{
    public static ShopSnapshot CreateSnapshot()
    {
        List<ProductDefinition> products = ProductCatalog.All
            .Select((product, index) => product with { Price = 50 + index * 25 })
            .ToList();

        return new ShopSnapshot(
            Products: products,
            Profile: new ApiProfileResponse
            {
                Id = "example-profile",
                Gems = 245,
                Equipment = []
            },
            Inventory: [],
            ErrorMessage: null,
            IsAuthenticated: true);
    }
}
