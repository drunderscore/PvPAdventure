using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PvPAdventure.Common.Visualization;
using PvPAdventure.Core.Config;
using PvPAdventure.Core.Utilities;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.TeammateSpectator;

internal sealed class TeammateSpectatorUIState : UIState
{
    private int shownPlayerListHash;
    private bool cardsVisible = true;

    public override void OnActivate()
    {
        base.OnActivate();
        Rebuild();
    }

    public override void Update(GameTime gameTime)
    {
        RebuildIfNeeded();
        base.Update(gameTime);
    }

    private void RebuildIfNeeded()
    {
#if DEBUG
        if (KeyboardHelper.Pressed(Keys.F5))
        {
            DebugLog.Chat("F5 pressed, rebuilding teammate spectator UI. Last player hash: " + shownPlayerListHash);
            Rebuild();
            return;
        }
#endif

        int hash = GetTeammatePlayerListHash();

        if (hash != shownPlayerListHash)
            Rebuild();
    }

    private void Rebuild()
    {
        shownPlayerListHash = GetTeammatePlayerListHash();

        RemoveAllChildren();

        ClientConfig clientConfig = ModContent.GetInstance<ClientConfig>();

        if (!clientConfig.ShowTeammatesToSpectate || !TeammateSpectatorUISystem.IsEnabled)
            return;

        Player localPlayer = Main.LocalPlayer;

        if (localPlayer is null || localPlayer.team == 0)
            return;

        List<Player> players = GetSpectatableTeammates(localPlayer);

        float scale = GetScale();
        float spacing = 4f * scale;
        float cardWidth = TeammateSpectatorUIPlayerCard.CardWidth * scale;
        float cardHeight = TeammateSpectatorUIPlayerCard.CardHeight * scale;
        float toggleSize = TeammateSpectatorUIVisibilityToggle.SlotSize * scale;

        float toggleLeft = GetToggleLeft(toggleSize, scale);
        float cardLeft = MathHelper.Max(0f, toggleLeft - spacing - cardWidth);
        float listHeight = cardsVisible && players.Count > 0 ? players.Count * cardHeight + (players.Count - 1) * spacing : toggleSize;
        float top = GetTop(listHeight, scale);

        TeammateSpectatorUIVisibilityToggle toggle = new(scale, () => cardsVisible, ToggleCardsVisible);
        toggle.Left.Set(toggleLeft, 0f);
        toggle.Top.Set(top, 0f);
        Append(toggle);

        if (!cardsVisible)
            return;

        for (int i = 0; i < players.Count; i++)
        {
            TeammateSpectatorUIPlayerCard card = new(players[i].whoAmI, scale);
            card.Left.Set(cardLeft, 0f);
            card.Top.Set(top + i * (cardHeight + spacing), 0f);
            Append(card);
        }
    }

    private int GetTeammatePlayerListHash()
    {
        HashCode hash = new();

        ClientConfig clientConfig = ModContent.GetInstance<ClientConfig>();
        Player localPlayer = Main.LocalPlayer;

        hash.Add(clientConfig.ShowTeammatesToSpectate);
        hash.Add(clientConfig.adventureUISize);
        hash.Add(TeammateSpectatorUISystem.IsEnabled);
        hash.Add(cardsVisible);
        hash.Add(Main.screenWidth);
        hash.Add(Main.screenHeight);
        hash.Add(Main.mH);
        hash.Add(localPlayer?.team ?? 0);

        if (localPlayer is null)
            return hash.ToHashCode();

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player player = Main.player[i];

            if (!IsSpectatableTeammate(player, localPlayer))
                continue;

            hash.Add(i);
            hash.Add(player.name);
            hash.Add(player.team);
            hash.Add(player.dead);
            hash.Add(player.ghost);
        }

        return hash.ToHashCode();
    }

    private void ToggleCardsVisible()
    {
        cardsVisible = !cardsVisible;
        Rebuild();
    }

    private static List<Player> GetSpectatableTeammates(Player localPlayer)
    {
        List<Player> players = [];

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player player = Main.player[i];

            if (IsSpectatableTeammate(player, localPlayer))
                players.Add(player);
        }

        return players;
    }

    private static bool IsSpectatableTeammate(Player player, Player localPlayer)
    {
        if (player is null || !player.active)
            return false;

        if (player.whoAmI == Main.myPlayer)
            return false;

        if (localPlayer.team == 0 || player.team != localPlayer.team)
            return false;

        return true;
    }

    private static float GetScale()
    {
        ClientConfig clientConfig = ModContent.GetInstance<ClientConfig>();

        return clientConfig.adventureUISize switch
        {
            ClientConfig.AdventureUISize.VerySmall => 0.7f,
            ClientConfig.AdventureUISize.Small => 0.8f,
            ClientConfig.AdventureUISize.Medium => 0.9f,
            ClientConfig.AdventureUISize.Big => 1.15f,
            _ => 1f
        };
    }

    private static float GetToggleLeft(float toggleSize, float scale)
    {
        float accessoryColumnX = Main.screenWidth - 64f - 28f;
        float leftmostAccessoryColumnX = accessoryColumnX - 44f;
        float gap = 10f * scale;

        return MathHelper.Max(0f, leftmostAccessoryColumnX - gap - toggleSize);
    }

    private static float GetTop(float totalHeight, float scale)
    {
        float preferredTop = 308f + Main.mH;
        if (!DisableTeamSelect.CanChangeTeams())
        {
            preferredTop -= 122f;
        }

        float margin = 20f * scale;
        float maxTop = Math.Max(margin, Main.screenHeight - margin - totalHeight);

        return MathHelper.Clamp(preferredTop, margin, maxTop);
    }
}