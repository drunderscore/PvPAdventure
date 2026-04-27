using DragonLens.Core.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PvPAdventure.Common.Arenas.UI;
using PvPAdventure.Common.Bounties;
using PvPAdventure.Common.Chat;
using PvPAdventure.Common.GameTimer;
using PvPAdventure.Common.Spectator.UI;
using PvPAdventure.Common.Statistics;
using PvPAdventure.Content.Items;
using PvPAdventure.Core.Config;
using Terraria;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Input;

[Autoload(Side = ModSide.Client)]
public class Keybinds : ModSystem
{
    public ModKeybind Scoreboard { get; private set; }
    public ModKeybind BountyShop { get; private set; }
    public ModKeybind AllChat { get; private set; }
    public ModKeybind Dash { get; private set; }
    public ModKeybind ArenasMenu { get; private set; }
    public ModKeybind UseAdventureMirror { get; private set; }

    #region Adventure mirror label
    public static string UseAdventureMirrorLabel => GetLabel(ModContent.GetInstance<Keybinds>().UseAdventureMirror, "assign a keybind in Controls");
    private static string GetLabel(ModKeybind keybind, string unboundText = "assign a keybind in Controls")
    {
        if (keybind is null)
            return unboundText;

        var keys = keybind.GetAssignedKeys();
        keys.RemoveAll(static key => string.IsNullOrWhiteSpace(key));

        return keys.Count > 0 ? string.Join(" / ", keys) : unboundText;
    }
    #endregion

    public override void Load()
    {
        Scoreboard = KeybindLoader.RegisterKeybind(Mod, "Scoreboard", Keys.OemTilde);
        BountyShop = KeybindLoader.RegisterKeybind(Mod, "BountyShop", Keys.P);
        AllChat = KeybindLoader.RegisterKeybind(Mod, "AllChat", Keys.U);
        Dash = KeybindLoader.RegisterKeybind(Mod, "Dash", Keys.F);
        ArenasMenu = KeybindLoader.RegisterKeybind(Mod, "ArenasMenu", Keys.F1);
        UseAdventureMirror = KeybindLoader.RegisterKeybind(Mod, "UseAdventureMirror", Keys.G);
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
        // All Chat
        if (keybinds.AllChat.JustPressed)
            ModContent.GetInstance<TeamChatManager>().OpenAllChat();

        // Arenas UI
        var arenasConfig = ModContent.GetInstance<ArenasConfig>();

        if (arenasConfig.IsArenasEnabled && keybinds.ArenasMenu.JustPressed)
        {
            Log.Chat("Arenas menu keybind pressed");
            ArenasUISystem.Toggle();
        }

        // Adventure mirror keybind
        if (keybinds.UseAdventureMirror.JustPressed)
        {
            Log.Chat("Adventure mirror keybind pressed");
            AdventureMirror.TryUse(Player);
        }
    }
}