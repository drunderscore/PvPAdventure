using DragonLens.Core.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PvPAdventure.Common.Spectator.SpectatorMode;
using PvPAdventure.Core.Config;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Spectator.Commands;

// Make everyone spectate, or everyone players.
internal class SpectateCommand : ModCommand
{
    public override string Command => "spectate";

    public override CommandType Type => CommandType.Chat;
    public override string Description => "Toggle spectate mode.";
    public override string Name => "Toggle spectate mode.";

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        // If not allowed, just print a message to user saying its not allowed.
        SpectatorConfig config = ModContent.GetInstance<SpectatorConfig>();
        if (!config.AllowSpectating)
        {
            Main.NewText("Spectating is disabled on this server.", Color.OrangeRed);
            return;
        }

        if (config.ForceSpectating)
        {
            Main.NewText("You cannot change your spectate status (Force Spectate Mode is on).", Color.OrangeRed);
            return;
        }

        // Toggle spectate mode.
        SpectateCommandHelper.ToggleSpectateMode(Main.LocalPlayer);
    }
}

internal class SpecCommand : ModCommand
{
    public override string Command => "spec";

    public override CommandType Type => CommandType.Chat;
    public override string Description => "Toggle spectate mode (admins only).";
    public override string Name => "Toggle spectate mode (admins only).";

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        // If not admin, just print a message to user saying its not allowed.
        if (!PermissionHandler.LooksLikeAdmin(Main.LocalPlayer))
        {
            Main.NewText("You must be an admin to use this command.", Color.OrangeRed);
            return;
        }

        // Toggle spectate mode.
        SpectateCommandHelper.ToggleSpectateMode(Main.LocalPlayer);
    }
}


public static class SpectateCommandHelper
{
    public static void ToggleSpectateMode(Player player)
    {
        if (SpectatorModeSystem.IsInSpectateMode(player))
        {
            SpectatorModeSystem.RequestSetLocalMode(PlayerMode.Player);
        }
        else
        {
            SpectatorModeSystem.RequestSetLocalMode(PlayerMode.Spectator);
        }
    }
}

#if DEBUG
public class SpectateDebugHelper : ModSystem
{
    public override void PostUpdateEverything()
    {
        if (Main.keyState.IsKeyDown(Keys.NumPad6) && Main.oldKeyState.IsKeyDown(Keys.NumPad6))
        {
            SpectateCommandHelper.ToggleSpectateMode(Main.LocalPlayer);
        }
    }
}
#endif


internal class GhostCommand : ModCommand
{
    public override string Command => "g";
    public override string Name => "Toggle ghost mode.";
    public override string Description => "Toggle ghost mode.";

    public override CommandType Type => CommandType.Chat;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        // Toggle ghost mode.
        if (Main.LocalPlayer.ghost) 
            Main.LocalPlayer.ghost = false;
        else 
            Main.LocalPlayer.ghost = true;
    }
}
