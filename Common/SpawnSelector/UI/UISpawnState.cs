using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PvPAdventure.Core.Config;
using PvPAdventure.Core.Debug;
using System;
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

    private int playerSignature = int.MinValue;
    private bool bossOffsetApplied;

    // Debug
#if DEBUG
    private static int debugExtraPlayersLocalCopies;
#endif

    public override void OnActivate()
    {
        //Log.Chat("OnActivate() called");

        // Top or bottom. Default to top.
        int vAlign = 0;
        int top = 85;

        var config = ModContent.GetInstance<ClientConfig>();
        bossOffsetApplied = ShouldOffsetForBossBar();

        if (config.spawnSelectorPosition == ClientConfig.SpawnSelectorPosition.Bottom)
        {
            vAlign = 1;
            top = bossOffsetApplied ? -82 : -22;
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
                Top = new StyleDimension(top - 38, 0),
                VAlign = vAlign,
            };
            // Add title last, on top of everything else
            Append(chooseYourSpawnPanel);
        }
    }

    private void ApplyPosition()
    {
        int vAlign = 0;
        int top = 85;

        var config = ModContent.GetInstance<ClientConfig>();
        if (config.spawnSelectorPosition == ClientConfig.SpawnSelectorPosition.Bottom)
        {
            vAlign = 1;
            top = bossOffsetApplied ? -82 : -22;
        }

        backgroundPanel.Top.Set(top, 0f);
        backgroundPanel.VAlign = vAlign;

        if (chooseYourSpawnPanel != null)
        {
            if (config.spawnSelectorPosition == ClientConfig.SpawnSelectorPosition.Bottom)
                top -= 38;

            chooseYourSpawnPanel.Top.Set(top - 38, 0f);
            chooseYourSpawnPanel.VAlign = vAlign;
        }

        Recalculate();
    }

    private static bool ShouldOffsetForBossBar()
    {
        var config = ModContent.GetInstance<ClientConfig>();
        bool bossBarActive = IsBossBarActive();

        return config.spawnSelectorPosition == ClientConfig.SpawnSelectorPosition.Bottom && bossBarActive;
    }

    private static bool IsBossBarActive()
    {
        for (int i = 0; i < Main.npc.Length; i++)
        {
            NPC npc = Main.npc[i];

            if (npc.active && (npc.boss || npc.GetBossHeadTextureIndex() >= 0))
                return true;
        }

        return false;
    }

    public override void Update(GameTime gameTime)
    {
#if DEBUG
        if (!Main.drawingPlayerChat)
        {
            if (Main.keyState.IsKeyDown(Keys.NumPad1) && !Main.oldKeyState.IsKeyDown(Keys.NumPad1))
            {
                debugExtraPlayersLocalCopies++;
                Log.Chat($"Extra copies: {debugExtraPlayersLocalCopies}. Use Numpad1/2 to adjust.");
                playerSignature = int.MinValue;
            }
            else if (Main.keyState.IsKeyDown(Keys.NumPad2) && !Main.oldKeyState.IsKeyDown(Keys.NumPad2))
            {
                if (debugExtraPlayersLocalCopies > 0)
                {
                    debugExtraPlayersLocalCopies--;
                    Log.Chat($"Extra copies: {debugExtraPlayersLocalCopies}. Use Numpad1/2 to adjust.");
                }

                playerSignature = int.MinValue;
            }
        }
#endif

        bool nextBossOffset = ShouldOffsetForBossBar();
        if (nextBossOffset != bossOffsetApplied)
        {
            bossOffsetApplied = nextBossOffset;
            ApplyPosition();
            Rebuild();
        }

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

        // Add players
        List<Player> players = GetPlayers();
        playerSignature = GetPlayerSignature(players);

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
        float y = VerticalPadding + 2; // [EXTRA]!

        // World spawn
        var worldSpawnPanel = new UIWorldSpawnPanel(itemHeight);
        worldSpawnPanel.Left.Set(x, 0f);
        worldSpawnPanel.Top.Set(y, 0f);
        worldSpawnPanel.SetPadding(0f);
        backgroundPanel.Append(worldSpawnPanel);

        x += itemHeight + Spacing;

        // My bed button
        bool hasSelfBed = Main.LocalPlayer.SpawnX != -1 && Main.LocalPlayer.SpawnY != -1;
        var myBedPanel = new UIMyBedButton(itemHeight, hasSelfBed);
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

            x += itemWidth;
            if (i < playerCount - 1)
                x += Spacing;
        }

        if (playerCount > 0)
            x += Spacing;

        // Random
        var randomPanel = new UIRandomTeleportPanel(itemHeight);
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

        if (Main.GameUpdateCount % 30 != 0)
            return false;

        return GetCurrentPlayerSignature() != playerSignature;
    }

    private static List<Player> GetPlayers()
    {
        List<Player> players = [];
        Player local = Main.LocalPlayer;
        if (local?.active != true || local.team == 0)
            return players;

        for (int i = 0; i < Main.maxPlayers; i++)
            if (Main.player[i] is { active: true } p && p.whoAmI != local.whoAmI && p.team == local.team)
                players.Add(p);

#if DEBUG
        if (Main.netMode != NetmodeID.Server)
            for (int i = 0; i < debugExtraPlayersLocalCopies; i++)
                players.Add(local);
#endif

        return players;
    }

    private static int GetPlayerSignature(List<Player> players)
    {
        HashCode hash = new();
        hash.Add(Main.LocalPlayer?.team ?? -1);
        foreach (Player player in players)
            hash.Add(player.whoAmI);

        hash.Add(players.Count);
        return hash.ToHashCode();
    }

    private static int GetCurrentPlayerSignature()
    {
        HashCode hash = new();
        Player local = Main.LocalPlayer;
        hash.Add(local?.team ?? -1);

        int count = 0;
        if (local?.active == true && local.team != 0)
        {
            for (int i = 0; i < Main.maxPlayers; i++)
                if (Main.player[i] is { active: true } p && p.whoAmI != local.whoAmI && p.team == local.team)
                {
                    count++;
                    hash.Add(p.whoAmI);
                }
        }

#if DEBUG
        if (Main.netMode != NetmodeID.Server && local?.active == true)
            for (int i = 0; i < debugExtraPlayersLocalCopies; i++)
            {
                count++;
                hash.Add(local.whoAmI);
            }
#endif

        hash.Add(count);
        return hash.ToHashCode();
    }
}
