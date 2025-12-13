using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Content.Items;
public class AdventureBag : ModItem
{
    public override string Texture => $"PvPAdventure/Assets/Item/AdventureBag";
    public override void SetStaticDefaults()
    {
        ItemID.Sets.OpenableBag[Type] = true;
    }

    public override void SetDefaults()
    {
        Item.width = 32;
        Item.height = 32;
        Item.rare = ItemRarityID.Orange;
        Item.maxStack = 1;
        Item.consumable = true;
    }

    public override bool CanRightClick()
    {
        return true;
    }

    public override void RightClick(Player player)
    {
        player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.SilverPickaxe, 1);
        player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.SilverAxe, 1);
        player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.Ambrosia, 1);
        player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.SlimeBed, 1);
        player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.Torch, 15);
        player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.Wood, 20);
        player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.LifeCrystal, 5);
        player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.ManaCrystal, 4);
        player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.MiningPotion, 2);
        player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.WormholePotion, 4);
        player.QuickSpawnItem(player.GetSource_OpenItem(Type), ModContent.ItemType<AdventureMirror>(), 1);
    }
    public override bool ConsumeItem(Player player)
    {
        return true;
    }
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        // Add custom tooltip line
        tooltips.Add(new TooltipLine(Mod, "PvPBagInfo", "Contains essential items for your PvP Adventure"));
    }
}
