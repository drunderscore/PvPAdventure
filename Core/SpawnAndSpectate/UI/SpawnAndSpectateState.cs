using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;

namespace PvPAdventure.Core.SpawnAndSpectate.UI;
public class SpawnAndSpectateState : UIState
{
    // UI components
    private SpawnAndSpectateBasePanel basePanel;
    private UITextPanel<string> chooseYourSpawnPanel;

    public override void OnActivate()
    {
        // Title panel
        chooseYourSpawnPanel = new(Language.GetTextValue("Mods.PvPAdventure.SpawnAndSpectate.ChooseYourSpawn"), 0.8f, true)
        {
            HAlign = 0.5f,
            BackgroundColor = new Color(73, 94, 171),
            Top = new StyleDimension(40, 0)
        };

        // Base panel containing all UI components
        basePanel = new();

        Append(basePanel);
        Append(chooseYourSpawnPanel);
    }

    public void Rebuild()
    {
#if DEBUG
        Main.NewText("[DEBUG] Rebuilding SpawnAndSpectateState");
#endif

        RemoveAllChildren();
        basePanel = null;
        chooseYourSpawnPanel = null;

        // Re-run normal construction
        OnActivate();
        Recalculate();
    }
}
