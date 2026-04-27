using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace PvPAdventure.Common.Spectator.UI.Tabs;

internal interface ISpectatorTab
{
    SpectatorTab Tab { get; }
    string HeaderText { get; }
    string TooltipText { get; }
    Asset<Texture2D> Icon { get; }
    void Refresh();
}

internal enum SpectatorTab
{
    Player,
    World,
    NPCs
}