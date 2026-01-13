using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Core.SpawnAndSpectate.HoldingMap;

public static class MapHoldingNetHandler
{
    public static void HandlePacket(BinaryReader reader, int whoAmI)
    {
        var type = (MapHoldingPlayer.VisualsPacketType)reader.ReadByte();

        switch (type)
        {
            case MapHoldingPlayer.VisualsPacketType.MapHoldingState:
                {
                    int playerIndex = reader.ReadByte();
                    bool holding = reader.ReadBoolean();

                    if (Main.netMode == NetmodeID.Server)
                    {
                        // Anti-spoof: a client may only set its own state.
                        if (playerIndex != whoAmI)
                            return;

                        Main.player[playerIndex]
                            .GetModPlayer<MapHoldingPlayer>()
                            .HoldingMap = holding;

                        // Broadcast to all clients (including sender is fine; excluding sender is also fine).
                        ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
                        packet.Write((byte)AdventurePacketIdentifier.MapHolding);
                        packet.Write((byte)MapHoldingPlayer.VisualsPacketType.MapHoldingState);
                        packet.Write((byte)playerIndex);
                        packet.Write(holding);
                        packet.Send();
                    }
                    else
                    {
                        Main.player[playerIndex]
                            .GetModPlayer<MapHoldingPlayer>()
                            .HoldingMap = holding;
                    }

                    break;
                }
        }
    }
}
