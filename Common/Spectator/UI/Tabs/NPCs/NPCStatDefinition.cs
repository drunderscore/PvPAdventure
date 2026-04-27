using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;

namespace PvPAdventure.Common.Spectator.UI.Tabs.NPCs;

internal sealed class NPCStatDefinition(string id, string label, Asset<Texture2D> icon, Func<NPC, string> getText, Func<NPC, string>? getHoverText = null, Rectangle? iconFrame = null)
{
    public string Id { get; } = id;
    public string Label { get; } = label;
    public Asset<Texture2D> Icon { get; } = icon;
    public Rectangle? IconFrame { get; } = iconFrame;
    public Func<NPC, string> GetText { get; } = getText;
    public Func<NPC, string>? GetHoverText { get; } = getHoverText;

    public NPCStatSnapshot Build(NPC npc)
    {
        string text = GetText(npc);
        return new(Label, text, GetHoverText?.Invoke(npc) ?? $"{Label}: {text}", Icon, IconFrame);
    }
}

internal readonly record struct NPCStatSnapshot(string Label, string Text, string HoverText, Asset<Texture2D> Icon, Rectangle? IconFrame);