using PvPAdventure.Core.Arenas.UI;
using PvPAdventure.Core.Net;
using SubworldLibrary;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Arenas;

public static class ArenaPlayerCountNet
{
    private static int cachedCount = -1;
    public static void ServerUpdate()
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        if (!SubworldSystem.IsActive<ArenasSubworld>())
        {
            if (cachedCount != 0)
            {
                cachedCount = 0;
                Broadcast(0);
            }
            return;
        }

        int count = 0;

        for (int i = 0; i < Main.maxPlayers; i++)
            if (Main.player[i]?.active == true)
                count++;

        if (count == cachedCount)
            return;

        cachedCount = count;
        Broadcast(count);
    }
    private static void Broadcast(int count)
    {
        var packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.ArenaPlayerCount);
        packet.Write((byte)count);
        packet.Send();
    }

    public static void Receive(BinaryReader reader)
    {
        ArenasJoinUI.SetPlayerCount(reader.ReadByte());
    }
}

public class ArenaCountSystem : ModSystem
{
    public override void PostUpdatePlayers()
    {
        ArenaPlayerCountNet.ServerUpdate();
    }
}
