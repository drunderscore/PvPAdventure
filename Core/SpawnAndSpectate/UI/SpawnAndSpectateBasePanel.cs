using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;

namespace PvPAdventure.Core.SpawnAndSpectate.UI;

/// <summary>
/// The base panel containing all elements for the spawn and spectate UI.
/// Includes, for example
/// <see cref="WorldSpawnPanel"/>
/// <see cref="SpawnAndSpectateCharacter"/>
/// <see cref="RandomTeleportPanel"/>
/// </summary>
public class SpawnAndSpectateBasePanel : UIPanel
{
    // UI components
    private RandomTeleportPanel randomPanel;
    private WorldSpawnPanel worldSpawnPanel;
    private readonly List<SpawnAndSpectateCharacter> playerItems = []; // list of teammate player UI items
    
    // Dimensions
    private const float Spacing = 8f; // between items
    private const float HorizontalPadding = 16f; // panel padding
    private const float VerticalPadding = 12f; // panel padding

    // Debug
#if DEBUG
    private static int s_debugExtraLocalCopies;
#endif

    public override void OnActivate()
    {
        HAlign = 0.5f;
        VAlign = 0.0f;
        Top.Set(82, 0);

        BackgroundColor = new Color(33, 43, 79) * 1f;
        SetPadding(0f);

        Rebuild();
    }

    public override void Update(GameTime gameTime)
    {
        if (IsMouseHovering)
        {
            Main.LocalPlayer.mouseInterface = true; // disable item use when hovering
        }

#if DEBUG
        if (!Main.drawingPlayerChat)
        {
            if (Main.keyState.IsKeyDown(Keys.L) && !Main.oldKeyState.IsKeyDown(Keys.L))
            {
                Log.Chat("+1 character UI. Use J or L to adjust.");
                s_debugExtraLocalCopies++;
                Rebuild();
            }
            else if (Main.keyState.IsKeyDown(Keys.J) && !Main.oldKeyState.IsKeyDown(Keys.J))
            {
                if (s_debugExtraLocalCopies > 0)
                {
                    s_debugExtraLocalCopies--;
                    Log.Chat("-1 character UI. Use J or L to adjust.");
                }

                Rebuild();
            }
        }
#endif

        if (NeedsRebuild())
        {
            Rebuild();

            Log.Chat("Called Rebuild() in SpawnAndSpectateBasePanel");
        }

        base.Update(gameTime);
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
        if (Main.netMode != NetmodeID.Server && local != null && local.active)
        {
            for (int i = 0; i < s_debugExtraLocalCopies; i++)
            {
                players.Add(local);
            }
        }
#endif

        int playerCount = players.Count;

        var density = SpawnAndSpectateCharacter.GetDensityForTeammateCount(playerCount);
        float itemWidth = SpawnAndSpectateCharacter.GetItemWidth(density);
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
        worldSpawnPanel.Activate();
        worldSpawnPanel.Left.Set(x, 0f);
        worldSpawnPanel.Top.Set(y, 0f);
        Append(worldSpawnPanel);

        x += worldWidth + Spacing;

        for (int i = 0; i < playerCount; i++)
        {
            // Create player item
            var row = new SpawnAndSpectateCharacter(players[i].whoAmI, density);
            row.Activate();

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
        randomPanel.Activate();
        randomPanel.Left.Set(x, 0f);
        randomPanel.Top.Set(y, 0f);
        Append(randomPanel);

        Recalculate();
        RecalculateChildren();
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
        if (Main.netMode != NetmodeID.Server && local != null && local.active)
        {
            for (int i = 0; i < s_debugExtraLocalCopies; i++)
            {
                players.Add(local.whoAmI);
            }
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

        // MAYBE: Go through all playeritems and ensure they have a size?
        //for (int i = 0; i < playerItems.Count; i++)
        //{
        //    var item = playerItems[i];
        //    var itemDims = item.GetDimensions();
        //    if (itemDims.Width <= 0f || itemDims.Height <= 0f)
        //        return true;
        //}

        return false;
    }

}
