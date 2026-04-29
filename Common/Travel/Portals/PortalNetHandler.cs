using PvPAdventure.Content.Portals;
using PvPAdventure.Core.Net;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Travel.Portals;

public static class PortalNetHandler
{
    private enum PortalPacketType : byte
    {
        PortalCreatorUse
    }

    public static void HandlePacket(BinaryReader reader, int whoAmI)
    {
        PortalPacketType type = (PortalPacketType)reader.ReadByte();

        switch (type)
        {
            case PortalPacketType.PortalCreatorUse:
                ReceivePortalCreatorUse(reader, whoAmI);
                break;

            default:
                Log.Warn($"[Portal] Unknown packet type={(byte)type}");
                break;
        }
    }

    public static void SendPortalCreatorUse(int slot)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.UsePortal);
        packet.Write((byte)PortalPacketType.PortalCreatorUse);
        packet.Write((byte)Main.myPlayer);
        packet.Write((byte)slot);
        packet.Send();
    }

    private static void ReceivePortalCreatorUse(BinaryReader reader, int whoAmI)
    {
        byte playerId = reader.ReadByte();
        byte slot = reader.ReadByte();

        if (Main.netMode == NetmodeID.Server)
        {
            if (playerId != whoAmI || !TryGetPortalCreator(playerId, slot, out Player player, out _))
                return;

            if (TravelRegionSystem.IsInTravelRegion(player))
                return;

            Log.Chat("Portal creation request received");
            PortalSystem.StartPortalCreation(player);
            SendPortalCreatorUse(playerId, slot, whoAmI);
            return;
        }

        if (Main.netMode != NetmodeID.MultiplayerClient || playerId == Main.myPlayer)
            return;

        if (!TryGetRemotePlayer(playerId, slot, out Player remotePlayer, out Item item))
            return;

        PortalCreatorItem.SetPortalCreationTime(item);

        remotePlayer.selectedItem = slot;
        remotePlayer.itemAnimation = item.useAnimation;
        remotePlayer.itemAnimationMax = item.useAnimation;
        remotePlayer.itemTime = item.useTime;
        remotePlayer.itemTimeMax = item.useTime;
    }

    private static void SendPortalCreatorUse(int playerId, int slot, int ignoreClient)
    {
        ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.UsePortal);
        packet.Write((byte)PortalPacketType.PortalCreatorUse);
        packet.Write((byte)playerId);
        packet.Write((byte)slot);
        packet.Send(ignoreClient: ignoreClient);
    }

    private static bool TryGetPortalCreator(int playerId, int slot, out Player player, out Item item)
    {
        player = null;
        item = null;

        if (playerId < 0 || playerId >= Main.maxPlayers || Main.player[playerId] is not { active: true } foundPlayer)
            return false;

        if (slot < 0 || slot >= foundPlayer.inventory.Length)
            return false;

        item = foundPlayer.inventory[slot];

        if (item?.ModItem is not PortalCreatorItem)
            return false;

        player = foundPlayer;
        return true;
    }

    private static bool TryGetRemotePlayer(int playerId, int slot, out Player player, out Item item)
    {
        player = null;
        item = null;

        if (playerId < 0 || playerId >= Main.maxPlayers || Main.player[playerId] is not { active: true } foundPlayer)
            return false;

        if (slot < 0 || slot >= foundPlayer.inventory.Length)
            return false;

        player = foundPlayer;
        item = foundPlayer.inventory[slot];

        if (item?.ModItem is not PortalCreatorItem)
            item.SetDefaults(ModContent.ItemType<PortalCreatorItem>());

        return true;
    }
}