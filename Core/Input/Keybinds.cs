using DragonLens.Core.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PvPAdventure.Common.Arenas.UI;
using PvPAdventure.Common.Bounties;
using PvPAdventure.Common.GameTimer;
using PvPAdventure.Common.Spectator.UI.State;
using PvPAdventure.Common.Statistics;
using PvPAdventure.Common.Teams;
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
    public ModKeybind SpectateMenu { get; private set; }
    public ModKeybind UseAdventureMirror { get; private set; }

    public override void Load()
    {
        Scoreboard = KeybindLoader.RegisterKeybind(Mod, "Scoreboard", Keys.OemTilde);
        BountyShop = KeybindLoader.RegisterKeybind(Mod, "BountyShop", Keys.P);
        AllChat = KeybindLoader.RegisterKeybind(Mod, "AllChat", Keys.U);
        Dash = KeybindLoader.RegisterKeybind(Mod, "Dash", Keys.F);
        ArenasMenu = KeybindLoader.RegisterKeybind(Mod, "ArenasMenu", Keys.NumPad7);
        SpectateMenu = KeybindLoader.RegisterKeybind(Mod, "SpectateMenu", Keys.NumPad8);
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
            ArenasUISystem.Toggle();
        }

        // Spectator UI
        var spectatorConfig = ModContent.GetInstance<SpectatorConfig>();
        if (spectatorConfig.ShowSpectateOptionWhenJoining && keybinds.SpectateMenu.JustPressed)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient && spectatorConfig.ForcePlayersToBeSpectatorsWhenJoining)
            {
                if (PermissionHandler.LooksLikeAdmin(Main.LocalPlayer))
                {
                    Main.NewText("Opening spectate options for admin.", Color.Yellow);
                }
                else
                {
                    Main.NewText("Spectator mode is enabled. Only admins can change your spectate status.", Color.OrangeRed);
                }

                return;
            }
            SpectatorUISystem.ToggleSpectateJoinUI();
        }

        // Adventure mirror keybind
        if (keybinds.UseAdventureMirror.JustPressed)
        {
            Log.Chat("Adventure mirror keybind pressed");
            AdventureMirror.TryUse(Player);
        }
    }
}