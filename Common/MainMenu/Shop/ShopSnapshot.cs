using System.Collections.Generic;
using PvPAdventure.Common.MainMenu.API;

namespace PvPAdventure.Common.MainMenu.Shop;

internal sealed record ShopSnapshot(
    List<ProductDefinition> Products,
    ApiProfileResponse? Profile,
    List<ApiInventoryItem>? Inventory,
    string? ErrorMessage,
    bool IsAuthenticated);
