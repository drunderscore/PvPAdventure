using Terraria.ID;
using Terraria.GameContent.UI.Elements;

namespace PvPAdventure.Core.Arenas.UI.LoadoutUI;

/// <summary>
/// Define loadouts here.
/// Add them to UI in
/// <see cref="ArenasLoadoutUI.AddLoadouts(UIList)()"/>
/// </summary>
public static class Loadouts
{
    private static readonly int Wings = ItemID.BetsyWings;

    public static readonly LoadoutDef Melee =
        new()
        {
            Name = "Melee",
            Head = ItemID.BeetleHelmet,
            Body = ItemID.BeetleScaleMail,
            Legs = ItemID.BeetleLeggings,
            Accessories =
            {
                Wings,
                ItemID.WarriorEmblem,
                ItemID.MechanicalGlove,
                ItemID.AnkhShield,
                ItemID.Tabi
            },
            Hotbar =
            {
                new LoadoutItem(ItemID.TerraBlade),
                new LoadoutItem(ItemID.TheEyeOfCthulhu),
                new LoadoutItem(ItemID.IceRod),
                new LoadoutItem(ItemID.GreaterHealingPotion, 10),
            },
            EquipmentHook = ItemID.ChristmasHook
        };

    public static readonly LoadoutDef Ranger =
        new()
        {
            Name = "Ranger",
            Head = ItemID.ShroomiteHeadgear,
            Body = ItemID.ShroomiteBreastplate,
            Legs = ItemID.ShroomiteLeggings,
            Accessories =
            {
                Wings,
                ItemID.RangerEmblem,
                ItemID.MagicQuiver,
                ItemID.AnkhShield,
                ItemID.Tabi
            },
            Hotbar =
            {
                new LoadoutItem(ItemID.ChainGun),
                new LoadoutItem(ItemID.ChlorophyteBullet, 9999),
                new LoadoutItem(ItemID.IceRod),
                new LoadoutItem(ItemID.GreaterHealingPotion, 10),
            },
            EquipmentHook = ItemID.LunarHook
        };

    public static readonly LoadoutDef Mage =
        new()
        {
            Name = "Mage",
            Head = ItemID.SpectreHood,
            Body = ItemID.SpectreRobe,
            Legs = ItemID.SpectrePants,
            Accessories =
            {
                Wings,
                ItemID.SorcererEmblem,
                ItemID.CelestialCuffs,
                ItemID.AnkhShield,
                ItemID.Tabi
            },
            Hotbar =
            {
                new LoadoutItem(ItemID.RazorbladeTyphoon),
                new LoadoutItem(ItemID.ManaPotion, 20),
                new LoadoutItem(ItemID.IceRod),
                new LoadoutItem(ItemID.GreaterHealingPotion, 10),
            },
            EquipmentHook = ItemID.SpookyHook
        };

    public static readonly LoadoutDef Summoner =
        new()
        {
            Name = "Summoner",
            Head = ItemID.SpookyHelmet,
            Body = ItemID.SpookyBreastplate,
            Legs = ItemID.SpookyLeggings,
            Accessories =
            {
                Wings,
                ItemID.SummonerEmblem,
                ItemID.PapyrusScarab,
                ItemID.AnkhShield,
                ItemID.Tabi
            },
            Hotbar =
            {
                new LoadoutItem(ItemID.XenoStaff),
                new LoadoutItem(ItemID.MaceWhip),
                new LoadoutItem(ItemID.IceRod),
                new LoadoutItem(ItemID.GreaterHealingPotion, 10),
            },
            EquipmentHook = ItemID.ChristmasHook
        };
}


