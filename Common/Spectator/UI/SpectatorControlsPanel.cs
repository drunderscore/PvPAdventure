using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PvPAdventure.Common.Spectator.SpectatorMode;
using PvPAdventure.Common.Travel;
using PvPAdventure.Core.Config;
using PvPAdventure.Core.Utilities;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace PvPAdventure.Common.Spectator.UI;

/// <summary>
/// Main spectator HUD in bottom center of the screen. 
/// Displays a horizontal list of spectatable players and allows the user to hover and lock onto a target.
/// </summary>
internal sealed class SpectatorControlsPanel : UIPanel
{
    private const int MinShownPlayerCards = 1;
    private const int MaxShownPlayerCards = 6;
    private static int requestedShownPlayerCards = 1;
    private static bool userChangedShownPlayerCards;
    private static int cardCountRevision;
    private static int lastShownPlayerCards = 1;

    private int shownPlayerCards = 1; // number of player cards shown in the panel
    private int visibleTargetStart; // first target index shown in the player card window
    private int observedCardCountRevision;

    private int locked = -1; // currently locked spectated player index, -1 means no locked target
    private int lockedNpc = -1; // currently locked spectated NPC index, -1 means no locked NPC target
    private int hovered = -1; // currently hovered spectated player index, -1 means no hovered target
    private bool lastAutoDirectorEnabled;
    private UIText statusText; // UI element for displaying the current status like "Spectating: PlayerName" or "Free camera" or "Auto-director"

    public SpectatorControlsPanel()
    {
        Width.Set(GetPanelWidth(), 0f);
        Height.Set(GetPanelHeight(), 0f);
        HAlign = 0.5f;
        //VAlign = 1f;
        //Top.Set(-25, 0f); // bottom padding
        ApplyTopOrBottomPosition(this, GetScale());
        //BackgroundColor = new Color(33, 43, 79) * 0.3f;
        BackgroundColor = new Color(73, 94, 171)*0.3f;
        Rebuild();
    }

