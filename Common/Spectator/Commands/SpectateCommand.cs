using Microsoft.Xna.Framework;
using PvPAdventure.Common.GameTimer;
using PvPAdventure.Common.Spectator.SpectatorMode;
using PvPAdventure.Core.Config;
using System;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Spectator.Commands;

// Make everyone spectate, or everyone players.
internal class SpectateCommand : ModCommand
{
    public override CommandType Type => CommandType.Chat;
    public override string Command => "spectate";
    public override string Description => "Toggle spectate mode.";

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        Player player = Main.LocalPlayer;

        if (ModContent.GetInstance<GameManager>().CurrentPhase != GameManager.Phase.Playing)
        {
            SpectatorModeSystem.ToggleSpectateMode(player.whoAmI);
            return;
        }

        if (SpectateCommandHelper.LooksLikeDragonLensAdmin(player))
        {
            SpectatorModeSystem.ToggleSpectateMode(player.whoAmI);
            return;
        }

        SpectatorConfig config = ModContent.GetInstance<SpectatorConfig>();

        if (!config.AllowSpectating)
        {
            Main.NewText("Spectating is disabled on this server.", Color.OrangeRed);
            return;
        }

        if (config.ForceSpectating)
        {
            Main.NewText("Cannot change spectate status (Force Spectate Mode is on).", Color.OrangeRed);
            return;
        }

        SpectatorModeSystem.ToggleSpectateMode(player.whoAmI);
    }
}

internal static class SpectateCommandHelper
{
    public static bool LooksLikeDragonLensAdmin(Player player)
    {
        try
        {
            Type type = Type.GetType("DragonLens.Core.Systems.PermissionHandler, DragonLens");

            if (type == null)
                return false;

            MethodInfo method = type.GetMethod("LooksLikeAdmin", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            if (method == null)
                return false;

            return method.Invoke(null, [player]) is true;
        }
        catch
        {
            return false;
        }
    }
}