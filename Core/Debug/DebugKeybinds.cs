using Microsoft.Xna.Framework.Input;
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

    public override void Load()
    {
        OpenControls = KeybindLoader.RegisterKeybind(Mod, "[DEBUG] OpenControls", Keys.NumPad0);
        SwitchTeams = KeybindLoader.RegisterKeybind(Mod, "[DEBUG] SwitchTeams", Keys.NumPad1);
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
    }
}

#endif