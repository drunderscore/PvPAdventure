using System.IO;
using Terraria;
using Terraria.ID;

namespace PvPAdventure.Common.SpawnSelector.Net;

public static class SpawnNetHandler
{
    public static void HandlePacket(BinaryReader reader, int whoAmI)
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        var type = (SpawnType)reader.ReadByte();
        short idx = reader.ReadInt16();

        Player p = Main.player[whoAmI];
        if (p != null && p.active)
            p.GetModPlayer<SpawnPlayer>().ApplySelectionFromNet(type, idx);
    }
}
