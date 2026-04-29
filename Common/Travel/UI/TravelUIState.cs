using Humanizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PvPAdventure.Core.Config;
using PvPAdventure.Core.Utilities;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using tModPorter;

namespace PvPAdventure.Common.Travel.UI;

internal class TravelUIState : UIState
{
    // Fields
    private int lastTargetHash;

    // UI compoenents
    public UIPanel backgroundPanel;
    private UITextPanel<string> chooseYourDestinationPanel;

    public override void OnInitialize()
    {
        backgroundPanel = new UIPanel();
        backgroundPanel.HAlign = 0.5f;
        backgroundPanel.VAlign = 1f;
        backgroundPanel.Top.Set(-82f, 0f);
        backgroundPanel.SetPadding(0f);
        backgroundPanel.BackgroundColor = new Color(33, 43, 79) * 1f;
        Append(backgroundPanel);
    }

    public override void OnActivate()
    {
        base.OnActivate();
        lastTargetHash = int.MinValue;
    }

    public void ForceRebuildNextUpdate()
    {
        lastTargetHash = int.MinValue;
        Log.Chat("Travel UI hash changed, force rebuilding");
    }

    public void RebuildIfNeeded()
    {
        Player player = Main.LocalPlayer;

        if (player?.active != true)
            return;

        List<TravelTarget> targets = TravelTeleportSystem.GetTargets(player);
        int hash = GetTargetHash(targets);

        if (hash == lastTargetHash)
            return;

        lastTargetHash = hash;
        Rebuild(targets);
    }

    private void Rebuild(List<TravelTarget> targets)
    {
        // Clear existing UI elements
        backgroundPanel.RemoveAllChildren();
        chooseYourDestinationPanel?.Remove();

        Player local = Main.LocalPlayer;

        ClientConfig clientConfig = ModContent.GetInstance<ClientConfig>();
        ServerConfig serverConfig = ModContent.GetInstance<ServerConfig>();

        // Set scale based on config
        float scale = clientConfig.travelUISize switch
        {
            ClientConfig.TravelUISize.VerySmall => 0.8f,
            ClientConfig.TravelUISize.Small => 0.9f,
            ClientConfig.TravelUISize.Medium => 1.0f,
            ClientConfig.TravelUISize.Big => 1.15f,
            _ => 1f
        };

        List<Player> players = [];

        // Add local player first
        if (local?.active == true)
            players.Add(local);

        // Add all other players
        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player p = Main.player[i];

            if (p?.active == true && p.whoAmI != local.whoAmI && local.team > 0 && p.team == local.team)
                players.Add(p);
        }

        // Add debug players
#if DEBUG
        for (int i = 0; i < debugPlayers; i++)
            players.Add(local);
#endif

        // Set up layout
        float unitWidth = 115f * scale;
        float fullHeight = 76f * scale;
        float innerSpacing = 0f * scale;
        float spacing = 12f * scale;
        float paddingX = 8f * scale;
        float paddingY = 12f * scale;
        bool bottom = clientConfig.travelUIPosition == ClientConfig.TravelUIPosition.Bottom;
        
        int worldUnits = serverConfig.IsWorldSpawnTeleportEnabled ? 1 : 0;
        int randomUnits = serverConfig.IsRandomTeleportEnabled ? 1 : 0;
        int playerCards = serverConfig.IsTeammateSpawnTeleportEnabled ? players.Count : 0;
        int elementCount = worldUnits + randomUnits + playerCards;
        float playerCardWidth = unitWidth * 2f + innerSpacing;
        float contentWidth = unitWidth * (worldUnits + randomUnits) + playerCardWidth * playerCards + spacing * Math.Max(0, elementCount - 1);

        backgroundPanel.Top.Set(bottom ? -116f * scale : 82f * scale, 0f);
        backgroundPanel.HAlign = 0.5f;
        backgroundPanel.VAlign = bottom ? 1f : 0f;
        backgroundPanel.Width.Set(paddingX * 2f + contentWidth, 0f);
        backgroundPanel.Height.Set(paddingY * 2f + fullHeight, 0f);
        backgroundPanel.SetPadding(0f);

        float x = paddingX;

        // Add world button
        if (serverConfig.IsWorldSpawnTeleportEnabled)
        {
            TravelTarget worldTarget = FindTarget(targets, TravelType.World, -1, new TravelTarget(TravelType.World, -1, Vector2.Zero, "World Spawn", "World", true));
            UITravelIconButton world = new(worldTarget, () => TextureAssets.SpawnPoint.Value, Language.GetTextValue("Mods.PvPAdventure.Travel.TeleportToWorldSpawn"), unitWidth, fullHeight, null, 0.9f);
            world.Left.Set(x, 0f);
            world.Top.Set(paddingY, 0f);
            backgroundPanel.Append(world);
            x += unitWidth + spacing;
        }

