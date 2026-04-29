using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PvPAdventure.Common.MainMenu.API.Profile;

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

    [JsonPropertyName("issued")]
    public DateTime Issued { get; set; }

    [JsonPropertyName("purchasePrice")]
    public int? PurchasePrice { get; set; }
}