    public void Rebuild()
    {
        RemoveAllChildren();

        // Universal scale
        float scale = GetScale();
        //Log.Chat("scale: " + scale);

        SetPadding(10f*scale);
        Width.Set(GetPanelWidth(), 0f);
        Height.Set(GetPanelHeight(), 0f);
        //Top.Set(-25f * GetScale(), 0f); // bottom padding
        ApplyTopOrBottomPosition(this, GetScale());

        //BackgroundColor = new Color(73, 94, 171)*0.8f;
        BackgroundColor = new Color(22, 28, 48) * 0.85f;
        BorderColor = Color.Black;

        // Target filtering
        List<int> targets = SpectatorTargetSystem.GetTargets(Main.myPlayer);

        for (int i = targets.Count - 1; i >= 0; i--)
        {
            int playerIndex = targets[i];

            if (playerIndex < 0 || playerIndex >= Main.maxPlayers || Main.player[playerIndex] is null || !Main.player[playerIndex].active)
                targets.RemoveAt(i);
        }

        shownPlayerListHash = GetActivePlayerListHash();

        if (!targets.Contains(locked))
            locked = -1;

        if (!targets.Contains(hovered))
            hovered = -1;

        shownPlayerCards = GetShownPlayerCardsForTargetCount(targets.Count);
        lastShownPlayerCards = shownPlayerCards;
        requestedShownPlayerCards = shownPlayerCards;
        Width.Set(GetPanelWidth(), 0f);

        // Update the number of visible targets based on the number of player cards to show
        visibleTargetStart = Math.Clamp(visibleTargetStart, 0, Math.Max(0, targets.Count - shownPlayerCards));
        int visibleTargets = Math.Min(targets.Count - visibleTargetStart, shownPlayerCards);

        // Layout
        float topRowHeight = GetNamePanelHeight(scale);
        float topRowPadding = 4f * scale;
        float rowGap = 4f * scale;
        float playerPanelPadding = 4f * scale;
        float navButtonWidth = 24f * scale;
        float navButtonGap = 8f * scale;
        float cardGap = 6f * scale;
        float cardWidth = UIPlayerCard.CardWidth * scale;
        float cardHeight = UIPlayerCard.CardHeight * scale;
        float shownCardsWidth = shownPlayerCards * cardWidth + Math.Max(0, shownPlayerCards - 1) * cardGap;
        float visibleCardsWidth = visibleTargets * cardWidth + Math.Max(0, visibleTargets - 1) * cardGap;
        float cardsStart = navButtonWidth + navButtonGap + Math.Max(0f, shownCardsWidth - visibleCardsWidth) * 0.5f;

        // Add top row panel
        UIPanel topRow = new();
        topRow.SetPadding(topRowPadding);
        topRow.Width.Set(0f, 1f);
        topRow.Height.Set(topRowHeight, 0f);
        topRow.BackgroundColor = new Color(35, 54, 96) * 0.85f;
        topRow.BorderColor = Color.Black;
        Append(topRow);

        // Update status text
        statusText = new UIText("") { HAlign = 0.5f, VAlign = 0.5f };
        topRow.Append(statusText);
        UpdateStatusText();

        // Add player panel
        UIPanel playersPanel = new();
        playersPanel.SetPadding(playerPanelPadding);
        playersPanel.Top.Set(topRowHeight + rowGap, 0f);
        playersPanel.Width.Set(0f, 1f);
        playersPanel.Height.Set(cardHeight + playerPanelPadding * 2f, 0f);
        playersPanel.BackgroundColor = UICommon.DefaultUIBlueMouseOver * 0.3f;
        playersPanel.BorderColor = Color.Black;
        Append(playersPanel);

        if (targets.Count == 0)
        {
            UIText noPlayersText = new("No players are available to spectate.", 0.9f)
            {
                HAlign = 0.5f,
                VAlign = 0.5f,
                TextColor = Color.LightGray
            };

            playersPanel.Append(noPlayersText);
            Recalculate();
            return;
        }

        // Add previous button
        AddPrevButton(playersPanel, playerPanelPadding, navButtonWidth, cardHeight, scale);

        // Add player cards
        for (int i = 0; i < visibleTargets; i++)
        {
            int targetIndex = visibleTargetStart + i;
            int playerIndex = targets[targetIndex];

            UIPlayerCard playerCard = new(playerIndex, i, this, scale);
            playerCard.Width.Set(cardWidth, 0f);
            playerCard.Height.Set(cardHeight, 0f);
            playerCard.Left.Set(cardsStart + i * (cardWidth + cardGap), 0f);
            playerCard.OnLeftClick += (evt, element) =>
            {
                if (evt.Target != playerCard)
                    return;

                SpectatorTargetSystem.TogglePlayerTarget(playerIndex);
                UpdateTarget();
                UpdateStatusText();
            }; 
            playersPanel.Append(playerCard);
        }

        // Add next button
        AddNextButton(playersPanel, playerPanelPadding, navButtonWidth, cardHeight, scale);

        Recalculate();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

#if DEBUG
        UpdateDebugPlayers();
#endif

        RebuildIfNeeded();
        RebuildIfCardCountChanged();
        HandleTargetNavigationKeys(gameTime);

        int nextHover = GetHoveredSlot();

        if (nextHover != hovered)
        {
            if (hovered >= 0)
                EndHover();

            if (nextHover >= 0)
                BeginHover(nextHover);
        }

        if (IsMouseHovering)
            Main.LocalPlayer.mouseInterface = true;

        if (lastAutoDirectorEnabled != AutoDirectorSystem.Enabled)
        {
            lastAutoDirectorEnabled = AutoDirectorSystem.Enabled;
            UpdateStatusText();
        }
    }

    public void UpdateTarget()
    {
        int oldLocked = locked;
        int oldLockedNpc = lockedNpc;

        Player target = SpectatorTargetSystem.GetLockedPlayerTarget();
        locked = target?.active == true ? target.whoAmI : -1;

        NPC npcTarget = SpectatorTargetSystem.GetLockedNPCTarget();
        lockedNpc = npcTarget?.active == true ? npcTarget.whoAmI : -1;

        if (locked != oldLocked || lockedNpc != oldLockedNpc)
            UpdateStatusText();
    }

