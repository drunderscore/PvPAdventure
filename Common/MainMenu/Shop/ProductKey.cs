namespace PvPAdventure.Common.MainMenu.Shop;

public readonly record struct ProductKey(string Prototype, string Name)
{
    public bool IsValid => !string.IsNullOrWhiteSpace(Prototype) && !string.IsNullOrWhiteSpace(Name);
}