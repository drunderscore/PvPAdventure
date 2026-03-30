using PvPAdventure.Common.MainMenu.State;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;

namespace PvPAdventure.Common.MainMenu.PlayServerList;

internal static class PlayMenuFlow
{
    public static void OpenCharacterSelect()
    {
        SoundEngine.PlaySound(SoundID.MenuOpen);
        var menu = ModContent.GetInstance<MainMenuSystem>();
        var state = new TPVPACharacterSelectUIState();
        menu.ui?.SetState(state);
        state.Recalculate();
    }

    public static void OpenServerList()
    {
        // FIX: Changed from 0 to 888 so Terraria knows a custom UI is active
        Main.menuMode = 888;
        MainMenuTPVPABrowserUIState.OpenState(() => new PlayServerListUIState(), playSound: false);
    }

    public static void ReturnToBrowser()
    {
        // FIX: Changed from 0 to 888 here as well for the back button flow
        Main.menuMode = 888;
        MainMenuTPVPABrowserUIState.OpenState(() => new MainMenuTPVPABrowserUIState(), playSound: false);
    }

    public static void SetSelectedPlayer(PlayerFileData data)
    {
        Main.ServerSideCharacter = false;
        Main.myPlayer = 0;
        data.SetAsActive();
    }
}