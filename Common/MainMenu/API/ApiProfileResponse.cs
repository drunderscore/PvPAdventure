using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PvPAdventure.Common.MainMenu.API;

public sealed class ApiProfileResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("gems")]
    public int Gems { get; set; }

    [JsonPropertyName("equipment")]
    public Dictionary<string, string> Equipment { get; set; } = [];
}

public sealed class ApiInventoryItem
{
    [JsonPropertyName("prototype")]
    public string Prototype { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
}

internal sealed record ProfileSnapshot(ApiProfileResponse Profile, List<ApiInventoryItem> Inventory);