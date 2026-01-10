using PvPAdventure.System;
using PvPAdventure.System.Client;
using SubworldLibrary;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Arenas.UI;

internal class ArenasPlayer : ModPlayer
{
    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        // Toggle temp
        var keybinds = ModContent.GetInstance<Keybinds>();
        int onehour = 60 * 60 * 60;
        var gm = ModContent.GetInstance<GameManager>();
        if (keybinds.Loadout.JustPressed && (gm.TimeRemaining < onehour || gm.CurrentPhase == GameManager.Phase.Waiting))
        {
            LoadoutUI.ArenasLoadoutUISystem.Toggle();
        }

        //if (keybinds.Arenas.JustPressed && gm.CurrentPhase == GameManager.Phase.Waiting)
        {
            //JoinUI.ArenasJoinUISystem.Toggle();
        }
    }

    public override void ResetEffects()
    {
        if (SubworldSystem.AnyActive())
        {
            Player.statLifeMax2 = 400;
            if (Player.statLife > 400)
                Player.statLife = 400;
        }
    }
}
