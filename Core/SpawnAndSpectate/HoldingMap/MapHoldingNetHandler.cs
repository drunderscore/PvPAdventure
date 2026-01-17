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
                        // whoAmI is the sender's network connection slot on server.
                        string senderName = Main.player[playerIndex].name;

                        if (playerIndex != whoAmI)
                        {
                            return;
                        }

                        // Update ModPlayer's map holding state.
                        Main.player[playerIndex].GetModPlayer<MapHoldingPlayer>().HoldingMap = holding;

                        Log.Debug($"[HoldingMap] Server set state: {senderName} holding={holding}. Broadcasting...");

                        // Send packet to all clients except the sender connection.
                        ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
                        packet.Write((byte)AdventurePacketIdentifier.HoldingMap);
                        packet.Write((byte)MapHoldingPlayer.VisualsPacketType.MapHoldingState);
                        packet.Write((byte)playerIndex);
                        packet.Write(holding);

                        packet.Send(toClient: -1, ignoreClient: whoAmI);
                    }
                    else
                    {
                        string fromName = Main.player[playerIndex].name;

                        Log.Chat("Set " + fromName + " (client) map holding state to: " + holding);
                        // Update ModPlayer's map holding state.
                        Main.player[playerIndex].GetModPlayer<MapHoldingPlayer>().HoldingMap = holding;
                    }

                    break;
                }
        }
    }
}
