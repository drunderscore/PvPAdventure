using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PvPAdventure.Core.Features.SpawnSelector.Systems;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Core.Debug;

#if DEBUG
[Autoload(Side = ModSide.Client)]
public class DebugKeybinds : ModSystem
{
    public ModKeybind OpenControls { get; private set; }
    public ModKeybind SwitchTeams { get; private set; }
    public ModKeybind AddPlayerToSpawnSelector { get; private set; }
    public ModKeybind ClearPlayersFromSpawnSelector { get; private set; }

    public override void Load()
    {
        OpenControls = KeybindLoader.RegisterKeybind(Mod, "OpenControls", Keys.NumPad0);
        SwitchTeams = KeybindLoader.RegisterKeybind(Mod, "SwitchTeams", Keys.NumPad1);
        AddPlayerToSpawnSelector = KeybindLoader.RegisterKeybind(Mod, "AddPlayerToSpawnSelector", Keys.NumPad2);
        ClearPlayersFromSpawnSelector = KeybindLoader.RegisterKeybind(Mod, "ClearPlayersFromSpawnSelector", Keys.NumPad3);
    }
}

public class DebugKeybindsPlayer : ModPlayer
{
    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        var key = ModContent.GetInstance<DebugKeybinds>();

        // Open controls
        if (key.OpenControls.JustPressed)
        {
            IngameFancyUI.OpenKeybinds();
        }

        // Switch teams
        if (key.SwitchTeams.JustPressed)
        {
            Player.team = (Player.team + 1) % 6; // Cycle through teams 0-5
            Main.NewText("[DEBUG] " + Player.name + " switched to team: " + (Terraria.Enums.Team)Player.team);
        }

        if (key.AddPlayerToSpawnSelector.JustPressed)
        {
            var sys = ModContent.GetInstance<SpawnSelectorSystem>();
            sys.state.spawnSelectorPanel.DebugAddPlayer();
            sys.state.spawnSelectorPanel.Rebuild();

            sys.ui.SetState(null);
            sys.ui.SetState(sys.state);
            Main.NewText("[DEBUG] Added player to UISpawnSelectorPanel");
        }

        if (key.ClearPlayersFromSpawnSelector.JustPressed)
        {
            var sys = ModContent.GetInstance<SpawnSelectorSystem>();
            sys.state.spawnSelectorPanel.DebugClearPlayers();
            Main.NewText("[DEBUG] Cleared all players from UISpawnSelectorPanel");
            sys.state.spawnSelectorPanel.Rebuild();
        }
    }
}

#endif