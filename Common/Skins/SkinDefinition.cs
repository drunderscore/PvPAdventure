using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace PvPAdventure.Common.Skins;

public readonly record struct SkinDefinition(
    string Id,
    string Name,
    string Description,
    int Price,
    Asset<Texture2D> Texture,
    int ItemType);

