using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;

namespace PvPAdventure.Core.SpawnAndSpectate.SpectateUI;

public class SpectateState : UIState
{
    // UI components
    private SpectateBasePanel spectateBasePanel;
    private UITextPanel<string> spectateTitlePanel;

    public override void OnActivate()
    {
        // Title panel
        spectateTitlePanel = new(Language.GetTextValue("Mods.PvPAdventure.SpawnAndSpectate.SpectateTitle"), 0.8f, true)
        {
            HAlign = 0.5f,
            BackgroundColor = new Color(73, 94, 171),
            Top = new StyleDimension(10, 0)
        };

        // Base panel containing all teammates + exit button
        spectateBasePanel = new SpectateBasePanel();

        Append(spectateBasePanel);
        Append(spectateTitlePanel);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        UpdateSpectateStatusText();
    }

    private void UpdateSpectateStatusText()
    {
        if (spectateTitlePanel == null)
            return;

        string text = Language.GetTextValue("Mods.PvPAdventure.SpawnAndSpectate.SpectateTitle");

        if (SpawnAndSpectateSystem.SpectatePlayerIndex is int idx)
        {
            if (idx >= 0 && idx < Main.maxPlayers)
            {
                Player p = Main.player[idx];
                if (p != null && p.active)
                {
                    text = Language.GetTextValue("Mods.PvPAdventure.SpawnAndSpectate.SpectatingPlayer", p.name);
                }
            }
        }

        spectateTitlePanel.SetText(text);
    }
}