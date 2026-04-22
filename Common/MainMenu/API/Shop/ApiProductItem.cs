using System.Text.Json.Serialization;

namespace PvPAdventure.Common.MainMenu.API.Shop;

public sealed class ApiProductItem
{
    [JsonPropertyName("prototype")]
    public string Prototype { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
}