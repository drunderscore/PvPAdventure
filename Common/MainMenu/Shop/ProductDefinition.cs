using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace PvPAdventure.Common.MainMenu.Shop;

public readonly record struct SkinIdentity(string Prototype, string Name)
{
    public bool IsValid => !string.IsNullOrWhiteSpace(Prototype) && !string.IsNullOrWhiteSpace(Name);
}

public readonly record struct ProductDefinition(
    string Prototype,
    string Name,
    string DisplayName,
    string Description,
    int Price,
    Asset<Texture2D> Texture,
    int ItemType)
{
    public SkinIdentity Identity => new(Prototype, Name);
}