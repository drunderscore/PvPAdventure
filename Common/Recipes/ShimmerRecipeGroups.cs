using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Recipes;
// - This is where we add "shimmer crafting" a system for mitigating bad RNG in-game
// - With each shimmer recipe, you add items to the RecipeGroup, and then when nearby the shimmer liquid, you are able to craft any item from that recipe group with X different items that are also from the recipe group
// - This system is still a little bit scuffed, as the IconItemID item isn't supposed to be a part of the recipes, but it is. Also, ideally, in-game, you should see which items you are using to craft, instead of just seeing the three group items, but that requires redoing the entire system.
public class RecipeGroupData
{
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public int IconItemID { get; set; }
    public int[] Items { get; set; }
    public int RecipeQuantity { get; set; }

    public RecipeGroupData(string name, string displayName, int iconItemID, int recipeQuantity, params int[] items)
    {
        Name = name;
        DisplayName = displayName;
        IconItemID = iconItemID;
        Items = items;
        RecipeQuantity = recipeQuantity;
    }
}

public class UnifiedRecipeGroupSystem : ModSystem
{
    private static readonly List<RecipeGroupData> AllGroups = new List<RecipeGroupData>
    {
        // Boss drops
        new RecipeGroupData("AnyGolemPrimary", "Any Golem Primary", ItemID.GolemMasterTrophy, 3,
            ItemID.GolemMasterTrophy, ItemID.Stynger, ItemID.PossessedHatchet, ItemID.HeatRay, ItemID.GolemFist, ItemID.StaffofEarth),

        new RecipeGroupData("AnyGolemSecondary", "Any Golem Secondary", ItemID.GolemBossBag, 3,
            ItemID.GolemBossBag, ItemID.SunStone, ItemID.ShinyStone, ItemID.EyeoftheGolem, ItemID.Picksaw),

        new RecipeGroupData("AnyQueenSlimePrimary", "Any Queen Slime Primary", ItemID.QueenSlimeMasterTrophy, 2,
            ItemID.QueenSlimeMasterTrophy, ItemID.Smolstar, ItemID.QueenSlimeHook, ItemID.QueenSlimeMountSaddle),

        new RecipeGroupData("AnyPlanteraPrimary", "Any Plantera Drop", ItemID.PlanteraMasterTrophy, 2,
            ItemID.PlanteraMasterTrophy, ItemID.GrenadeLauncher, ItemID.NettleBurst, ItemID.FlowerPow, ItemID.VenusMagnum),

        new RecipeGroupData("AnyDukePrimary", "Any Duke Primary", ItemID.DukeFishronMasterTrophy, 3,
            ItemID.DukeFishronMasterTrophy, ItemID.RazorbladeTyphoon, ItemID.BubbleGun, ItemID.Flairon, ItemID.Tsunami, ItemID.TempestStaff),

        new RecipeGroupData("AnyEmpressPrimary", "Any Empress Primary", ItemID.FairyQueenMasterTrophy, 2,
            ItemID.FairyQueenMasterTrophy, ItemID.FairyQueenMagicItem, ItemID.FairyQueenRangedItem, ItemID.PiercingStarlight, ItemID.RainbowWhip, ItemID.EmpressBlade),

        new RecipeGroupData("AnyWallPrimary", "Any Wall of Flesh Primary", ItemID.WallofFleshMasterTrophy, 2,
            ItemID.WallofFleshMasterTrophy, ItemID.FireWhip, ItemID.ClockworkAssaultRifle, ItemID.BreakerBlade, ItemID.LaserRifle),

        new RecipeGroupData("AnySaucerPrimary", "Any Saucer Primary", ItemID.UFOMasterTrophy, 3,
            ItemID.UFOMasterTrophy, ItemID.XenoStaff, ItemID.LaserMachinegun, ItemID.InfluxWaver, ItemID.ElectrosphereLauncher, ItemID.Xenopopper),
        
        // Mimics
        new RecipeGroupData("AnyCorruptionMimicPrimary", "Any Corrupt Mimic Primary", ItemID.Fake_CorruptionChest, 2,
            ItemID.Fake_CorruptionChest, ItemID.DartRifle, ItemID.ClingerStaff, ItemID.ChainGuillotines),

        new RecipeGroupData("AnyHallowedMimicPrimary", "Any Hallowed Mimic Primary", ItemID.Fake_HallowedChest, 2,
            ItemID.Fake_HallowedChest, ItemID.DaedalusStormbow, ItemID.CrystalVileShard, ItemID.FlyingKnife),

        new RecipeGroupData("AnyMimicPrimary", "Any Mimic Primary", ItemID.TitanGlove, 3,
            ItemID.DeadMansChest, ItemID.TitanGlove, ItemID.CrossNecklace, ItemID.StarCloak, ItemID.PhilosophersStone, ItemID.MagicDagger, ItemID.DualHook),
        
        // Dungeon
        new RecipeGroupData("AnyBrickWall", "Any Brick Wall", ItemID.NecromanticSign, 3,
            ItemID.NecromanticSign, ItemID.ShadowbeamStaff, ItemID.RocketLauncher, ItemID.PaladinsHammer),
        
        // Chests
        new RecipeGroupData("AnyGold", "Any Gold Chest Item", ItemID.CloudinaBottle, 3,
            ItemID.CloudinaBottle, ItemID.HermesBoots, ItemID.FlareGun, ItemID.ShoeSpikes, ItemID.BandofRegeneration, ItemID.Mace),

        new RecipeGroupData("AnyJungle", "Any Jungle Chest Item", ItemID.AnkletoftheWind, 3,
            ItemID.AnkletoftheWind, ItemID.Boomstick, ItemID.StaffofRegrowth, ItemID.FlowerBoots, ItemID.FiberglassFishingPole, ItemID.FeralClaws),

        new RecipeGroupData("AnyHighDesert", "Any High Desert Chest Item", ItemID.PharaohsMask, 2,
            ItemID.PharaohsMask, ItemID.MysticCoilSnake, ItemID.SandBoots, ItemID.AncientChisel),

        new RecipeGroupData("AnyLowDesert", "Any Low Desert Chest Item", ItemID.PharaohsRobe, 2,
            ItemID.PharaohsRobe, ItemID.SandstorminaBottle, ItemID.ThunderSpear, ItemID.ThunderStaff),

        new RecipeGroupData("AnyIce", "Any Ice Chest Item", ItemID.SnowballLauncher, 2,
            ItemID.SnowballLauncher, ItemID.FlurryBoots, ItemID.BlizzardinaBottle, ItemID.SnowballCannon, ItemID.IceSkates, ItemID.IceBlade, ItemID.IceBoomerang, ItemID.Fish),

        new RecipeGroupData("AnySky", "Any Sky Chest Item", ItemID.CreativeWings, 2,
            ItemID.CreativeWings, ItemID.ShinyRedBalloon, ItemID.Starfury, ItemID.CelestialMagnet, ItemID.LuckyHorseshoe),
    };

