using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.UI;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace PvPAdventure.Common.Spectator.UI;

internal sealed class SpectatorControls : UIElement
{
    private const float NamePanelBaseScale = 1.0f;
    private readonly SpectatorTargetKind targetKind;

    private UIElement controlsRoot;
    private UITextActionPanel prevButton;
    private UITextActionPanel namePanel;
    private UITextActionPanel nextButton;

    public SpectatorControls(SpectatorTargetKind targetKind)
    {
        this.targetKind = targetKind;

        Width.Set(300f, 0f);
        Height.Set(36f, 0f);
        HAlign = 0.5f;
        VAlign = 1f;
        Top.Set(targetKind == SpectatorTargetKind.NPC ? -96f : -56f, 0f);

        controlsRoot = new UIElement();
        controlsRoot.Width.Set(300f, 0f);
        controlsRoot.Height.Set(36f, 0f);
        Append(controlsRoot);

        prevButton = new UITextActionPanel("<", HandlePreviousClick, 36f, 0.75f, true);
        prevButton.Width.Set(36f, 0f);
        prevButton.Left.Set(0f, 0f);
        controlsRoot.Append(prevButton);

        namePanel = new UITextActionPanel("", HandleNamePanelClick, 36f, NamePanelBaseScale, false);
        namePanel.Width.Set(220f, 0f);
        namePanel.Left.Set(40f, 0f);
        controlsRoot.Append(namePanel);

        nextButton = new UITextActionPanel(">", HandleNextClick, 36f, 0.75f, true);
        nextButton.Width.Set(36f, 0f);
        nextButton.Left.Set(264f, 0f);
        controlsRoot.Append(nextButton);

        ApplyTheme();
    }
    public override void OnActivate()
    {
        base.OnActivate();
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (IsMouseHovering)
        {
            Main.LocalPlayer.mouseInterface = true; // disable mouse clicks
        }

        string currentText = SpectatorSystem.GetTargetPanelTooltip(targetKind);
        string prevText = targetKind == SpectatorTargetKind.NPC ? "Spectate prev NPC: -" : "Spectate prev player: -";
        string nextText = targetKind == SpectatorTargetKind.NPC ? "Spectate next NPC: -" : "Spectate next player: -";

        if (targetKind == SpectatorTargetKind.NPC)
        {
            NPC target = SpectatorSystem.GetNPCTarget();
            List<int> targets = SpectatorSystem.GetNPCTargets();

            if (targets.Count > 0)
            {
                int currentIndex = target is null ? -1 : targets.IndexOf(target.whoAmI);
                int prevIndex = currentIndex < 0 ? targets.Count - 1 : currentIndex - 1;
                int nextIndex = currentIndex < 0 ? 0 : currentIndex + 1;

                if (prevIndex < 0)
                    prevIndex = targets.Count - 1;

                if (nextIndex >= targets.Count)
                    nextIndex = 0;

                NPC prevTarget = Main.npc[targets[prevIndex]];
                NPC nextTarget = Main.npc[targets[nextIndex]];

                prevText = $"Spectate prev NPC: {prevTarget.FullName}";
                nextText = $"Spectate next NPC: {nextTarget.FullName}";
            }
        }
        else
        {
            Player target = SpectatorSystem.GetPlayerTarget();
            List<int> targets = SpectatorSystem.GetTargets(Main.LocalPlayer.whoAmI);

            if (targets.Count > 0)
            {
                int currentIndex = target is null ? -1 : targets.IndexOf(target.whoAmI);

                int prevIndex = currentIndex < 0 ? targets.Count - 1 : currentIndex - 1;
                int nextIndex = currentIndex < 0 ? 0 : currentIndex + 1;

                if (prevIndex < 0)
                    prevIndex = targets.Count - 1;

                if (nextIndex >= targets.Count)
                    nextIndex = 0;

                Player prevTarget = Main.player[targets[prevIndex]];
                Player nextTarget = Main.player[targets[nextIndex]];

                prevText = $"Spectate prev player: {prevTarget.name}";
                nextText = $"Spectate next player: {nextTarget.name}";

#if DEBUG
                prevText += $"({prevTarget.whoAmI})";
                nextText += $"({nextTarget.whoAmI})";
#endif
            }
        }

        if (prevButton.IsMouseHovering)
            Main.instance.MouseText(prevText);

        if (nextButton.IsMouseHovering)
            Main.instance.MouseText(nextText);

        if (namePanel.IsMouseHovering)
            Main.instance.MouseText(currentText);

        if (prevButton.IsMouseHovering || nextButton.IsMouseHovering || namePanel.IsMouseHovering)
            Main.LocalPlayer.mouseInterface = true;
    }

    public void UpdateTarget()
    {
        string text = SpectatorSystem.GetCurrentTargetText(targetKind);
        namePanel.SetTextAndFitScale(text, NamePanelBaseScale, 0.35f, 12f);
    }

    private void HandlePreviousClick()
    {
        if (targetKind == SpectatorTargetKind.NPC)
            SpectatorSystem.PreviousNPCTarget();
        else
            SpectatorSystem.PreviousPlayerTarget();
    }

    private void HandleNextClick()
    {
        if (targetKind == SpectatorTargetKind.NPC)
            SpectatorSystem.NextNPCTarget();
        else
            SpectatorSystem.NextPlayerTarget();
    }

    private void HandleNamePanelClick()
    {
        SpectatorSystem.ToggleTargetSelection(targetKind);
    }

    private void ApplyTheme()
    {
        Color backgroundColor = targetKind == SpectatorTargetKind.NPC
            ? new Color(125, 38, 38) * 0.88f
            : new Color(63, 82, 151) * 0.88f;
        Color borderColor = targetKind == SpectatorTargetKind.NPC
            ? new Color(188, 86, 86)
            : new Color(89, 116, 213);

        prevButton.BackgroundColor = backgroundColor;
        prevButton.BorderColor = borderColor;
        namePanel.BackgroundColor = backgroundColor;
        namePanel.BorderColor = borderColor;
        nextButton.BackgroundColor = backgroundColor;
        nextButton.BorderColor = borderColor;
    }
}
