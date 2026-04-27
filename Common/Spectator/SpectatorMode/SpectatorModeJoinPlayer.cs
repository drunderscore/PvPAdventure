using PvPAdventure.Common.Spectator.Net;
using PvPAdventure.Core.Config;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Spectator.SpectatorMode;

/// <summary>
/// Applies forced spectator mode after SSC has finished loading.
/// </summary>
public class SpectatorModeJoinPlayer : ModPlayer
{
    private int forceSpectatorDelayTicks;

    public void ScheduleForceSpectating()
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        var spectatorConfig = ModContent.GetInstance<SpectatorConfig>();
        if (!spectatorConfig.ForceSpectating)
            return;

        forceSpectatorDelayTicks = 30;
        Main.LocalPlayer.ghost = true;

        Log.Chat($"Force spectating is enabled, will send request to become a spectator in 30 ticks for player id: {Player.whoAmI}");
    }

    public override void PostUpdate()
    {
        if (forceSpectatorDelayTicks <= 0)
            return;

        Main.LocalPlayer.ghost = true;

        forceSpectatorDelayTicks--;

        if (forceSpectatorDelayTicks > 0)
            return;

        Log.Chat($"Sending request to become a spectator for player id: {Main.myPlayer}");
        SpectatorModeNetHandler.SendRequestSetMode(Main.myPlayer, PlayerMode.Spectator);
    }
}