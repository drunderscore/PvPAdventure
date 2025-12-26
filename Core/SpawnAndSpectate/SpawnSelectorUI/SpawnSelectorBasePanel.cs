using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.UI.Elements;

namespace PvPAdventure.Core.SpawnAndSpectate.SpawnSelectorUI;

/// <summary>
/// The base panel containing all elements for the spawn selector UI.
/// Includes, for example <see cref="SpawnSelectorCharacter"/>, and <see cref="SpawnSelectorRandomPanel"/>.
/// </summary>
public class SpawnSelectorBasePanel : UIPanel
{
    // UI components
    private SpawnSelectorRandomPanel randomPanel;

    // Collections
    private readonly List<SpawnSelectorCharacter> playerItems = []; // list of teammate player UI items
    
    // Dimensions
    private const float Spacing = 8f; // between items
    private const float HorizontalPadding = 16f; // panel padding
    private const float VerticalPadding = 12f; // panel padding

    public override void OnActivate()
    {
        HAlign = 0.5f;
        VAlign = 0.0f;
        Top.Set(52, 0);

        BackgroundColor = new Color(33, 43, 79) * 1f;
        SetPadding(0f);

        Rebuild();
    }

    private void Rebuild()
    {
        RemoveAllChildren();
        playerItems.Clear();

        var players = new List<Player>();
        var local = Main.LocalPlayer;

        if (local.team != 0)
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];

                if (p == null || !p.active)
                    continue;

                if (p.whoAmI == local.whoAmI || p.team != local.team)
                    continue;

                players.Add(p);
            }
        }

        int playerCount = players.Count;

        float itemWidth = SpawnSelectorCharacter.ItemWidth;
        float itemHeight = SpawnSelectorCharacter.ItemHeight;
        float randomWidth = itemHeight;

        float contentWidth = playerCount * itemWidth
                             + playerCount * Spacing
                             + randomWidth;

        float panelWidth = contentWidth + HorizontalPadding * 2f;
        float panelHeight = itemHeight + VerticalPadding * 2f;

        if (panelWidth <= 0f)
            panelWidth = itemWidth + HorizontalPadding * 2f;

        if (panelHeight <= 0f)
            panelHeight = itemHeight + VerticalPadding * 2f;

        Width.Set(panelWidth, 0f);
        Height.Set(panelHeight, 0f);

        float startX = HorizontalPadding;
        float y = VerticalPadding;

        for (int i = 0; i < playerCount; i++)
        {
            float x = startX + i * (itemWidth + Spacing);

            var row = new SpawnSelectorCharacter(players[i].whoAmI);
            row.Left.Set(x, 0f);
            row.Top.Set(y, 0f);

            Append(row);
            playerItems.Add(row);
        }

        randomPanel = new(
            startX,
            itemHeight,
            playerCount,
            itemWidth,
            Spacing,
            y
        );
        Append(randomPanel);

        Recalculate();
        RecalculateChildren();
    }

    public override void Update(GameTime gameTime)
    {
        if (NeedsRebuild())
        {
            Rebuild();
        }

        base.Update(gameTime);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);
    }

    private bool NeedsRebuild()
    {
        var dims = GetDimensions();
        if (dims.Width <= 0f || dims.Height <= 0f)
            return true;

        if (randomPanel == null)
            return true;

        var randomDims = randomPanel.GetDimensions();
        if (randomDims.Width <= 0f || randomDims.Height <= 0f)
            return true;

        var local = Main.LocalPlayer;
        var players = new List<int>();

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            var p = Main.player[i];
            if (p == null || !p.active)
                continue;
            if (p.whoAmI == local.whoAmI || p.team != local.team)
                continue;

            players.Add(p.whoAmI);
        }

        if (players.Count != playerItems.Count)
            return true;

        for (int i = 0; i < players.Count; i++)
        {
            if (playerItems[i] == null) return true;
            if (playerItems[i].PlayerIndex != players[i]) return true;
        }

        return false;
    }

}
