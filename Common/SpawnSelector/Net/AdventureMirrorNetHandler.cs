using PvPAdventure.Content.Items;
using PvPAdventure.Core.Net;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.SpawnSelector.Net;

public static class AdventureMirrorNetHandler
{
    /// <summary>
    /// Handles an incoming network packet related to the Adventure Mirror item for the specified player.
    /// Updates the player's visual state to reflect item usage.
    /// Receives a packet containing the player ID and inventory slot of the Adventure Mirror being used.
    /// </summary>
    public static void HandlePacket(BinaryReader reader, int whoAmI)
    {
        byte playerId = reader.ReadByte();
        byte slot = reader.ReadByte();

        if (Main.netMode == NetmodeID.Server)
        {
            if (playerId != whoAmI)
                return;

            if (playerId < 0 || playerId >= Main.maxPlayers)
                return;

            Player player = Main.player[playerId];
            if (player is null || !player.active)
                return;

            if (slot < 0 || slot >= player.inventory.Length)
                return;

            Item item = player.inventory[slot];
            if (item?.ModItem is not AdventureMirror)
                return;

            ModPacket p = (ModPacket)ModContent.GetInstance<PvPAdventure>().GetPacket();
            p.Write((byte)AdventurePacketIdentifier.AdventureMirrorRightClickUse);
            p.Write(playerId);
            p.Write(slot);
            p.Send();
        }
        else if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            if (playerId == Main.myPlayer)
                return;

            Player player = Main.player[playerId];
            if (player is null || !player.active)
                return;

            if (slot < 0 || slot >= player.inventory.Length)
                return;

            Item item = player.inventory[slot];
            if (item?.ModItem is not AdventureMirror)
                return;

            // Visual state only
            player.selectedItem = slot;
            player.itemAnimation = item.useAnimation;
            player.itemAnimationMax = item.useAnimation;
            player.itemTime = item.useTime;
            player.itemTimeMax = item.useTime;
        }
    }
}
