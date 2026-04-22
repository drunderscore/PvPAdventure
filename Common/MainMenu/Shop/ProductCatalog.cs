using PvPAdventure.Common.Skins;
using PvPAdventure.Core.Utilities;
using System.Collections.Generic;
using Terraria.ID;

namespace PvPAdventure.Common.MainMenu.Shop;

public static class ProductCatalog
{
    private static readonly Dictionary<SkinIdentity, ProductDefinition> ByIdentity = new()
    {
        [new(Prototype: "sniper_rifle", Name: "red")] = new(
            Prototype: "sniper_rifle",
            Name: "red",
            DisplayName: "Red Sniper Rifle",
            Description: "It's red, alright?",
            Price: 50,
            Texture: Ass.SniperRifleRed,
            ItemType: ItemID.SniperRifle),

        [new(Prototype: "sniper_rifle", Name: "blue")] = new(
            Prototype: "sniper_rifle",
            Name: "blue",
            DisplayName: "Blue Sniper Rifle",
            Description: "It's blue, alright?",
            Price: 50,
            Texture: Ass.SniperRifleBlue,
            ItemType: ItemID.SniperRifle),

        [new(Prototype: "sniper_rifle", Name: "ember")] = new("sniper_rifle", "ember", "Ember Sniper Rifle", "A heated finish for aggressive aim.", 50, Ass.SniperRifleRed, ItemID.SniperRifle),
        [new(Prototype: "sniper_rifle", Name: "frost")] = new("sniper_rifle", "frost", "Frost Sniper Rifle", "Cold, clean, and precise.", 50, Ass.SniperRifleBlue, ItemID.SniperRifle),
        [new(Prototype: "sniper_rifle", Name: "rose")] = new("sniper_rifle", "rose", "Rose Sniper Rifle", "A bright rose-tinted body.", 50, Ass.SniperRifleRed, ItemID.SniperRifle),
        [new(Prototype: "sniper_rifle", Name: "storm")] = new("sniper_rifle", "storm", "Storm Sniper Rifle", "Built for high-pressure plays.", 50, Ass.SniperRifleBlue, ItemID.SniperRifle),
        [new(Prototype: "sniper_rifle", Name: "scarlet")] = new("sniper_rifle", "scarlet", "Scarlet Sniper Rifle", "Sharp crimson lines across the barrel.", 50, Ass.SniperRifleRed, ItemID.SniperRifle),
        [new(Prototype: "sniper_rifle", Name: "azure")] = new("sniper_rifle", "azure", "Azure Sniper Rifle", "Deep blue plating with clean highlights.", 50, Ass.SniperRifleBlue, ItemID.SniperRifle),
        [new(Prototype: "sniper_rifle", Name: "lava")] = new("sniper_rifle", "lava", "Lava Sniper Rifle", "Glows like a forge at midnight.", 50, Ass.SniperRifleRed, ItemID.SniperRifle),
        [new(Prototype: "sniper_rifle", Name: "tidal")] = new("sniper_rifle", "tidal", "Tidal Sniper Rifle", "A slick ocean-inspired finish.", 50, Ass.SniperRifleBlue, ItemID.SniperRifle),
        [new(Prototype: "sniper_rifle", Name: "ruby")] = new("sniper_rifle", "ruby", "Ruby Sniper Rifle", "A polished red gemstone sheen.", 50, Ass.SniperRifleRed, ItemID.SniperRifle),
        [new(Prototype: "sniper_rifle", Name: "cobalt")] = new("sniper_rifle", "cobalt", "Cobalt Sniper Rifle", "Dark alloy blue with bright edges.", 50, Ass.SniperRifleBlue, ItemID.SniperRifle),

        [new(Prototype: "battle_blade", Name: "red")] = new("battle_blade", "red", "Red Battle Blade", "Cuts through the arena with style.", 50, Ass.SniperRifleRed, ItemID.BreakerBlade),
        [new(Prototype: "battle_blade", Name: "blue")] = new("battle_blade", "blue", "Blue Battle Blade", "A cool steel palette for duelists.", 50, Ass.SniperRifleBlue, ItemID.BreakerBlade),
        [new(Prototype: "battle_blade", Name: "ember")] = new("battle_blade", "ember", "Ember Battle Blade", "Hot streaks along the edge.", 50, Ass.SniperRifleRed, ItemID.BreakerBlade),
        [new(Prototype: "battle_blade", Name: "frost")] = new("battle_blade", "frost", "Frost Battle Blade", "Icy trim and a colder attitude.", 50, Ass.SniperRifleBlue, ItemID.BreakerBlade),
        [new(Prototype: "battle_blade", Name: "crimson")] = new("battle_blade", "crimson", "Crimson Battle Blade", "Heavy red finish with a war-ready look.", 50, Ass.SniperRifleRed, ItemID.BreakerBlade),
        [new(Prototype: "battle_blade", Name: "marine")] = new("battle_blade", "marine", "Marine Battle Blade", "Blue combat paint over a broad blade.", 50, Ass.SniperRifleBlue, ItemID.BreakerBlade),
        [new(Prototype: "battle_blade", Name: "inferno")] = new("battle_blade", "inferno", "Inferno Battle Blade", "Bright and loud, just like the fights.", 50, Ass.SniperRifleRed, ItemID.BreakerBlade),
        [new(Prototype: "battle_blade", Name: "glacier")] = new("battle_blade", "glacier", "Glacier Battle Blade", "Cool tones for calmer killers.", 50, Ass.SniperRifleBlue, ItemID.BreakerBlade)
    };

    public static IEnumerable<ProductDefinition> All => ByIdentity.Values;

    public static bool TryGet(SkinIdentity identity, out ProductDefinition definition)
    {
        return ByIdentity.TryGetValue(identity, out definition);
    }

    public static bool TryGet(string prototype, string name, out ProductDefinition definition)
    {
        return TryGet(new SkinIdentity(Prototype: prototype, Name: name), out definition);
    }
}