    private int GetHoveredSlot()
    {
        if (!ContainsPoint(Main.MouseScreen))
            return -1;

        for (int i = 0; i < Elements.Count; i++)
        {
            int playerIndex = GetHoveredSlot(Elements[i]);

            if (playerIndex >= 0)
                return playerIndex;
        }

        return -1;
    }

    private static int GetHoveredSlot(UIElement element)
    {
        if (element is UIPlayerCard slot && slot.ContainsPoint(Main.MouseScreen))
            return slot.PlayerIndex;

        for (int i = 0; i < element.Elements.Count; i++)
        {
            int playerIndex = GetHoveredSlot(element.Elements[i]);

            if (playerIndex >= 0)
                return playerIndex;
        }

        return -1;
    }

    private void BeginHover(int playerIndex)
    {
        if (!IsTargetValid(playerIndex))
            return;

        hovered = playerIndex;
        SpectatorTargetSystem.SetPreviewTarget(playerIndex);
        UpdateStatusText();
    }

    private void EndHover()
    {
        hovered = -1;
        SpectatorTargetSystem.ClearPreviewTarget();
        UpdateStatusText();
    }

    private string GetStatusText()
    {
        if (AutoDirectorSystem.Enabled)
            return "Auto director On";

        if (hovered >= 0 && Main.player[hovered]?.active == true)
        {
            if (locked == hovered)
                return $"Following {Main.player[hovered].name}. Click to stop following";

            return $"Previewing {Main.player[hovered].name}. Click to follow";
        }

        if (locked >= 0 && Main.player[locked]?.active == true)
            return $"Following {Main.player[locked].name}";

        if (lockedNpc >= 0 && Main.npc[lockedNpc]?.active == true)
            return $"Following \"{Main.npc[lockedNpc].FullName}\"";

        return "You are in ghost mode";
    }

    private void UpdateStatusText()
    {
        statusText?.SetText(GetStatusText());
    }

    internal void SetStatusText(string text)
    {
        statusText?.SetText(text);
    }

    internal void ResetStatusText()
    {
        UpdateStatusText();
    }

    private static bool IsTargetValid(int playerIndex)
    {
        return playerIndex >= 0 && SpectatorTargetSystem.GetTargets(Main.myPlayer).Contains(playerIndex);
    }

    public static int ShownPlayerCardCount => lastShownPlayerCards;

    public static void ChangeShownPlayerCards(int direction)
    {
        int targetCount = SpectatorTargetSystem.GetTargets(Main.myPlayer).Count;
        int maxShownPlayerCards = Math.Min(MaxShownPlayerCards, targetCount);
        int next = lastShownPlayerCards + direction;

        if (next < MinShownPlayerCards)
        {
            Main.NewText("Minimum players are already showing.", Color.Yellow);
            return;
        }

        if (next > maxShownPlayerCards)
        {
            Main.NewText("Maximum players are already showing.", Color.Yellow);
            return;
        }

        userChangedShownPlayerCards = true;
        requestedShownPlayerCards = next;
        cardCountRevision++;
    }

    #region Target navigation
    private Keys? heldNavigationKey;
    private double navigationRepeatTimer;

    private const double NavigationInitialRepeatDelay = 0.35;
    private const double NavigationRepeatInterval = 0.06;
    private void HandleTargetNavigationKeys(GameTime gameTime)
    {
        int direction = 0;

        if (JustPressed(Keys.Left))
            direction = -1;

        if (JustPressed(Keys.Right))
            direction = 1;

        if (direction != 0)
        {
            NavigateTarget(direction);
            heldNavigationKey = direction < 0 ? Keys.Left : Keys.Right;
            navigationRepeatTimer = NavigationInitialRepeatDelay;
            return;
        }

        if (heldNavigationKey is not Keys heldKey || !Main.keyState.IsKeyDown(heldKey))
        {
            heldNavigationKey = null;
            return;
        }

        navigationRepeatTimer -= gameTime.ElapsedGameTime.TotalSeconds;

        if (navigationRepeatTimer > 0)
            return;

        navigationRepeatTimer += NavigationRepeatInterval;
        NavigateTarget(heldKey == Keys.Left ? -1 : 1);
    }

