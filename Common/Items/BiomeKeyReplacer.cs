using PvPAdventure.Content.Items.BiomeKeyMolds;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Items;
/// <summary>
/// -  Checks when a list of relelvant "Biome Key" items are spawned using OnSpawn <br/>
/// -  Replaces relevant "Biome Key" items if they are "dropped" by a non-player <br/>
/// - They are replaced with our ModItems, the BiomeKeyMold counterparts <br/>
/// - This emulates pre-1.3 behavior, where enemies dropped biome key molds instead of the keys outright <br/>
/// </summary>
public class BiomeKeyReplacer : GlobalItem
{
    public override void OnSpawn(Item item, IEntitySource source)
    {
        switch (item.type)
        {
            case ItemID.CorruptionKey:
                item.SetDefaults(ModContent.ItemType<CorruptionKeyMold>());
                break;
            case ItemID.HallowedKey:
                item.SetDefaults(ModContent.ItemType<HallowedKeyMold>());
                break;
            case ItemID.FrozenKey:
                item.SetDefaults(ModContent.ItemType<FrozenKeyMold>());
                break;
            case ItemID.JungleKey:
                item.SetDefaults(ModContent.ItemType<JungleKeyMold>());
                break;
            case ItemID.DungeonDesertKey:
                item.SetDefaults(ModContent.ItemType<DesertKeyMold>());
                break;
        }

        if (item.type == ModContent.ItemType<CorruptionKeyMold>() ||
            item.type == ModContent.ItemType<HallowedKeyMold>() ||
            item.type == ModContent.ItemType<FrozenKeyMold>() ||
            item.type == ModContent.ItemType<JungleKeyMold>() ||
            item.type == ModContent.ItemType<DesertKeyMold>())
        {
            item.stack = 1;
        }
    }
}
