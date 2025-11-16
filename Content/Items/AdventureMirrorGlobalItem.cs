using PvPAdventure.Core.Features.SpawnSelector.Players;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Content.Items
{
    public class AdventureMirrorGlobalItem : GlobalItem
    {
        public override bool InstancePerEntity => true;

        // Force the item to always be favorited
        public override void UpdateInventory(Item item, Player player)
        {
            if (item.type == ModContent.ItemType<AdventureMirror>())
            {
                item.favorited = true; // Force on every tick
            }
        }

        public override bool CanPickup(Item item, Player player)
        {
            // Disallow item pickup while timer is on
            if (player.GetModPlayer<AdventureMirrorPlayer>().MirrorTimer > 0)
            {
                return false;
            }

            return base.CanPickup(item, player);
        }
    }
}