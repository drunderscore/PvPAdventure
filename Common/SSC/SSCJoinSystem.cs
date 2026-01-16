using PvPAdventure.Core.Debug;
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
        _delayTicks = 60; // 1 second

        // Enter as a ghost initially
        Main.LocalPlayer.ghost = true;
    }

    public override void PostUpdateEverything()
    {
        if (_sent)
            return;

        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        if (_delayTicks > 0)
        {
            _delayTicks--;
            return;
        }

        _sent = true;
        SendJoinRequest();
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

        Log.Chat("send 2");

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
