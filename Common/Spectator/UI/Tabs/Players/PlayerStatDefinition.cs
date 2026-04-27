using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;

namespace PvPAdventure.Common.Spectator.UI.Tabs.Players;

internal sealed class PlayerStatDefinition
{
    public PlayerStatDefinition(
        string id,
        string label,
        Asset<Texture2D> icon,
        Func<Player, string> getText,
        Func<Player, string>? getHoverText = null,
        Microsoft.Xna.Framework.Rectangle? iconFrame = null)
        : this(id, label, _ => icon, getText, getHoverText, _ => iconFrame)
    {
    }

    public PlayerStatDefinition(
        string id,
        string label,
        Func<Player, Asset<Texture2D>> getIcon,
        Func<Player, string> getText,
        Func<Player, string>? getHoverText = null,
        Func<Player, Microsoft.Xna.Framework.Rectangle?>? getIconFrame = null)
    {
        Id = id;
        Label = label;
        GetIcon = getIcon;
        GetText = getText;
        GetHoverText = getHoverText;
        GetIconFrame = getIconFrame;
    }

    public string Id { get; }
    public string Label { get; }
    public Func<Player, Asset<Texture2D>> GetIcon { get; }
    public Func<Player, string> GetText { get; }
    public Func<Player, string>? GetHoverText { get; }
    public Func<Player, Rectangle?>? GetIconFrame { get; }

    public PlayerStatSnapshot Build(Player player)
    {
        string text = GetText(player);
        return new PlayerStatSnapshot(Label, text, GetHoverText?.Invoke(player) ?? $"{Label}: {text}", GetIcon(player), GetIconFrame?.Invoke(player));
    }
}

internal readonly record struct PlayerStatSnapshot(
    string Label,
    string Text,
    string HoverText,
    Asset<Texture2D> Icon,
    Rectangle? IconFrame);