using Microsoft.Xna.Framework.Input;
using Terraria.ModLoader;

namespace PvPAdventure.System.Client;

[Autoload(Side = ModSide.Client)]
public class Keybinds : ModSystem
{
    public ModKeybind Scoreboard { get; private set; }
    public ModKeybind BountyShop { get; private set; }
    public ModKeybind AllChat { get; private set; }
    public ModKeybind Dash { get; private set; }
    public ModKeybind ArenasMenu { get; private set; }

    public override void Load()
    {
        Scoreboard = KeybindLoader.RegisterKeybind(Mod, "Scoreboard", Keys.OemTilde);
        BountyShop = KeybindLoader.RegisterKeybind(Mod, "BountyShop", Keys.P);
        AllChat = KeybindLoader.RegisterKeybind(Mod, "AllChat", Keys.U);
        Dash = KeybindLoader.RegisterKeybind(Mod, "DashKeybind", Keys.F);
        ArenasMenu = KeybindLoader.RegisterKeybind(Mod, "OpenArenasMenu", Keys.G);
    }
}