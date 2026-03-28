using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PvPAdventure.Common.MainMenu.Profile;

public class ApiProfileResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("gems")]
    public int Gems { get; set; }

    [JsonPropertyName("equipment")]
    public Dictionary<string, string> Equipment { get; set; }
}

public class ApiInventoryItem
{
    [JsonPropertyName("prototype")]
    public string Prototype { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }
}

public class ApiPurchaseRequest
{
    [JsonPropertyName("prototype")]
    public string Prototype { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }
}

public class ApiEquipmentUpdateRequest
{
    [JsonPropertyName("prototype")]
    public string Prototype { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; } // Nullable to support un-equipping!
}
