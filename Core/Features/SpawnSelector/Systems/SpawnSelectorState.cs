using Microsoft.Xna.Framework;
using PvPAdventure.Core.Features.SpawnSelector.UI;
using Terraria.UI;
using Terraria.GameContent.UI.Elements;

namespace PvPAdventure.Core.Features.SpawnSelector.Systems
{
    internal class SpawnSelectorState : UIState
    {
        public override void OnInitialize()
        {
            // Create the base panel with the teleport options
            UISpawnSelectorPanel spawnSelectorPanel = new();
            Append(spawnSelectorPanel);

            // Create the title panel with a height of 32px, 10 px from the top, centered horizontally
            UITextPanel<string> chooseYourSpawnPanel = new("Choose Your Spawn", 0.8f, true);
            chooseYourSpawnPanel.HAlign = 0.5f;
            chooseYourSpawnPanel.VAlign = 0f;
            chooseYourSpawnPanel.Top.Set(10, 0);
            chooseYourSpawnPanel.BackgroundColor = new Color(73, 94, 171);
            Append(chooseYourSpawnPanel);
        }
    }
}
