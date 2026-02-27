using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace PvPAdventure.Common.MainMenu.Shop;

/// <summary>
/// Represents a TPVPA shop item that can be bought.
/// </summary>
public sealed record ShopItemDefinition(
    string Id,
    string Title,
    string Description,
    int CostGems,
    Asset<Texture2D> Icon);