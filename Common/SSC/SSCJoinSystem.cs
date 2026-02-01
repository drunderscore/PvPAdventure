using Microsoft.Xna.Framework;
using PvPAdventure.Core.Config;
using PvPAdventure.Core.Net;
using Steamworks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static PvPAdventure.Common.SSC.SSC;

namespace PvPAdventure.Common.SSC;

/// <summary>
/// Joins the world as a ghost, 
/// and after a small delay sends a request to join as a proper SSC character.
/// Hopefully reworked in the future for smoother player experience.
/// </summary>
[Autoload(Side = ModSide.Client)]
public class SSCJoinSystem : ModSystem
{
    private bool _sent;
    private int _delayTicks;
    public override void OnWorldLoad()
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        if (!SSC.IsEnabled)
            return;

        _sent = false;
        _delayTicks = 30*1; // wait a second to ensure vanilla hooks run properly.
        // in the future, the delay may be 0 or we may use a hook that runs earlier for a smoother join experience.

        // Enter as a ghost initially
        Main.LocalPlayer.ghost = true;

#if DEBUG
        // DEBUG:
        // Be the local char for 5 seconds.
        // Some extra time to properly debug the SSC flow of joining with a local char.
        // Also disable ghost to see the real player.
        //_delayTicks = 60 * 3;
        //Main.LocalPlayer.ghost = false;
#endif
    }

    public override void PostUpdateEverything()
    {
        if (_sent)
            return;

        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        if (_delayTicks > 0)
        {
            if (_delayTicks % 60 == 0)
            {
                Log.Chat("Joining in ticks: " + _delayTicks);
            }
            _delayTicks--;
            return;
        }

        _sent = true;

        // Join immediately if arenas is off.
        var config = ModContent.GetInstance<ArenasConfig>();
        if (!config.IsArenasEnabled)
        {
            SendJoinRequest();
        }
    }

    public override void OnWorldUnload()
    {
        _sent = false;
        _delayTicks = 0;
    }

    public static void SendJoinRequest()
    {
        if (!SSC.IsEnabled)
            return;

        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        Player player = Main.LocalPlayer;

        var packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.SSC);
        packet.Write((byte)SSCPacketType.ClientJoin);

        packet.Write(SteamUser.GetSteamID().m_SteamID.ToString());
        packet.Write(player.name);

        // Appearance
        Appearance.WriteAppearence(packet, player);

        packet.Send();
    }
}
