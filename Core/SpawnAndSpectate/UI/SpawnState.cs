using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;

namespace PvPAdventure.Core.SpawnAndSpectate.UI;
public class SpawnState : UIState
{
    // UI components
    public SpawnAndSpectateBasePanel basePanel;
    public UITextPanel<string> chooseYourSpawnPanel;

    public override void OnActivate()
    {
        //Log.Chat("OnActivate() called");

        // Title panel
        chooseYourSpawnPanel = new(Language.GetTextValue("Mods.PvPAdventure.Spawn.ChooseYourSpawn"), 0.7f, true)
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
}
