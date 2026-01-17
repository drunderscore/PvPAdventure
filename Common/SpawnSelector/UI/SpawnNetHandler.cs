using System.IO;
using Terraria;
using Terraria.ID;
using static PvPAdventure.Common.SpawnSelector.SpawnSystem;

namespace PvPAdventure.Common.SpawnSelector.UI;

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
