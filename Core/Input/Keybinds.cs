using Microsoft.Xna.Framework.Input;
using PvPAdventure.Common.Bounties;
using PvPAdventure.Common.Statistics;
using PvPAdventure.Content.Portals;
using System.Collections.Generic;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Input;

[Autoload(Side = ModSide.Client)]
public class Keybinds : ModSystem
{
    public ModKeybind Scoreboard { get; private set; }
    public ModKeybind BountyShop { get; private set; }
    public ModKeybind AllChat { get; private set; }
    public ModKeybind Dash { get; private set; }
    public ModKeybind UsePortalCreator { get; private set; }

    #region Portal creator label
    public static string UsePortalCreatorLabel
    {
        get
        {
            ModKeybind keybind = ModContent.GetInstance<Keybinds>().UsePortalCreator;

            if (keybind is null)
                return null;

            List<string> keys = keybind.GetAssignedKeys();
            keys.RemoveAll(static key => string.IsNullOrWhiteSpace(key));

            return keys.Count > 0 ? string.Join(" / ", keys) : null;
        }
    }
    #endregion

    public override void Load()
    {
        Scoreboard = KeybindLoader.RegisterKeybind(Mod, "Scoreboard", Keys.OemTilde);
        BountyShop = KeybindLoader.RegisterKeybind(Mod, "BountyShop", Keys.P);
        AllChat = KeybindLoader.RegisterKeybind(Mod, "AllChat", Keys.U);
        Dash = KeybindLoader.RegisterKeybind(Mod, "Dash", Keys.F);
        UsePortalCreator = KeybindLoader.RegisterKeybind(Mod, "UsePortalCreator", Keys.G);
    }
}

internal class KeybindsPlayer : ModPlayer
{
    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        var pointsManager = ModContent.GetInstance<PointsManager>();
        var keybinds = ModContent.GetInstance<Keybinds>();

        // Scoreboard
        if (keybinds.Scoreboard.JustPressed)
        {
            pointsManager.BossCompletion.Active = true;
            Main.InGameUI.SetState(pointsManager.UiScoreboard);
        }
        else if (keybinds.Scoreboard.JustReleased)
        {
            pointsManager.BossCompletion.Active = false;
            Main.InGameUI.SetState(null);
        }

        // Bounty Shop
        if (keybinds.BountyShop.JustPressed)
        {
            var bountyShop = ModContent.GetInstance<BountyManager>().UiBountyShop;

            if (Main.InGameUI.CurrentState == bountyShop)
                Main.InGameUI.SetState(null);
            else
                Main.InGameUI.SetState(bountyShop);
        }

        // UsePortalCreator keybind
        if (keybinds.UsePortalCreator.JustPressed)
        {
            //Log.Chat("Portal creator item keybind pressed");
            PortalCreatorItem.TryUse(Player);
        }
    }
}