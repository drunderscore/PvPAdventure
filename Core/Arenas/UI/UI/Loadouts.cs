using System.Collections.Generic;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;

namespace PvPAdventure.Core.Arenas.UI.LoadoutUI;

/// <summary>
/// Define loadouts here.
/// Add them to UI in
/// <see cref="ArenasLoadoutUI.AddLoadouts(UIList)()"/>
/// </summary>
public static class Loadouts
{
    public static readonly Loadout Melee =
        new()
        {
            Name = "Melee",
            Head = ItemID.BeetleHelmet,
            Body = ItemID.BeetleScaleMail,
            Legs = ItemID.BeetleLeggings,
            Accessories =
            {
                ItemID.BetsyWings,
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
                new LoadoutItem(ItemID.Dynamite, 60),
                new LoadoutItem(ItemID.HoneyBucket),
                new LoadoutItem(ItemID.GrayBrick, 999),
                new LoadoutItem(ItemID.WoodPlatform, 999),
                new LoadoutItem(ItemID.Binoculars),
            },
            GrapplingHook = ItemID.ChristmasHook
        };

    public static readonly Loadout Ranger =
        new()
        {
            Name = "Ranger",
            Head = ItemID.ShroomiteHeadgear,
            Body = ItemID.ShroomiteBreastplate,
            Legs = ItemID.ShroomiteLeggings,
            Accessories =
            {
                ItemID.BetsyWings,
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
                new LoadoutItem(ItemID.Dynamite, 60),
                new LoadoutItem(ItemID.HoneyBucket),
                new LoadoutItem(ItemID.GrayBrick, 999),
                new LoadoutItem(ItemID.BorealWoodPlatform, 999),
                new LoadoutItem(ItemID.Binoculars),
            },
            GrapplingHook = ItemID.LunarHook
        };

    public static readonly Loadout Mage =
        new()
        {
            Name = "Mage",
            Head = ItemID.SpectreHood,
            Body = ItemID.SpectreRobe,
            Legs = ItemID.SpectrePants,
            Accessories =
            {
                ItemID.BetsyWings,
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
                new LoadoutItem(ItemID.Dynamite, 60),
                new LoadoutItem(ItemID.HoneyBucket),
                new LoadoutItem(ItemID.GrayBrick, 999),
                new LoadoutItem(ItemID.WoodPlatform, 999),
                new LoadoutItem(ItemID.Binoculars),
            },
            GrapplingHook = ItemID.SpookyHook
        };

    public static readonly Loadout Summoner =
        new()
        {
            Name = "Summoner",
            Head = ItemID.SpookyHelmet,
            Body = ItemID.SpookyBreastplate,
            Legs = ItemID.SpookyLeggings,
            Accessories =
            {
                ItemID.BetsyWings,
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
                new LoadoutItem(ItemID.Dynamite, 60),
                new LoadoutItem(ItemID.HoneyBucket),
                new LoadoutItem(ItemID.GrayBrick, 999),
                new LoadoutItem(ItemID.BorealWoodPlatform, 999),
                new LoadoutItem(ItemID.Binoculars),
            },
            GrapplingHook = ItemID.ChristmasHook
        };
}

public class Loadout
{
    public string Name;

    public int Head;
    public int Body;
    public int Legs;

    public List<int> Accessories = [];              // 0-6, max 6 items
    public List<LoadoutItem> Hotbar = [];            // 0–10, max 10 items
    public int GrapplingHook;
}

public readonly struct LoadoutItem(int type, int stack = 1)
{
    public readonly int Type = type;
    public readonly int Stack = stack;
}