        // Add player cards for teammates
        if (serverConfig.IsTeammateSpawnTeleportEnabled)
        {
            foreach (Player player in players)
            {
                string bedReason = player.whoAmI == local.whoAmI ? "You have no bed set" : $"{player.name} has no bed set";
                string portalReason = player.whoAmI == local.whoAmI ? "You have no portal" : $"{player.name} has no portal";

                TravelTarget bed = FindTarget(targets, TravelType.Bed, player.whoAmI, new TravelTarget(TravelType.Bed, player.whoAmI, Vector2.Zero, $"{player.name}'s Bed", "Bed", false, bedReason));
                TravelTarget portal = FindTarget(targets, TravelType.Portal, player.whoAmI, new TravelTarget(TravelType.Portal, player.whoAmI, Vector2.Zero, $"{player.name}'s Portal", "Portal", false, portalReason));

                UITravelPlayerCard button = new(player, bed, portal, unitWidth, fullHeight, innerSpacing);
                button.Left.Set(x, 0f);
                button.Top.Set(paddingY, 0f);
                backgroundPanel.Append(button);

                x += playerCardWidth + spacing;
            }
        }

        // Add random button
        if (serverConfig.IsRandomTeleportEnabled)
        {
            TravelTarget randomTarget = FindTarget(targets, TravelType.Random, -1, new TravelTarget(TravelType.Random, -1, Vector2.Zero, "Random", "Random", true));
            UITravelIconButton random = new(randomTarget, () => Ass.Icon_Question_Mark.Value, Language.GetTextValue("Mods.PvPAdventure.Travel.Random"), unitWidth, fullHeight, forcedMapBgIndex: 7, iconScaleMultiplier: 1.1f);
            random.Left.Set(x, 0f);
            random.Top.Set(paddingY, 0f);
            backgroundPanel.Append(random);
        }

        backgroundPanel.Recalculate();
        backgroundPanel.RecalculateChildren();

        chooseYourDestinationPanel = new(Language.GetTextValue("Mods.PvPAdventure.Travel.ChooseYourDestination"), 0.8f * scale, true)
        {
            HAlign = 0.5f,
            VAlign = backgroundPanel.VAlign,
            Width = { Pixels = 300f * scale },
            Height = { Pixels = 45 * scale },
            Top = new StyleDimension(backgroundPanel.Top.Pixels - 44f * scale, 0f),
            BackgroundColor = new Color(73, 94, 171),
            //BackgroundColor = new Color(33, 43, 79) * 1f,
        };
        chooseYourDestinationPanel.SetPadding(12f * scale);
        Append(chooseYourDestinationPanel);

        //Log.Chat($"Travel UI rebuilt with {players.Count} players");
    }

    private static TravelTarget FindTarget(List<TravelTarget> targets, TravelType type, int playerIndex, TravelTarget fallback)
    {
        foreach (TravelTarget target in targets)
        {
            if (target.Type == type && target.PlayerIndex == playerIndex)
                return target;
        }

        return fallback;
    }

    /// <summary>
    /// Gets a hash code for the list of travel targets, used to determine if the UI needs to be rebuilt.
    /// If any of the properties of the travel targets change, the hash code will change and the UI will be rebuilt.
    /// </summary>
    /// <param name="targets">The list of travel targets.</param>
    /// <returns>A hash code representing the current state of the travel targets.</returns>
    private static int GetTargetHash(List<TravelTarget> targets)
    {
        HashCode hash = new();

        foreach (TravelTarget target in targets)
        {
            hash.Add(target.Type);
            hash.Add(target.PlayerIndex);
            hash.Add(target.Name);
            hash.Add(target.Available);
            hash.Add((int)target.WorldPosition.X);
            hash.Add((int)target.WorldPosition.Y);
        }

#if DEBUG
        hash.Add(debugPlayers);
#endif

        return hash.ToHashCode();
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);
        DrawPortalCreatorTimer(spriteBatch);
    }

    private void DrawPortalCreatorTimer(SpriteBatch sb)
    {
        Player local = Main.LocalPlayer;
        int secondsLeft = TravelTeleportSystem.PortalCreatorSecondsLeft(local);

        if (secondsLeft <= 0 || chooseYourDestinationPanel == null)
            return;

        CalculatedStyle dims = chooseYourDestinationPanel.GetDimensions();
        string text = secondsLeft.ToString();

        Vector2 size = FontAssets.DeathText.Value.MeasureString(text);
        Vector2 pos = new(
            dims.X + dims.Width + 12f,
            dims.Y + (dims.Height - size.Y) * 0.5f + 8f
        );

        Utils.DrawBorderStringBig(sb, text, pos, Color.White);
    }


    #region Debug
#if DEBUG
    private static int debugPlayers; // used for UI testing
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (Main.keyState.IsKeyDown(Keys.NumPad1) && !Main.oldKeyState.IsKeyDown(Keys.NumPad1))
        {
            debugPlayers++;
            Log.Chat($"Extra copies: {debugPlayers}. Use Numpad1/2 to adjust.");
        }
        else if (Main.keyState.IsKeyDown(Keys.NumPad2) && !Main.oldKeyState.IsKeyDown(Keys.NumPad2))
        {
            if (debugPlayers > 0)
            {
                debugPlayers--;
                Log.Chat($"Extra copies: {debugPlayers}. Use Numpad1/2 to adjust.");
            }
        }

        if (Main.keyState.IsKeyDown(Keys.F5) && !Main.oldKeyState.IsKeyDown(Keys.F5))
        {
            ForceRebuildNextUpdate();
        }
    }
#endif
    #endregion

}
