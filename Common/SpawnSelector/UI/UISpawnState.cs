using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PvPAdventure.Core.Debug;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace PvPAdventure.Common.SpawnSelector.UI;

/// <summary>
/// The main state containing all elements for the spawn and spectate UI.
/// Contains the following UI:
/// <see cref="UIWorldSpawnPanel"/>
/// <see cref="UISpawnCharacter"/> (which contains: <seealso cref="UIBedButton"/>)
/// <see cref="UIRandomTeleportPanel"/>
/// </summary>
public class UISpawnState : UIState
{
    // UI components
    public UIPanel backgroundPanel;
    private UITextPanel<string> chooseYourSpawnPanel;

    // UI components which are a part of backgroundPanel
    private UIRandomTeleportPanel randomPanel;
    private UIMyBedButton myBedPanel;
    private UIWorldSpawnPanel worldSpawnPanel;
    private readonly List<UISpawnCharacter> playerItems = []; // list of teammate player UI items

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
        Log.Chat("OnActivate() called");

        // UI state settings

        // Title
        chooseYourSpawnPanel = new(Language.GetTextValue("Mods.PvPAdventure.Spawn.ChooseYourSpawn"), 0.75f, true)
        {
            HAlign = 0.5f,
            BackgroundColor = new Color(73, 94, 171),
            Top = new StyleDimension(42, 0)
        };

        // Background panel
        backgroundPanel = new()
        {
            HAlign = 0.5f,
            Top = new StyleDimension(82, 0),
            BackgroundColor = new Color(33, 43, 79) * 1f
        };
        backgroundPanel.SetPadding(0f);
        Append(backgroundPanel);

        // Rebuild
        Rebuild();

        // Add title last, on top of everything else
        Append(chooseYourSpawnPanel);
    }

    public override void Update(GameTime gameTime)
    {
        if (IsMouseHovering)
        {
            //Main.LocalPlayer.mouseInterface = true; // disable item use when hovering
        }

#if DEBUG
        if (!Main.drawingPlayerChat)
        {
            if (Main.keyState.IsKeyDown(Keys.L) && !Main.oldKeyState.IsKeyDown(Keys.L))
            {
                s_debugExtraLocalCopies++;
                Log.Chat($"Extra copies: {s_debugExtraLocalCopies}. Use J or L to adjust.");
                Rebuild();
            }
            else if (Main.keyState.IsKeyDown(Keys.J) && !Main.oldKeyState.IsKeyDown(Keys.J))
            {
                if (s_debugExtraLocalCopies > 0)
                {
                    s_debugExtraLocalCopies--;
                    Log.Chat($"Extra copies: {s_debugExtraLocalCopies}. Use J or L to adjust.");
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
        //backgroundPanel.RemoveAllChildren();
        //RemoveChild(chooseYourSpawnPanel);
        playerItems.Clear();

        var players = new List<Player>();
        Player local = Main.LocalPlayer;

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
            for (int i = 0; i < s_debugExtraLocalCopies; i++)
                players.Add(local);
#endif

        int playerCount = players.Count;

        var density = UISpawnCharacter.GetDensityForTeammateCount(playerCount);
        float itemWidth = UISpawnCharacter.GetItemWidth(density);
        float itemHeight = UISpawnCharacter.ItemHeight;

        float worldWidth = itemHeight;
        float randomWidth = itemHeight;

        int gaps = 1;
        if (playerCount > 0)
        {
            gaps += playerCount - 1;
            gaps += 1;
        }

        float contentWidth =
            worldWidth + worldWidth +
            (playerCount * itemWidth) +
            randomWidth +
            (gaps * Spacing);

        float panelWidth = contentWidth + HorizontalPadding * 2f;
        float panelHeight = itemHeight + VerticalPadding * 2f;

        backgroundPanel.Width.Set(panelWidth, 0f);
        backgroundPanel.Height.Set(panelHeight, 0f);

        float x = HorizontalPadding;
        float y = VerticalPadding;

        // World spawn
        worldSpawnPanel = new UIWorldSpawnPanel(itemHeight);
        worldSpawnPanel.Left.Set(x, 0f);
        worldSpawnPanel.Top.Set(y, 0f);
        backgroundPanel.Append(worldSpawnPanel);

        x += worldWidth + Spacing;

        // My bed button
        bool hasSelfBed = Main.LocalPlayer.SpawnX != -1 && Main.LocalPlayer.SpawnY != -1;
        myBedPanel = new UIMyBedButton(itemHeight, hasSelfBed);
        myBedPanel.Left.Set(x, 0f);
        myBedPanel.Top.Set(y, 0f);
        backgroundPanel.Append(myBedPanel);
        x += worldWidth + Spacing;

        // Players
        for (int i = 0; i < playerCount; i++)
        {
            var row = new UISpawnCharacter(players[i].whoAmI, density);
            row.Left.Set(x, 0f);
            row.Top.Set(y, 0f);
            row.Activate();

            backgroundPanel.Append(row);
            playerItems.Add(row);

            x += itemWidth;
            if (i < playerCount - 1)
                x += Spacing;
        }

        if (playerCount > 0)
            x += Spacing;

        // Random
        randomPanel = new UIRandomTeleportPanel(itemHeight);
        randomPanel.Left.Set(x, 0f);
        randomPanel.Top.Set(y, 0f);
        backgroundPanel.Append(randomPanel);

        backgroundPanel.Recalculate();
        backgroundPanel.RecalculateChildren();
    }

    private bool NeedsRebuild()
    {
        if (backgroundPanel == null)
            return true;

        var dims = backgroundPanel.GetDimensions();
        if (dims.Width <= 1f || dims.Height <= 1f)
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
            for (int i = 0; i < s_debugExtraLocalCopies; i++)
                players.Add(local.whoAmI);
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
