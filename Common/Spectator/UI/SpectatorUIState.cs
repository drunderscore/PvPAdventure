using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Spectator.SpectatorMode;
using Terraria;
using Terraria.UI;

namespace PvPAdventure.Common.Spectator.UI;

internal sealed class SpectatorUIState : UIState
{
    private SpectatorControls spectatorControlsElement;
    private SpectatorPanel spectatePanel;

    public override void OnActivate()
    {
        RemoveAllChildren();
    }

    internal bool IsSpectatePanelOpen() => spectatePanel?.Parent != null;

    internal void ToggleSpectatePanel()
    {
        if (IsSpectatePanelOpen())
        {
            spectatePanel.Remove();
            return;
        }

        spectatePanel ??= new SpectatorPanel();
        Append(spectatePanel);
    }

    internal void EnsurePlayerSpectatorControlsOpen()
    {
        if (spectatorControlsElement?.Parent is not null)
            return;

        spectatorControlsElement ??= new SpectatorControls();
        Append(spectatorControlsElement);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        Player local = Main.LocalPlayer;
        if (local is null || !local.active)
            return;

        if (SpectatorModeSystem.IsInSpectateMode(local))
            EnsurePlayerSpectatorControlsOpen();
        else
        {
            spectatorControlsElement?.Remove();
            spectatePanel?.Remove();
        }

        spectatorControlsElement?.UpdateTarget();
    }

    public override void Draw(SpriteBatch sb)
    {
        base.Draw(sb);

        Player local = Main.LocalPlayer;

        if (local?.active != true)
            return;

        if (!SpectatorModeSystem.IsInSpectateMode(local))
            return;

        SpectatorInventoryOverlay.Draw(sb);
    }
}
