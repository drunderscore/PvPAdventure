using PvPAdventure.Core.Net;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Game.EndScreen;

/// <summary>Sends end screen snapshots to team clients.</summary>
public static class EndScreenNetHandler
{
    public static void HandlePacket(BinaryReader reader, int whoAmI)
    {
        if (Main.netMode == NetmodeID.Server)
            return;

        EndScreenSnapshot snapshot = EndScreenSnapshot.Deserialize(reader);
        ModContent.GetInstance<EndScreenSystem>().ShowSnapshot(snapshot);
    }

    public static void SendSnapshot(EndScreenSnapshot snapshot, int toClient)
    {
        if (Main.netMode != NetmodeID.Server || snapshot == null)
            return;

        ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.EndScreen);
        snapshot.Serialize(packet);
        packet.Send(toClient); // team-filtered by caller
    }
}