    public override void AddRecipeGroups()
    {
        foreach (var groupData in AllGroups)
        {
            RecipeGroup mainGroup = new RecipeGroup(
                () => Language.GetTextValue(groupData.DisplayName),
                groupData.Items
            );
            RecipeGroup.RegisterGroup($"PvPAdventure:{groupData.Name}", mainGroup);

            foreach (int itemID in groupData.Items.Where(id => id != groupData.IconItemID))
            {
                var validItems = groupData.Items
                    .Where(id => id != itemID && id != groupData.IconItemID)
                    .Prepend(groupData.IconItemID)
                    .ToArray();

                RecipeGroup excludeGroup = new RecipeGroup(
                    () => Language.GetTextValue(groupData.DisplayName),
                    validItems
                );
                RecipeGroup.RegisterGroup($"PvPAdventure:{groupData.Name}Exclude{itemID}", excludeGroup);
            }
        }
    }
    public override void AddRecipes()
    {
        var shimmerCondition = new Condition(
            Language.GetText("Mods.PvPAdventure.Conditions.NearShimmer"),
            () => Main.LocalPlayer.adjShimmer
        );

        foreach (var groupData in AllGroups)
        {
            foreach (int itemID in groupData.Items.Where(id => id != groupData.IconItemID)) // IconItemID doesn't work completely as intended but it isn't a big deal
            {
                Recipe.Create(itemID)
                    .AddRecipeGroup($"PvPAdventure:{groupData.Name}Exclude{itemID}", groupData.RecipeQuantity)
                    .AddCondition(shimmerCondition)
                    .DisableDecraft()
                    .Register();
            }
        }
    }
}
