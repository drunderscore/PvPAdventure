using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;

namespace PvPAdventure.Core.SpawnAndSpectate.SpawnSelectorUI;
public class SpawnSelectorState : UIState
{
    // UI components
    public SpawnSelectorBasePanel spawnSelectorPanel;

    public override void OnActivate()
    {
        // Title panel
        UITextPanel<string> chooseYourSpawnPanel =
            new(Language.GetTextValue("Mods.PvPAdventure.SpawnAndSpectate.ChooseYourSpawn"), 0.8f, true)
            {
                HAlign = 0.5f,
                BackgroundColor = new Color(73, 94, 171),
                Top = new StyleDimension(10, 0)
            };

        // Base panel containing all teammates and random panel
        spawnSelectorPanel = new();

        Append(spawnSelectorPanel);
        Append(chooseYourSpawnPanel);
    }
}
