using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Helpers;
using PvPAdventure.Core.SpawnSelector.UI;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace PvPAdventure.Core.SpawnSelector.Systems;

public class SpawnSelectorState : UIState
{
    public SpawnSelectorBasePanel spawnSelectorPanel;
    public override void OnInitialize()
    {
        spawnSelectorPanel = new();
        Append(spawnSelectorPanel);

        UITextPanel<string> chooseYourSpawnPanel = new("Choose Your Spawn", 0.8f, true);
        chooseYourSpawnPanel.HAlign = 0.5f;
        chooseYourSpawnPanel.Top.Set(10, 0);
        chooseYourSpawnPanel.BackgroundColor = new Color(73, 94, 171);
        Append(chooseYourSpawnPanel);
    }

    public override void OnActivate()
    {
        base.OnActivate();
        SpawnSelectorBasePanel panel = null;
        foreach (var element in Elements)
        {
            panel = element as SpawnSelectorBasePanel;
            if (panel != null)
                break;
        }
        if (panel != null)
        {
            panel.Rebuild();
            Log.Info("SpawnSelectorState: Rebuilt panel on activate");
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);
    }
}
