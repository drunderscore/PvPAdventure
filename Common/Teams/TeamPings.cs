using System.IO;
using Terraria;
using Terraria.GameContent.NetModules;
using Terraria.ModLoader;
using Terraria.Net;

namespace PvPAdventure.Common.Teams;

internal class TeamPings : ModSystem
{
    public override void Load()
    {
        if (Main.dedServ)
            // Only send world map pings to teammates.
            On_NetPingModule.Deserialize += OnNetPingModuleDeserialize;
    }

    // NOTE: This should only ever be applied to the server.
    private bool OnNetPingModuleDeserialize(On_NetPingModule.orig_Deserialize orig, NetPingModule self,
        BinaryReader reader, int userid)
    {
        var position = reader.ReadVector2();
        var packet = NetPingModule.Serialize(position);

        var senderTeam = (Terraria.Enums.Team)Main.player[userid].team;

        foreach (var client in Netplay.Clients)
        {
            if (!client.IsActive)
                continue;

            var player = Main.player[client.Id];
            if (!player.active || player.team == (int)Terraria.Enums.Team.None || player.team == (int)senderTeam)
                NetManager.Instance.SendToClient(packet, client.Id);
        }

        return true;
    }
}
