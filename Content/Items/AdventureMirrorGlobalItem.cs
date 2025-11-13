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

    }
}