    private void NavigateTarget(int direction)
    {
        List<int> targets = SpectatorTargetSystem.GetTargets(Main.myPlayer);

        if (targets.Count == 0)
        {
            SpectatorTargetSystem.ClearTarget();
            UpdateTarget();
            UpdateStatusText();
            return;
        }

        int oldVisibleTargetStart = visibleTargetStart;
        int currentIndex = targets.IndexOf(locked);
        int nextIndex = currentIndex < 0 ? direction < 0 ? targets.Count - 1 : 0 : currentIndex + direction;

        if (nextIndex < 0)
            nextIndex = targets.Count - 1;

        if (nextIndex >= targets.Count)
            nextIndex = 0;

        int playerIndex = targets[nextIndex];

        SpectatorTargetSystem.SetPlayerTarget(playerIndex);
        locked = playerIndex;
        lockedNpc = -1;

        MakeTargetVisible(nextIndex, targets.Count);

        if (visibleTargetStart != oldVisibleTargetStart)
            Rebuild();
        else
            UpdateStatusText();
    }

    private void MakeTargetVisible(int targetIndex, int targetCount)
    {
        int maxVisibleTargetStart = Math.Max(0, targetCount - shownPlayerCards);

        visibleTargetStart = Math.Clamp(visibleTargetStart, 0, maxVisibleTargetStart);

        if (targetIndex < visibleTargetStart)
            visibleTargetStart = targetIndex;
        else if (targetIndex >= visibleTargetStart + shownPlayerCards)
            visibleTargetStart = targetIndex - shownPlayerCards + 1;

        visibleTargetStart = Math.Clamp(visibleTargetStart, 0, maxVisibleTargetStart);
    }

    private static bool JustPressed(Keys key)
    {
        return Main.keyState.IsKeyDown(key) && !Main.oldKeyState.IsKeyDown(key);
    }
    #endregion

    #region Rebuild if player list changes
    public override void OnActivate()
    {
        base.OnActivate();
        Rebuild();
    }
    private int shownPlayerListHash; // cached active Main.player list hash to detect joins/leaves
    
    private void RebuildIfNeeded()
    {
#if DEBUG
        if (KeyboardHelper.Pressed(Keys.F5))
        {
            Log.Chat("F5 pressed, rebuilding. Last player hash: " + shownPlayerListHash);
            Rebuild();
        }
#endif

            // Update player cache if the list of targets has changed
            // Update player cache if the Main.player list has changed
            int playerListHash = GetActivePlayerListHash();

        if (playerListHash != shownPlayerListHash)
            Rebuild();
    }

    private void RebuildIfCardCountChanged()
    {
        if (observedCardCountRevision == cardCountRevision)
            return;

        observedCardCountRevision = cardCountRevision;
        Rebuild();
    }
    private static int GetActivePlayerListHash()
    {
        HashCode hash = new();

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player player = Main.player[i];

            if (player is null || !player.active)
                continue;

            hash.Add(i);
            //hash.Add(player.whoAmI);
            //hash.Add(player.name);
        }

