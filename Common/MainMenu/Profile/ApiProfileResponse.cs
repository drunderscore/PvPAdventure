using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PvPAdventure.Common.MainMenu.Profile;

public sealed class ApiProfileResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("gems")]
    public int Gems { get; set; }

    // Maps Prototype -> Name (e.g., "sniper_rifle" -> "blue")
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