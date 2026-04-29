using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.World;
/// <summary>
/// Changes the loot table of breaking a Shadow Orb to always give a weapon and give the other drops in a secondary pool.
/// </summary>
public class ShadowOrbLoot : ModSystem
{
    private static readonly int[] Pool1Weapons = { ItemID.BallOHurt, ItemID.Vilethorn, ItemID.Musket };
    private static readonly int[] Pool2Accessories = { ItemID.ShadowOrb, ItemID.BandofStarpower };

    public override void Load()
    {
        On_Item.NewItem_IEntitySource_int_int_int_int_int_int_bool_int_bool_bool += OnNewItem;
    }

    public override void Unload()
    {
        On_Item.NewItem_IEntitySource_int_int_int_int_int_int_bool_int_bool_bool -= OnNewItem;
    }

    private static int OnNewItem(
        On_Item.orig_NewItem_IEntitySource_int_int_int_int_int_int_bool_int_bool_bool orig,
        IEntitySource source, int x, int y, int width, int height,
        int type, int stack, bool noBroadcast, int prefixGiven, bool noGrabDelay, bool reverseLookup)
    {
        // If vanilla is dropping a Band of Starpower or Shadow Orb light pet from a tile break...
        if ((type == ItemID.BandofStarpower || type == ItemID.ShadowOrb)
            && source is EntitySource_TileBreak
            && Main.netMode != NetmodeID.MultiplayerClient)
        {
            // Replace it with a random Pool 1 weapon
            int replacement = Main.rand.Next(Pool1Weapons);
            int result = orig(source, x, y, width, height, replacement, stack, noBroadcast, prefixGiven, noGrabDelay, reverseLookup);

            // If the weapon was a Musket, also drop 100 Musket Balls
            if (replacement == ItemID.Musket)
                orig(source, x, y, width, height, ItemID.MusketBall, 100, noBroadcast, 0, noGrabDelay, reverseLookup);

            // 40% chance for a Pool 2 secondary drop
            if (Main.rand.NextFloat() < 0.4f)
            {
                int pool2Type = Pool2Accessories[Main.rand.Next(Pool2Accessories.Length)];
                orig(source, x, y, width, height, pool2Type, 1, noBroadcast, 0, noGrabDelay, reverseLookup);
            }

            return result;
        }

        return orig(source, x, y, width, height, type, stack, noBroadcast, prefixGiven, noGrabDelay, reverseLookup);
    }
}