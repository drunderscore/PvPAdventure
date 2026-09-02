using System.IO;
using Microsoft.Xna.Framework;
using PvPAdventure.Core.Net;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.NPCs;

internal static class ShakingChestNetHandler
{
    public static void HandlePacket(BinaryReader reader)
    {
        Vector2 position = new(reader.ReadSingle(), reader.ReadSingle());
        int width = reader.ReadInt16();
        int height = reader.ReadInt16();
        Vector2 velocity = new(reader.ReadSingle(), reader.ReadSingle());

        if (Main.netMode == NetmodeID.MultiplayerClient)
            ShakingChestNPC.PlayDisappearFx(position, width, height, velocity);
    }

    public static void SendDisappearFx(NPC npc)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            ShakingChestNPC.PlayDisappearFx(npc.position, npc.width, npc.height, npc.velocity);
            return;
        }

        ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.ShakingChest);
        packet.Write(npc.position.X);
        packet.Write(npc.position.Y);
        packet.Write((short)npc.width);
        packet.Write((short)npc.height);
        packet.Write(npc.velocity.X);
        packet.Write(npc.velocity.Y);
        packet.Send();
    }
}
