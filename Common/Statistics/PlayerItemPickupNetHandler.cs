using PvPAdventure.Common.Bounties;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Statistics;

public static class PlayerItemPickupNetHandler
{
    public static void HandlePacket(BinaryReader reader, int whoAmI)
    {
        var itemPickup = StatisticsPlayer.ItemPickup.Deserialize(reader);

        if (Main.netMode != NetmodeID.Server)
        {
            return;
        }

        var player = Main.player[whoAmI];
        if (player == null || !player.active)
        {
            return;
        }

        itemPickup.Apply(player.GetModPlayer<StatisticsPlayer>());

        ModContent.GetInstance<BountyManager>()
            .OnPlayerItemPickupsUpdated(player, itemPickup.Items.ToHashSet());
    }
}
