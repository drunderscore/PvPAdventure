using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;

namespace PvPAdventure.Common.Spectator.UI.Players;

internal sealed class PlayerStatDefinition(string id, string label, Asset<Texture2D> icon, Func<Player, string> getText, Func<Player, string>? getHoverText = null, Rectangle? iconFrame = null)
{
    public string Id { get; } = id;
    public string Label { get; } = label;
    public Asset<Texture2D> Icon { get; } = icon;
    public Rectangle? IconFrame { get; } = iconFrame;
    public Func<Player, string> GetText { get; } = getText;
    public Func<Player, string>? GetHoverText { get; } = getHoverText;

    public PlayerStatSnapshot Build(Player player)
    {
        string text = GetText(player);
        return new(Label, text, GetHoverText?.Invoke(player) ?? $"{Label}: {text}", Icon, IconFrame);
    }
}

internal readonly record struct PlayerStatSnapshot(string Label, string Text, string HoverText, Asset<Texture2D> Icon, Rectangle? IconFrame);
