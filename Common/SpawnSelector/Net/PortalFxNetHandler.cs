using Microsoft.Xna.Framework;
using PvPAdventure.Core.Net;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.SpawnSelector.Net;

public static class PortalFxNetHandler
{
    public static void Send(Vector2 worldPos, bool killed, int damage = 0, int toWho = -1, int ignoreClient = -1)
    {
        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            PortalSystem.PlayPortalFx(worldPos, killed, damage);
            return;
        }

        ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.PortalFx);
        packet.Write(worldPos.X);
        packet.Write(worldPos.Y);
        packet.Write(killed);
        packet.Write(damage);
        packet.Send(toWho, ignoreClient);
    }

    public static void HandlePacket(BinaryReader reader, int whoAmI)
    {
        Vector2 worldPos = new(reader.ReadSingle(), reader.ReadSingle());
        bool killed = reader.ReadBoolean();
        int damage = reader.ReadInt32();

        if (Main.netMode == NetmodeID.Server)
        {
            Send(worldPos, killed, damage, ignoreClient: whoAmI);
            return;
        }

        PortalSystem.PlayPortalFx(worldPos, killed, damage);
    }
}
