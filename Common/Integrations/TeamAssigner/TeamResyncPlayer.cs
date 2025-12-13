namespace PvPAdventure.Common.Integrations.TeamAssigner;

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

/// <summary>
/// I really dislike having to do it this way,
/// but this hotfix ensures team resyncs properly when players join.
/// </summary>
public class TeamResyncPlayer : ModPlayer
{
    private int _resyncTimer;
    private bool _pendingResync;

    public override void OnEnterWorld()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            _pendingResync = true;
            _resyncTimer = 0;
        }
    }

    public override void PostUpdate()
    {
        if (!_pendingResync || Main.netMode != NetmodeID.MultiplayerClient)
            return;

        // wait 0.5s
        if (++_resyncTimer < 30)
            return;

        _pendingResync = false;

        var packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.PlayerTeam);

        // Send current team to server
        new AdventurePlayer.Team((byte)Player.whoAmI, (Terraria.Enums.Team)Player.team).Serialize(packet);

        packet.Send();
    }
}

