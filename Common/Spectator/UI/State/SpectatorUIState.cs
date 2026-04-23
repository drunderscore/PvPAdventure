using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Spectator.UI.NPCs;
using PvPAdventure.Common.Spectator.UI.Players;
using PvPAdventure.Common.Spectator.UI.World;
using PvPAdventure.Core.Utilities;
using PvPAdventure.UI;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace PvPAdventure.Common.Spectator.UI.State;

internal sealed class SpectatorUIState : UIState
{
    private UIColoredImageButton playerButton;
    private UIColoredImageButton worldButton;
    private UIColoredImageButton npcButton;

    private SpectatorControls playerSpectatorControls;
    private SpectatorControls npcSpectatorControls;
    private SpectatorPlayerPanel playerPanel;
    private SpectatorWorldPanel worldPanel;
    private SpectatorNPCPanel npcPanel;

    private JoinPanel joinPanel;
    private bool showJoinPanel;

    public override void OnActivate()
    {
        RemoveAllChildren();

        playerButton = CreateTopButton(2, TogglePlayerPanel, Ass.Icon_PlayerHead);
        worldButton = CreateTopButton(3, ToggleWorldPanel, Ass.Icon_World);
        npcButton = CreateTopButton(4, ToggleNpcPanel, Ass.Icon_NPC);

        Append(playerButton);
        Append(worldButton);
        Append(npcButton);

        UpdateJoinPanel();
    }

    private static UIColoredImageButton CreateTopButton(int index, Action onClick, Asset<Texture2D> icon)
    {
        UIColoredImageButton button = new(icon, isSmall: true);
        button.HAlign = 1f;
        button.Top.Set(80f, 0f);
        button.Left.Set(-100f - index * 32f, 0f);
        button.SetVisibility(1f, 1f);
        button.OnLeftClick += (_, _) => onClick();
        return button;
    }

    internal void ToggleJoinPanel()
    {
        showJoinPanel = !showJoinPanel;
        UpdateJoinPanel();
    }

    internal void CloseJoinPanel()
    {
        showJoinPanel = false;
        UpdateJoinPanel();
    }

    internal bool IsJoinPanelOpen() => showJoinPanel;

    private void UpdateJoinPanel()
    {
        if (showJoinPanel)
        {
            joinPanel?.Remove();
            joinPanel = new JoinPanel();
            Append(joinPanel);
        }
        else
        {
            joinPanel?.Remove();
            joinPanel = null;
        }
    }

    private static bool HandleHover(UIElement element, string text)
    {
        if (element?.IsMouseHovering != true)
            return false;

        Main.instance.MouseText(text);
        Main.LocalPlayer.mouseInterface = true;
        return true;
    }

    internal void EnsurePlayerSpectatorControlsOpen()
    {
        if (playerSpectatorControls?.Parent is not null)
            return;

        playerSpectatorControls ??= new SpectatorControls(SpectatorTargetKind.Player);
        Append(playerSpectatorControls);
    }

    internal void EnsureNpcSpectatorControlsOpen()
    {
        if (npcSpectatorControls?.Parent is not null)
            return;

        npcSpectatorControls ??= new SpectatorControls(SpectatorTargetKind.NPC);
        Append(npcSpectatorControls);
    }

    internal void TogglePlayerSpectatorControls()
    {
        if (playerSpectatorControls?.Parent is null)
        {
            playerSpectatorControls ??= new SpectatorControls(SpectatorTargetKind.Player);
            Append(playerSpectatorControls);
        }
        else playerSpectatorControls.Remove();
    }

    internal void ToggleNpcSpectatorControls()
    {
        if (npcSpectatorControls?.Parent is null)
        {
            npcSpectatorControls ??= new SpectatorControls(SpectatorTargetKind.NPC);
            Append(npcSpectatorControls);
        }
        else npcSpectatorControls.Remove();
    }

    private void TogglePlayerPanel()
    {
        if (playerPanel?.Parent is null)
        {
            playerPanel ??= new SpectatorPlayerPanel();
            Append(playerPanel);
        }
        else playerPanel.Remove();
    }

    private void ToggleWorldPanel()
    {
        if (worldPanel?.Parent is null)
        {
            worldPanel ??= new SpectatorWorldPanel();
            Append(worldPanel);
        }
        else worldPanel.Remove();
    }

    private void ToggleNpcPanel()
    {
        if (npcPanel?.Parent is null)
        {
            npcPanel ??= new SpectatorNPCPanel();
            Append(npcPanel);
        }
        else npcPanel.Remove();
    }

    private static void ToggleGhostState()
    {
        Player local = Main.LocalPlayer;
        if (local is null || !local.active)
            return;

        if (local.ghost)
            local.ghost = false;
        else
            local.ghost = true;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        Player local = Main.LocalPlayer;
        if (local is null || !local.active)
            return;

        HandleHover(playerButton, "Open player panel");
        HandleHover(worldButton, "Open world panel");
        HandleHover(npcButton, "Open NPC panel");

        playerSpectatorControls?.UpdateTarget();
        npcSpectatorControls?.UpdateTarget();
    }

    public override void Draw(SpriteBatch sb)
    {
        if (playerButton is null || worldButton is null || npcButton is null)
            return;

        base.Draw(sb);

#if DEBUG
        //DebugDrawer.DrawElement(sb, eyeButton);
        //DebugDrawer.DrawElement(sb, playerButton);
        //DebugDrawer.DrawElement(sb, worldButton);
        //DebugDrawer.DrawElement(sb, npcButton, drawSize: false);
#endif
    }
}

public sealed class JoinPanel : UIElement
{
    public JoinPanel()
    {
        Width.Set(0f, 1f);
        Height.Set(0f, 1f);

        UIDraggableElement root = new() { HAlign = 0.5f };
        root.Width.Set(290f, 0f);
        root.Height.Set(156f, 0f);
        root.Top.Set(100f, 0f);
        Append(root);

        UITextPanel<string> title = new("Choose Player Mode", 0.6f, true)
        {
            HAlign = 0.5f,
            BackgroundColor = new Color(73, 94, 171)
        };
        title.Width.Set(0f, 1f);
        title.OnLeftMouseDown += (evt, _) => root.BeginDrag(evt);
        title.OnLeftMouseUp += (evt, _) => root.EndDrag(evt);
        root.Append(title);

        root.Recalculate();
        float titleHeight = title.GetOuterDimensions().Height;

        UIPanel container = new()
        {
            BackgroundColor = new Color(33, 43, 79) * 0.8f
        };
        container.SetPadding(0f);
        container.Top.Set(titleHeight, 0f);
        container.Width.Set(0f, 1f);
        container.Height.Set(-titleHeight, 1f);
        root.Append(container);

        UITextActionPanel playerRow = new("Player", SpectatorUISystem.EnterPlayerMode, titleHeight, 0.5f, true, Ass.Icon_Player.Value);
        playerRow.Left.Set(8f, 0f);
        playerRow.Top.Set(8f, 0f);
        playerRow.Width.Set(-16f, 1f);

        UITextActionPanel spectateRow = new("Spectator", SpectatorUISystem.EnterSpectateMode, titleHeight, 0.5f, true, Ass.Icon_Eye.Value);
        spectateRow.Left.Set(8f, 0f);
        spectateRow.Top.Set(8f + titleHeight + 8f, 0f);
        spectateRow.Width.Set(-16f, 1f);

        container.Append(playerRow);
        container.Append(spectateRow);

        root.Recalculate();
    }
}