using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PvPAdventure.Core.Config;
using PvPAdventure.Core.Debug;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.UI;

namespace PvPAdventure.Common.SpawnSelector.UI;

/// <summary>
/// The main state containing all elements for the spawn and spectate UI.
/// Contains the following UI:
/// <see cref="UIWorldSpawnPanel"/>
/// <see cref="UITeammatePanel"/> (which contains: <seealso cref="UITeammateBedButton"/>)
/// <see cref="UIRandomTeleportPanel"/>
/// </summary>
public class UISpawnState : UIState
{
    // UI components
    public UIPanel backgroundPanel;
    private UITextPanel<string> chooseYourSpawnPanel;
    public UITextPanel<string> TitlePanel => chooseYourSpawnPanel;

    // UI components which are a part of backgroundPanel
    private UIRandomTeleportPanel randomPanel;
    private UIMyBedButton myBedPanel;
    private UIWorldSpawnPanel worldSpawnPanel;
    private readonly List<UITeammatePanel> playerItems = []; // list of teammate player UI items

    // Rebuild frequently
    private bool _forceRebuild;
    public void RequestRebuild() => _forceRebuild = true;

    // Debug
#if DEBUG
    private static int s_debugExtraLocalCopies;
#endif

    public override void OnActivate()
    {
        //Log.Chat("OnActivate() called");

        // Top or bottom. Default to top.
        int vAlign = 0;
        int top = 85;

        var config = ModContent.GetInstance<ClientConfig>();
        if (config.spawnSelectorPosition == ClientConfig.SpawnSelectorPosition.Bottom)
        {
            vAlign = 1;
            top = -22;
        }

        // Background panel
        backgroundPanel = new()
        {
            Top = new StyleDimension(top, 0),
            VAlign = vAlign,
            HAlign = 0.5f,
            BackgroundColor = new Color(33, 43, 79) * 1f,
        };
        backgroundPanel.SetPadding(0f);
        Append(backgroundPanel);

        // Rebuild
        Rebuild();

        // Title
        if (config.ShowChooseYourSpawnText)
        {
            if (config.spawnSelectorPosition == ClientConfig.SpawnSelectorPosition.Bottom)
            {
                top -= 38;
            }

            chooseYourSpawnPanel = new(Language.GetTextValue("Mods.PvPAdventure.Spawn.ChooseYourSpawn"), 0.7f, true)
            {
                HAlign = 0.5f,
                BackgroundColor = new Color(73, 94, 171),
                Top = new StyleDimension(top-38, 0),
                VAlign = vAlign,
            };
            // Add title last, on top of everything else
            Append(chooseYourSpawnPanel);
        }
    }

    public override void Update(GameTime gameTime)
    {
#if DEBUG
        if (!Main.drawingPlayerChat)
        {
            if (Main.keyState.IsKeyDown(Keys.NumPad1) && !Main.oldKeyState.IsKeyDown(Keys.NumPad1))
            {
                s_debugExtraLocalCopies++;
                Log.Chat($"Extra copies: {s_debugExtraLocalCopies}. Use Numpad1/2 to adjust.");
                Rebuild();
            }
            else if (Main.keyState.IsKeyDown(Keys.NumPad2) && !Main.oldKeyState.IsKeyDown(Keys.NumPad2))
            {
                if (s_debugExtraLocalCopies > 0)
                {
                    s_debugExtraLocalCopies--;
                    Log.Chat($"Extra copies: {s_debugExtraLocalCopies}. Use Numpad1/2 to adjust.");
                }

                Rebuild();
            }
        }
#endif

        if (NeedsRebuild())
        {
            Rebuild();
            //Log.Chat("Called Rebuild() in SpawnAndSpectateBasePanel");
        }

        base.Update(gameTime);
    }

    private void Rebuild()
    {
        // Clear
        backgroundPanel.RemoveAllChildren();
        playerItems.Clear();

        // Add players
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

        var density = UITeammatePanel.GetDensityForTeammateCount(playerCount);
        float itemWidth = UITeammatePanel.GetItemWidth(density);

        // Dimensions
        float itemHeight = 64;
        const float Spacing = 6f;
        const float HorizontalPadding = 8f; // panel padding
        const float VerticalPadding = 10f; // panel padding

        int gaps = 1;
        if (playerCount > 0)
        {
            gaps += playerCount - 1;
            gaps += 1;
        }

        float contentWidth =
            itemHeight +           // world spawn
            itemHeight +           // my bed
            itemHeight +           // my portal
            (playerCount * itemWidth) + // players
            itemHeight +           // random
            (gaps * Spacing);

        float panelWidth = contentWidth + HorizontalPadding * 2f + HorizontalPadding;
        float panelHeight = itemHeight + VerticalPadding * 2f;

        backgroundPanel.Width.Set(panelWidth, 0f);
        backgroundPanel.Height.Set(panelHeight, 0f);

        float x = HorizontalPadding;
        float y = VerticalPadding + 2 ; // [EXTRA]!

        // World spawn
        worldSpawnPanel = new UIWorldSpawnPanel(itemHeight);
        worldSpawnPanel.Left.Set(x, 0f);
        worldSpawnPanel.Top.Set(y, 0f);
        worldSpawnPanel.SetPadding(0f);
        backgroundPanel.Append(worldSpawnPanel);

        x += itemHeight + Spacing;

        // My bed button
        bool hasSelfBed = Main.LocalPlayer.SpawnX != -1 && Main.LocalPlayer.SpawnY != -1;
        myBedPanel = new UIMyBedButton(itemHeight, hasSelfBed);
        myBedPanel.Left.Set(x, 0f);
        myBedPanel.Top.Set(y, 0f);
        backgroundPanel.Append(myBedPanel);
        x += itemHeight + Spacing;

        // My portal button
        bool hasPortal = PortalSystem.HasPortal(Main.LocalPlayer);
        var myPortalPanel = new UIMyPortalButton(itemHeight, hasPortal);
        myPortalPanel.Left.Set(x, 0f);
        myPortalPanel.Top.Set(y, 0f);
        backgroundPanel.Append(myPortalPanel);
        x += itemHeight + Spacing;

        // Players
        for (int i = 0; i < playerCount; i++)
        {
            var row = new UITeammatePanel(players[i].whoAmI, density);
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
        if (_forceRebuild)
        {
            _forceRebuild = false;
            return true;
        }

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