        return hash.ToHashCode();
    }
    #endregion

    #region Layout Helpers
    private static void ApplyTopOrBottomPosition(UIElement element, float scale)
    {
        ClientConfig clientConfig = ModContent.GetInstance<ClientConfig>();

        if (clientConfig.spectateUIPosition == ClientConfig.AdventureUIPosition.Top)
        {
            element.VAlign = 0f;
            element.Top.Set(40f * scale, 0f);
            return;
        }

        element.VAlign = 1f;
        element.Top.Set(-25f * scale, 0f);
    }

    private static float GetScale()
    {
        ClientConfig clientConfig = ModContent.GetInstance<ClientConfig>();

        float scale = clientConfig.spectateUISize switch
        {
            ClientConfig.AdventureUISize.VerySmall => 0.7f,
            ClientConfig.AdventureUISize.Small => 0.8f,
            ClientConfig.AdventureUISize.Medium => 0.9f,
            ClientConfig.AdventureUISize.Big => 1.15f,
            _ => 1f
        };

        return scale;
    }

    private static float GetNamePanelHeight(float scale)
    {
        return 38f * scale;
    }

    private static int GetShownPlayerCardsForTargetCount(int targetCount)
    {
        if (targetCount <= 0)
            return MinShownPlayerCards;

        int maxShownPlayerCards = Math.Min(MaxShownPlayerCards, targetCount);
        int desired = userChangedShownPlayerCards ? requestedShownPlayerCards : targetCount;

        return Math.Clamp(desired, MinShownPlayerCards, maxShownPlayerCards);
    }

    private static int GetPanelHeight()
    {
        float scale = GetScale();

        float outerPadding = 10f * scale;
        float topRowHeight = GetNamePanelHeight(scale);
        float rowGap = 4f * scale;
        float playerPanelPadding = 4f * scale;
        float cardHeight = UIPlayerCard.CardHeight * scale;

        return (int)(outerPadding * 2f + topRowHeight + rowGap + cardHeight + playerPanelPadding * 2f);
    }

    private int GetPanelWidth()
    {
        float scale = GetScale();
        float outerPadding = 10f * scale;
        float playerPanelPadding = 4f * scale;
        float navButtonWidth = 24f * scale;
        float navButtonGap = 8f * scale;
        float cardGap = 6f * scale;
        float cardWidth = UIPlayerCard.CardWidth * scale;

        return (int)(outerPadding * 2f + playerPanelPadding * 2f + shownPlayerCards * cardWidth + Math.Max(0, shownPlayerCards - 1) * cardGap + navButtonWidth * 2f + navButtonGap * 2f);
    }

    private void AddPrevButton(UIPanel playersPanel, float playerPanelPadding, float navButtonWidth, float cardHeight, float scale)
    {
        UIAutoScaleTextTextPanel<string> prevButton = new("<");
        prevButton.SetPadding(0f);
        prevButton.Top.Set(playerPanelPadding + cardHeight * 0.5f - 15f * scale, 0f);
        prevButton.Height.Set(30f * scale, 0f); prevButton.Width.Set(navButtonWidth, 0f);
        prevButton.BackgroundColor = new Color(55, 48, 92) * 0.9f;
        prevButton.BorderColor = Color.Black;
        prevButton.OnLeftClick += (evt, element) => NavigateTarget(-1);
        prevButton.OnMouseOver += (evt, element) =>
        {
            prevButton.BorderColor = Color.Yellow;
            statusText?.SetText("Go to previous player");
        };
        prevButton.OnMouseOut += (evt, element) =>
        {
            prevButton.BorderColor = Color.Black;
            UpdateStatusText();
        };

        playersPanel.Append(prevButton);
    }

    private void AddNextButton(UIPanel playersPanel, float playerPanelPadding, float navButtonWidth, float cardHeight, float scale)
    {
        UIAutoScaleTextTextPanel<string> nextButton = new(">");
        nextButton.SetPadding(0f);
        nextButton.HAlign = 1f;
        nextButton.Top.Set(playerPanelPadding + cardHeight * 0.5f - 15f * scale, 0f);
        nextButton.Height.Set(30f * scale, 0f);
        nextButton.Width.Set(navButtonWidth, 0f);
        nextButton.BackgroundColor = new Color(55, 48, 92) * 0.9f;
        nextButton.BorderColor = Color.Black;
        nextButton.OnLeftClick += (evt, element) => NavigateTarget(1);
        nextButton.OnMouseOver += (evt, element) =>
        {
            nextButton.BorderColor = Color.Yellow;
            statusText?.SetText("Go to next player");
        };
        nextButton.OnMouseOut += (evt, element) =>
        {
            nextButton.BorderColor = Color.Black;
            UpdateStatusText();
        };

        playersPanel.Append(nextButton);
    }

    private void AddMinusButton(UIPanel topRow, float topRowHeight)
    {
        UIAutoScaleTextTextPanel<string> minusButton = new("-");
        minusButton.SetPadding(0f);
        minusButton.HAlign = 1f;
        minusButton.Left.Set(-topRowHeight, 0f);
        minusButton.Width.Set(topRowHeight, 0f);
        minusButton.Height.Set(topRowHeight, 0f);
        minusButton.BackgroundColor = new Color(92, 45, 45) * 0.9f;
        minusButton.BorderColor = new Color(220, 105, 105) * 0.9f;
        minusButton.OnLeftClick += (evt, element) => ChangeShownPlayerCards(-1);
        minusButton.OnMouseOver += (evt, element) =>
        {
            minusButton.BorderColor = Color.Yellow;
            statusText?.SetText("Show fewer players");
        };
        minusButton.OnMouseOut += (evt, element) =>
        {
            minusButton.BorderColor = new Color(220, 105, 105) * 0.9f;
            UpdateStatusText();
        };

        topRow.Append(minusButton);
    }

    private void AddPlusButton(UIPanel topRow, float topRowHeight)
    {
        UIAutoScaleTextTextPanel<string> plusButton = new("+");
        plusButton.SetPadding(0f);
        plusButton.HAlign = 1f;
        plusButton.Width.Set(topRowHeight, 0f);
        plusButton.Height.Set(topRowHeight, 0f);
        plusButton.BackgroundColor = new Color(45, 80, 52) * 0.9f;
        plusButton.BorderColor = new Color(120, 220, 140) * 0.9f;
        plusButton.OnLeftClick += (evt, element) => ChangeShownPlayerCards(1);
        plusButton.OnMouseOver += (evt, element) =>
        {
            plusButton.BorderColor = Color.Yellow;
            statusText?.SetText("Show more players");
        };
        plusButton.OnMouseOut += (evt, element) =>
        {
            plusButton.BorderColor = new Color(120, 220, 140) * 0.9f;
            UpdateStatusText();
        };

        topRow.Append(plusButton);
    }
    #endregion

    #region Debug players used for UI testing
