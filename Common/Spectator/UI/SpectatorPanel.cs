using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Spectator.UI.Tabs;
using PvPAdventure.Common.Spectator.UI.Tabs.NPCs;
using PvPAdventure.Common.Spectator.UI.Tabs.Players;
using PvPAdventure.Common.Spectator.UI.Tabs.World;
using PvPAdventure.UI;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace PvPAdventure.Common.Spectator.UI;

/// <summary>
/// Main spectator panel. Hosts tab buttons and displays the selected tab.
/// </summary>
internal sealed class SpectatorPanel : UIDraggablePanel
{
    private readonly List<ISpectatorTab> tabs = [];
    private readonly List<SpectatorTabButton> tabButtons = [];

    private ISpectatorTab currentTab;
    protected override bool ShowRefreshButton => false;

    protected override void OnClosePanelLeftClick() => Remove();
    protected override bool IsTabButtonHovered() => tabButtons.Any(static button => button.IsMouseHovering);
    protected override void OnPanelRebuilt()
    {
        tabButtons.Clear();
        BuildTabButtons();
        ShowTab(currentTab?.Tab ?? SpectatorTab.Player);
    }

    public SpectatorPanel() : base("")
    {
        Width.Set(560f, 0f);
        Height.Set(560f, 0f);
        HAlign = 0.32f;
        VAlign = 0.45f;

        tabs.Add(new PlayerTab());
        tabs.Add(new NPCTab());
        tabs.Add(new WorldTab());

        currentTab = tabs[0];

        BuildTabButtons();
        ShowTab(currentTab.Tab);
    }

    //private void BuildTabButtons()
    //{
    //    tabButtons.Clear();

    //    const float reservedRightWidth = 40f;
    //    int count = tabs.Count;

    //    for (int i = 0; i < count; i++)
    //    {
    //        ISpectatorTab capturedTab = tabs[i];

    //        SpectatorTabButton button = new(
    //            capturedTab.HeaderText,
    //            capturedTab.TooltipText,
    //            capturedTab.Icon,
    //            () => currentTab == capturedTab,
    //            () => ShowTab(capturedTab.Tab),
    //            GetTabIconYOffset(capturedTab.Tab));

    //        button.Left.Set(-reservedRightWidth * i / count, i / (float)count);
    //        button.Width.Set(-reservedRightWidth / count, 1f / count);

    //        TitlePanel.Append(button);
    //        tabButtons.Add(button);
    //    }
    //}

    private void BuildTabButtons()
    {
        tabButtons.Clear();

        const float buttonWidth = 100f;

        for (int i = 0; i < tabs.Count; i++)
        {
            ISpectatorTab capturedTab = tabs[i];

            SpectatorTabButton button = new(
                capturedTab.HeaderText,
                capturedTab.TooltipText,
                capturedTab.Icon,
                () => currentTab == capturedTab,
                () => ShowTab(capturedTab.Tab),
                GetTabIconYOffset(capturedTab.Tab),
                GetLabelXOffset(capturedTab.Tab));

            button.Left.Set(i * buttonWidth, 0f);
            button.Width.Set(buttonWidth, 0f);

            TitlePanel.Append(button);
            tabButtons.Add(button);
        }
    }

    private static float GetLabelXOffset(SpectatorTab tab)
    {
        return tab switch
        {
            SpectatorTab.Player => 0f,
            SpectatorTab.NPCs => 6f,
            SpectatorTab.World => 6f,
            _ => 0f
        };
    }

    private static float GetTabIconYOffset(SpectatorTab tab)
    {
        return tab switch
        {
            SpectatorTab.Player => -3f,
            SpectatorTab.NPCs => -5f,
            SpectatorTab.World => -5f,
            _ => 0f
        };
    }

    private void ShowTab(SpectatorTab tab)
    {
        ISpectatorTab nextTab = GetTab(tab);

        if (nextTab is null)
            return;

        ContentPanel.RemoveAllChildren();

        currentTab = nextTab;

        UIElement element = (UIElement)currentTab;
        element.Width.Set(0f, 1f);
        element.Height.Set(0f, 1f);
        element.SetPadding(0f);

        ContentPanel.Append(element);

        currentTab.Refresh();

        foreach (SpectatorTabButton button in tabButtons)
            button.Recalculate();

        Recalculate();
    }

    private ISpectatorTab GetTab(SpectatorTab tab)
    {
        foreach (ISpectatorTab candidate in tabs)
        {
            if (candidate.Tab == tab)
                return candidate;
        }

        return null;
    }

    internal sealed class SpectatorTabButton : UIPanel
    {
        private readonly Func<bool> isSelected;
        private readonly string hoverText;

        public SpectatorTabButton(string headerText, string tooltipText, Asset<Texture2D> icon, Func<bool> isSelected, Action onClick, float iconYOffset, float labelXOffset = 0f)
        {
            this.isSelected = isSelected;
            hoverText = tooltipText;

            Height.Set(0f, 1f);
            VAlign = 0.5f;
            SetPadding(0f);

            OnLeftClick += (_, _) => onClick();

            Append(new UIImage(icon.Value)
            {
                Left = new StyleDimension(6f, 0f),
                Top = new StyleDimension(iconYOffset, 0f),
                VAlign = 0.5f,
                Width = new StyleDimension(22f, 0f),
                Height = new StyleDimension(22f, 0f)
            });

            Append(new UIText(headerText, textScale: 1f)
            {
                Left = new StyleDimension(31f + labelXOffset, 0f),
                VAlign = 0.5f
            });
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            BackgroundColor = isSelected() ? new Color(83, 97, 168) : new Color(63, 82, 151) * 0.85f;
            BorderColor = IsMouseHovering ? Color.Yellow : isSelected() ? Color.White : Color.Black;

            if (IsMouseHovering)
                Main.instance.MouseText(hoverText);
        }
    }
}