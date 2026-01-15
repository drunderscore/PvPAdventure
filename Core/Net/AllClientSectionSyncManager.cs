using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Net;

[Autoload(Side = ModSide.Server)]
public class AllClientSectionSyncManager : ModSystem
{
    private bool _broadcasting;

    public override void Load()
    {
        On_NetMessage.SendSection += OnNetMessageSendSection;
    }

    private void OnNetMessageSendSection(On_NetMessage.orig_SendSection orig, int whoami, int sectionx, int sectiony)
    {
        if (!_broadcasting)
        {
            try
            {
                _broadcasting = true;
                foreach (var client in Netplay.Clients)
                {
                    if (client.Id == whoami || !client.IsActive || client.State != 10)
                        continue;

                    NetMessage.SendSection(client.Id, sectionx, sectiony);
                }
            }
            finally
            {
                _broadcasting = false;
            }
        }

        orig(whoami, sectionx, sectiony);
    }

    public override bool HijackGetData(ref byte messageType, ref BinaryReader reader, int playerNumber)
    {
        if (messageType != MessageID.SpawnTileData)
            return false;

        // Player is asking for spawn data to prepare to spawn. We'll also send them approx. sections for all the other
        // players.

        try
        {
            _broadcasting = true;
            foreach (var player in Main.ActivePlayers)
            {
                if (player.whoAmI == playerNumber)
                    continue;

                RemoteClient.CheckSection(playerNumber, player.position);
            }
        }
        finally
        {
            _broadcasting = false;
        }

        return false;
    }
}