#if DEBUG
    private static readonly List<int> debugPlayers = [];
    private void UpdateDebugPlayers()
    {
        if (KeyboardHelper.Pressed(Keys.NumPad1))
        {
            AddDebugPlayer();
            Rebuild();
        }

        if (KeyboardHelper.Pressed(Keys.NumPad2))
        {
            RemoveDebugPlayer();
            Rebuild();
        }
    }

    private static string GetNextDebugPlayerName()
    {
        for (int number = 1; number <= Main.maxPlayers; number++)
        {
            string name = $"Player{number}";

            bool duplicate = false;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];

                if (player?.active == true && player.name == name)
                {
                    duplicate = true;
                    break;
                }
            }

            if (!duplicate)
                return name;
        }

        return "Player";
    }

    private static void AddDebugPlayer()
    {
        int slot = -1;

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            if (i != Main.myPlayer && Main.player[i]?.active != true)
            {
                slot = i;
                break;
            }
        }

        if (slot < 0)
        {
            Log.Chat("No free player slots for debug spectate player.");
            return;
        }

        Player player = Main.LocalPlayer.SerializedClone();

        player.whoAmI = slot;
        player.name = GetNextDebugPlayerName();
        player.active = true;
        player.dead = false;
        player.ghost = false;
        player.team = Main.LocalPlayer.team;
        player.statLife = Math.Max(1, player.statLife);
        player.Center = new Vector2(Main.rand.Next(100, Math.Max(101, Main.maxTilesX - 100)), Main.rand.Next(100, Math.Max(101, Main.maxTilesY - 100))) * 16f;

        Main.player[slot] = player;
        SpectatorModeSystem.Modes[slot] = PlayerMode.Player;
        debugPlayers.Add(slot);

        Log.Chat($"Added debug spectate {player.name} at slot {slot}.");
    }

    private void RemoveDebugPlayer()
    {
        if (debugPlayers.Count == 0)
            return;

        int slot = debugPlayers[^1];
        debugPlayers.RemoveAt(debugPlayers.Count - 1);

        if (hovered == slot)
            hovered = -1;

        if (locked == slot)
            locked = -1;

        Main.player[slot] = new Player { whoAmI = slot };
        SpectatorModeSystem.Modes.Remove(slot);

        if (IsTargetValid(locked))
            SpectatorTargetSystem.SetPlayerTarget(locked);
        else
            SpectatorTargetSystem.ClearTarget();

        Log.Chat($"Removed debug spectate from slot {slot}.");
    }
#endif
    #endregion

}
