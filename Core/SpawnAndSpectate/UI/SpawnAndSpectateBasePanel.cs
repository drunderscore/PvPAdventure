using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;

namespace PvPAdventure.Core.SpawnAndSpectate.UI;

/// <summary>
/// The base panel containing all elements for the spawn and spectate UI.
/// Includes, for example <see cref="SpawnAndSpectateCharacter"/>, and <see cref="SpectateExitPanel"/>.
/// </summary>
public class SpawnAndSpectateBasePanel : UIPanel
{
    // UI components
    private RandomTeleportPanel randomPanel;
    private WorldSpawnPanel worldSpawnPanel;

    // Collections
    private readonly List<SpawnAndSpectateCharacter> playerItems = []; // list of teammate player UI items
    
    // Dimensions
    private const float Spacing = 8f; // between items
    private const float HorizontalPadding = 16f; // panel padding
    private const float VerticalPadding = 12f; // panel padding
    public override void OnActivate()
    {
        HAlign = 0.5f;
        VAlign = 0.0f;
        Top.Set(82, 0);

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

#if DEBUG
        bool addedDebugTestPlayer = false;

        if (Main.netMode == NetmodeID.SinglePlayer && players.Count == 0 && local != null && local.active)
        {
            // UI-only "test teammate" (uses the local player's actual data).
            players.Add(local);
            addedDebugTestPlayer = true;
        }
#endif

        int playerCount = players.Count;

        float itemWidth = SpawnAndSpectateCharacter.ItemWidth;
        float itemHeight = SpawnAndSpectateCharacter.ItemHeight;

        float worldWidth = itemHeight;
        float randomWidth = itemHeight;

        int gaps = 1;
        if (playerCount > 0)
        {
            gaps += playerCount - 1;
            gaps += 1;
        }

        float contentWidth =
            worldWidth +
            (playerCount * itemWidth) +
            randomWidth +
            (gaps * Spacing);

        float panelWidth = contentWidth + HorizontalPadding * 2f;
        float panelHeight = itemHeight + VerticalPadding * 2f;

        Width.Set(panelWidth, 0f);
        Height.Set(panelHeight, 0f);

        float x = HorizontalPadding;
        float y = VerticalPadding;

        worldSpawnPanel = new WorldSpawnPanel(itemHeight);
        worldSpawnPanel.Left.Set(x, 0f);
        worldSpawnPanel.Top.Set(y, 0f);
        Append(worldSpawnPanel);

        x += worldWidth + Spacing;

        for (int i = 0; i < playerCount; i++)
        {
#if DEBUG
            bool isDebugTest = false;
            if (addedDebugTestPlayer && players[i].whoAmI == local.whoAmI)
            {
                isDebugTest = true;
            }

            var row = new SpawnAndSpectateCharacter(players[i].whoAmI, isDebugTest);
#else
        var row = new SpawnAndSpectateCharacter(players[i].whoAmI);
#endif

            row.Left.Set(x, 0f);
            row.Top.Set(y, 0f);

            Append(row);
            playerItems.Add(row);

            x += itemWidth;

            if (i < playerCount - 1)
                x += Spacing;
        }

        if (playerCount > 0)
            x += Spacing;

        randomPanel = new RandomTeleportPanel(itemHeight);
        randomPanel.Left.Set(x, 0f);
        randomPanel.Top.Set(y, 0f);
        Append(randomPanel);

        Recalculate();
        RecalculateChildren();
    }

    public override void Update(GameTime gameTime)
    {
        if (IsMouseHovering)
        {
            Main.LocalPlayer.mouseInterface = true; // disable item use when hovering
        }

        if (NeedsRebuild())
        {
            Rebuild();

#if DEBUG
            Main.NewText("[DEBUG]: Rebuilt SpawnAndSpectateBasePanel");
#endif
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

        if (worldSpawnPanel == null)
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

#if DEBUG
        if (Main.netMode == NetmodeID.SinglePlayer && players.Count == 0 && local != null && local.active)
        {
            players.Add(local.whoAmI);
        }
#endif

        if (players.Count != playerItems.Count)
            return true;

        for (int i = 0; i < players.Count; i++)
        {
            if (playerItems[i] == null)
                return true;

            if (playerItems[i].PlayerIndex != players[i])
                return true;
        }

        return false;
    }

}
