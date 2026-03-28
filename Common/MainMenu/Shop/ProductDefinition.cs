using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ID;

namespace PvPAdventure.Common.MainMenu.Shop;

// Added SkinIdentity to make passing the composite key around much easier
public readonly record struct SkinIdentity(string Prototype, string Name)
{
    public bool IsValid => !string.IsNullOrEmpty(Prototype) && !string.IsNullOrEmpty(Name);
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