using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Spectator.Drawers.Inventory;
using PvPAdventure.Common.Spectator.SpectatorMode;
using Terraria;
using Terraria.UI;

namespace PvPAdventure.Common.Spectator.UI;

internal sealed class SpectatorUIState : UIState
{
    private SpectatorControlsPanel spectatorControlsElement;

    public override void OnActivate()
    {
        RemoveAllChildren();
    }

    public void RebuildSpectatorControlsPanel()
    {
        spectatorControlsElement?.Rebuild();
    }

    internal void EnsureSpectatorHUDStaysOpen()
    {
        if (spectatorControlsElement?.Parent is not null)
        {
            return;
        }

        spectatorControlsElement ??= new SpectatorControlsPanel();
        Append(spectatorControlsElement);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        Player local = Main.LocalPlayer;
        if (local is null || !local.active)
            return;

        if (SpectatorModeSystem.IsInSpectateMode(local))
        {
            EnsureSpectatorHUDStaysOpen();
            PlayerHudOverlay.Update();
        }
        else
        {
            spectatorControlsElement?.Remove();
            PlayerHudOverlay.Clear();
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

        // Draw spectated player's inventory
        PlayerHudOverlay.Draw(sb);
    }
}
