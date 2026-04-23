using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace PvPAdventure.Common.MainMenu.Shop;

public readonly record struct ShopProduct(
    string Prototype,
    string Name,
    string DisplayName,
    Asset<Texture2D> Texture,
    int ItemType,
    int Price = 0);