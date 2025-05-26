using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PvPAdventure.System;

[Autoload(Side = ModSide.Both)]
public class RecipeManager : ModSystem
{

    public override void AddRecipes()
    {

        Recipe.Create(ItemID.Headstone)
            .AddIngredient(ItemID.StoneBlock, 50)
            .AddTile(TileID.HeavyWorkBench)
            .Register();

        int[] itemsToRemove = new int[]
    {
            ItemID.TrueNightsEdge,
            ItemID.MoonlordArrow
    };

        for (int i = 0; i < Main.recipe.Length; i++)
        {
            Recipe recipe = Main.recipe[i];
            if (recipe.createItem.type != ItemID.None && itemsToRemove.Contains(recipe.createItem.type))
            {
                recipe.DisableRecipe();
            }
        }

        //temp sudo terrablade
        Recipe.Create(ItemID.TrueNightsEdge)
            .AddIngredient(ItemID.SoulofFright, 20)
            .AddIngredient(ItemID.SoulofMight, 20)
            .AddIngredient(ItemID.SoulofSight, 20)
            .AddIngredient(ItemID.NightsEdge)
            .AddIngredient(ItemID.TrueExcalibur)
            .AddIngredient(ItemID.BrokenHeroSword)
            .AddTile(TileID.MythrilAnvil)
            .Register();


    }

    public class AnyGolem1 : ModSystem
    {
        public static RecipeGroup AnyGolemPrimary;
        public static int[] PrimaryItems => new int[] {
            ItemID.GolemMasterTrophy,
            ItemID.Stynger,
            ItemID.PossessedHatchet,
            ItemID.HeatRay,
            ItemID.GolemFist,
            ItemID.StaffofEarth
        };

        public override void AddRecipeGroups()
        {
            // Main group with trophy as first item (for icon)
            AnyGolemPrimary = new RecipeGroup(() => Language.GetTextValue("Any Golem Primary"), PrimaryItems);
            RecipeGroup.RegisterGroup("PvPAdventure:AnyGolemPrimary", AnyGolemPrimary);

            // Create exclude subgroups while maintaining trophy as first item
            foreach (int itemID in PrimaryItems.Where(id => id != ItemID.GolemMasterTrophy))
            {
                var validItems = PrimaryItems
                    .Where(id => id != itemID && id != ItemID.GolemMasterTrophy)
                    .Prepend(ItemID.GolemMasterTrophy) // Keep trophy first for icon
                    .ToArray();

                RecipeGroup group = new RecipeGroup(
                    () => Language.GetTextValue("Any Golem Primary"),
                    validItems
                );
                RecipeGroup.RegisterGroup($"PvPAdventure:AnyGolemPrimaryExclude{itemID}", group);
            }
        }
    }

    public class AnyGolem2 : ModSystem
    {
        public static RecipeGroup AnyGolemSecondary;
        public static int[] SecondaryItems => new int[] {
            ItemID.GolemBossBag,
            ItemID.SunStone,
            ItemID.ShinyStone,
            ItemID.EyeoftheGolem,
            ItemID.Picksaw
        };

        public override void AddRecipeGroups()
        {
            // Main group with boss bag as first item (for icon)
            AnyGolemSecondary = new RecipeGroup(() => Language.GetTextValue("Any Golem Secondary"), SecondaryItems);
            RecipeGroup.RegisterGroup("PvPAdventure:AnyGolemSecondary", AnyGolemSecondary);

            // Create exclude subgroups while maintaining boss bag as first item
            foreach (int itemID in SecondaryItems.Where(id => id != ItemID.GolemBossBag))
            {
                var validItems = SecondaryItems
                    .Where(id => id != itemID && id != ItemID.GolemBossBag)
                    .Prepend(ItemID.GolemBossBag) // Keep boss bag first for icon
                    .ToArray();

                RecipeGroup group = new RecipeGroup(
                    () => Language.GetTextValue("Any Golem Secondary"),
                    validItems
                );
                RecipeGroup.RegisterGroup($"PvPAdventure:AnyGolemSecondaryExclude{itemID}", group);
            }
        }
    }

    public class AnyQueenSlime1 : ModSystem
    {
        public static RecipeGroup AnyQueenSlimePrimary;
        public static int[] PrimaryItems => new int[] {
            ItemID.QueenSlimeMasterTrophy,
            ItemID.Smolstar,
            ItemID.QueenSlimeHook,
            ItemID.QueenSlimeMountSaddle,
        };

        public override void AddRecipeGroups()
        {
            // Main group with trophy as first item (for icon)
            AnyQueenSlimePrimary = new RecipeGroup(() => Language.GetTextValue("Any Queen Slime Primary"), PrimaryItems);
            RecipeGroup.RegisterGroup("PvPAdventure:AnyQueenSlimePrimary", AnyQueenSlimePrimary);

            // Create exclude subgroups while maintaining icon as first item
            foreach (int itemID in PrimaryItems.Where(id => id != ItemID.QueenSlimeMasterTrophy))
            {
                var validItems = PrimaryItems
                    .Where(id => id != itemID && id != ItemID.QueenSlimeMasterTrophy)
                    .Prepend(ItemID.QueenSlimeMasterTrophy) // Keep item first for icon
                    .ToArray();

                RecipeGroup group = new RecipeGroup(
                    () => Language.GetTextValue("Any Queen Slime Primary"),
                    validItems
                );
                RecipeGroup.RegisterGroup($"PvPAdventure:AnyQueenSlimePrimaryExclude{itemID}", group);
            }
        }
    }

    public class AnyPlantera1 : ModSystem
    {
        public static RecipeGroup AnyPlanteraPrimary;
        public static int[] PrimaryItems => new int[] {
            ItemID.PlanteraMasterTrophy,
            ItemID.GrenadeLauncher,
            ItemID.LeafBlower,
            ItemID.WaspGun,
            ItemID.NettleBurst,
            ItemID.FlowerPow,
            ItemID.VenusMagnum,
            ItemID.Seedler,
        };

        public override void AddRecipeGroups()
        {
            // Main group with trophy as first item (for icon)
            AnyPlanteraPrimary = new RecipeGroup(() => Language.GetTextValue("Any Plantera Drop"), PrimaryItems);
            RecipeGroup.RegisterGroup("PvPAdventure:AnyPlanteraPrimary", AnyPlanteraPrimary);

            // Create exclude subgroups while maintaining icon as first item
            foreach (int itemID in PrimaryItems.Where(id => id != ItemID.PlanteraMasterTrophy))
            {
                var validItems = PrimaryItems
                    .Where(id => id != itemID && id != ItemID.PlanteraMasterTrophy)
                    .Prepend(ItemID.PlanteraMasterTrophy) // Keep item first for icon
                    .ToArray();

                RecipeGroup group = new RecipeGroup(
                    () => Language.GetTextValue("Any Plantera Drop"),
                    validItems
                );
                RecipeGroup.RegisterGroup($"PvPAdventure:AnyPlanteraPrimaryExclude{itemID}", group);
            }
        }
    }

    public class AnyDuke1 : ModSystem
    {
        public static RecipeGroup AnyDukePrimary;
        public static int[] PrimaryItems => new int[] {
            ItemID.DukeFishronMasterTrophy,
            ItemID.RazorbladeTyphoon,
            ItemID.BubbleGun,
            ItemID.Flairon,
            ItemID.Tsunami,
            ItemID.TempestStaff,
        };

        public override void AddRecipeGroups()
        {
            // Main group with trophy as first item (for icon)
            AnyDukePrimary = new RecipeGroup(() => Language.GetTextValue("Any Duke Primary"), PrimaryItems);
            RecipeGroup.RegisterGroup("PvPAdventure:AnyDukePrimary", AnyDukePrimary);

            // Create exclude subgroups while maintaining icon as first item
            foreach (int itemID in PrimaryItems.Where(id => id != ItemID.DukeFishronMasterTrophy))
            {
                var validItems = PrimaryItems
                    .Where(id => id != itemID && id != ItemID.DukeFishronMasterTrophy)
                    .Prepend(ItemID.DukeFishronMasterTrophy) // Keep item first for icon
                    .ToArray();

                RecipeGroup group = new RecipeGroup(
                    () => Language.GetTextValue("Any Duke Primary"),
                    validItems
                );
                RecipeGroup.RegisterGroup($"PvPAdventure:AnyDukePrimaryExclude{itemID}", group);
            }
        }
    }

    public class AnyEmpress1 : ModSystem
    {
        public static RecipeGroup AnyEmpressPrimary;
        public static int[] PrimaryItems => new int[] {
            ItemID.FairyQueenMasterTrophy,
            ItemID.FairyQueenMagicItem,
            ItemID.FairyQueenRangedItem,
            ItemID.PiercingStarlight,
            ItemID.RainbowWhip,
            ItemID.EmpressBlade,
        };

        public override void AddRecipeGroups()
        {
            // Main group with trophy as first item (for icon)
            AnyEmpressPrimary = new RecipeGroup(() => Language.GetTextValue("Any Empress Primary"), PrimaryItems);
            RecipeGroup.RegisterGroup("PvPAdventure:AnyEmpressPrimary", AnyEmpressPrimary);

            // Create exclude subgroups while maintaining icon as first item
            foreach (int itemID in PrimaryItems.Where(id => id != ItemID.FairyQueenMasterTrophy))
            {
                var validItems = PrimaryItems
                    .Where(id => id != itemID && id != ItemID.FairyQueenMasterTrophy)
                    .Prepend(ItemID.FairyQueenMasterTrophy) // Keep item first for icon
                    .ToArray();

                RecipeGroup group = new RecipeGroup(
                    () => Language.GetTextValue("Any Empress Primary"),
                    validItems
                );
                RecipeGroup.RegisterGroup($"PvPAdventure:AnyEmpressPrimaryExclude{itemID}", group);
            }
        }
    }

    public class AnyWall1 : ModSystem
    {
        public static RecipeGroup AnyWallPrimary;
        public static int[] PrimaryItems => new int[] {
            ItemID.WallofFleshMasterTrophy,
            ItemID.FireWhip,
            ItemID.ClockworkAssaultRifle,
            ItemID.BreakerBlade,
            ItemID.LaserRifle,
        };

        public override void AddRecipeGroups()
        {
            // Main group with trophy as first item (for icon)
            AnyWallPrimary = new RecipeGroup(() => Language.GetTextValue("Any Wall of Flesh Primary"), PrimaryItems);
            RecipeGroup.RegisterGroup("PvPAdventure:AnyWallPrimary", AnyWallPrimary);

            // Create exclude subgroups while maintaining icon as first item
            foreach (int itemID in PrimaryItems.Where(id => id != ItemID.WallofFleshMasterTrophy))
            {
                var validItems = PrimaryItems
                    .Where(id => id != itemID && id != ItemID.WallofFleshMasterTrophy)
                    .Prepend(ItemID.WallofFleshMasterTrophy) // Keep item first for icon
                    .ToArray();

                RecipeGroup group = new RecipeGroup(
                    () => Language.GetTextValue("Any Wall of Flesh Primary"),
                    validItems
                );
                RecipeGroup.RegisterGroup($"PvPAdventure:AnyWallPrimaryExclude{itemID}", group);
            }
        }
    }

    public class AnySaucer1 : ModSystem
    {
        public static RecipeGroup AnySaucerPrimary;
        public static int[] PrimaryItems => new int[] {
            ItemID.UFOMasterTrophy,
            ItemID.XenoStaff,
            ItemID.LaserMachinegun,
            ItemID.InfluxWaver,
            ItemID.ElectrosphereLauncher,
            ItemID.Xenopopper,
        };

        public override void AddRecipeGroups()
        {
            // Main group with trophy as first item (for icon)
            AnySaucerPrimary = new RecipeGroup(() => Language.GetTextValue("Any Saucer Primary"), PrimaryItems);
            RecipeGroup.RegisterGroup("PvPAdventure:AnySaucerPrimary", AnySaucerPrimary);

            // Create exclude subgroups while maintaining icon as first item
            foreach (int itemID in PrimaryItems.Where(id => id != ItemID.UFOMasterTrophy))
            {
                var validItems = PrimaryItems
                    .Where(id => id != itemID && id != ItemID.UFOMasterTrophy)
                    .Prepend(ItemID.UFOMasterTrophy) // Keep item first for icon
                    .ToArray();

                RecipeGroup group = new RecipeGroup(
                    () => Language.GetTextValue("Any Saucer Primary"),
                    validItems
                );
                RecipeGroup.RegisterGroup($"PvPAdventure:AnySaucerPrimaryExclude{itemID}", group);
            }
        }
    }

    public class AnyCorruptionMimic1 : ModSystem
    {
        public static RecipeGroup AnyCorruptionMimicPrimary;
        public static int[] PrimaryItems => new int[] {
            ItemID.Fake_CorruptionChest,
            ItemID.PutridScent,
            ItemID.DartRifle,
            ItemID.ClingerStaff,
            ItemID.ChainGuillotines,
            ItemID.WormHook,
        };

        public override void AddRecipeGroups()
        {
            // Main group with trophy as first item (for icon)
            AnyCorruptionMimicPrimary = new RecipeGroup(() => Language.GetTextValue("Any Corrupt Mimic Primary"), PrimaryItems);
            RecipeGroup.RegisterGroup("PvPAdventure:AnyCorruptionMimicPrimary", AnyCorruptionMimicPrimary);

            // Create exclude subgroups while maintaining icon as first item
            foreach (int itemID in PrimaryItems.Where(id => id != ItemID.Fake_CorruptionChest))
            {
                var validItems = PrimaryItems
                    .Where(id => id != itemID && id != ItemID.Fake_CorruptionChest)
                    .Prepend(ItemID.Fake_CorruptionChest) // Keep item first for icon
                    .ToArray();

                RecipeGroup group = new RecipeGroup(
                    () => Language.GetTextValue("Any Corrupt Mimic Primary"),
                    validItems
                );
                RecipeGroup.RegisterGroup($"PvPAdventure:AnyCorruptionMimicPrimaryExclude{itemID}", group);
            }
        }
    }

    public class AnyHallowedMimic1 : ModSystem
    {
        public static RecipeGroup AnyHallowedMimicPrimary;
        public static int[] PrimaryItems => new int[] {
            ItemID.Fake_HallowedChest,
            ItemID.DaedalusStormbow,
            ItemID.CrystalVileShard,
            ItemID.FlyingKnife,
            ItemID.IlluminantHook,
        };

        public override void AddRecipeGroups()
        {
            // Main group with trophy as first item (for icon)
            AnyHallowedMimicPrimary = new RecipeGroup(() => Language.GetTextValue("Any Hallowed Mimic Primary"), PrimaryItems);
            RecipeGroup.RegisterGroup("PvPAdventure:AnyHallowedMimicPrimary", AnyHallowedMimicPrimary);

            // Create exclude subgroups while maintaining icon as first item
            foreach (int itemID in PrimaryItems.Where(id => id != ItemID.Fake_HallowedChest))
            {
                var validItems = PrimaryItems
                    .Where(id => id != itemID && id != ItemID.Fake_HallowedChest)
                    .Prepend(ItemID.Fake_HallowedChest) // Keep item first for icon
                    .ToArray();

                RecipeGroup group = new RecipeGroup(
                    () => Language.GetTextValue("Any Hallowed Mimic Primary"),
                    validItems
                );
                RecipeGroup.RegisterGroup($"PvPAdventure:AnyHallowedMimicPrimaryExclude{itemID}", group);
            }
        }
    }

    public class RecipeSystem : ModSystem
    {
        public override void AddRecipes()
        {
            var shimmerCondition = new Condition(
                Language.GetText("Mods.PvPAdventure.Conditions.NearShimmer"),
                () => Main.LocalPlayer.adjShimmer
            );

            // Primary recipes (trophy remains icon)
            foreach (int itemID in AnyGolem1.PrimaryItems.Where(id => id != ItemID.GolemMasterTrophy))
            {
                Recipe.Create(itemID)
                    .AddRecipeGroup($"PvPAdventure:AnyGolemPrimaryExclude{itemID}", 3)
                    .AddCondition(shimmerCondition)
                    .DisableDecraft()
                    .Register();
            }

            // Secondary recipes (boss bag remains icon)
            foreach (int itemID in AnyGolem2.SecondaryItems.Where(id => id != ItemID.GolemBossBag))
            {
                Recipe.Create(itemID)
                    .AddRecipeGroup($"PvPAdventure:AnyGolemSecondaryExclude{itemID}", 3)
                    .AddCondition(shimmerCondition)
                    .DisableDecraft()
                    .Register();
            }

            // Primary QS recipes (Relic remains icon)
            foreach (int itemID in AnyQueenSlime1.PrimaryItems.Where(id => id != ItemID.QueenSlimeMasterTrophy))
            {
                Recipe.Create(itemID)
                    .AddRecipeGroup($"PvPAdventure:AnyQueenSlimePrimaryExclude{itemID}", 3)
                    .AddCondition(shimmerCondition)
                    .DisableDecraft()
                    .Register();
            }

            // Primary Plantera recipes (Relic remains icon)
            foreach (int itemID in AnyPlantera1.PrimaryItems.Where(id => id != ItemID.PlanteraMasterTrophy))
            {
                Recipe.Create(itemID)
                    .AddRecipeGroup($"PvPAdventure:AnyPlanteraPrimaryExclude{itemID}", 4)
                    .AddCondition(shimmerCondition)
                    .DisableDecraft()
                    .Register();
            }

            // Primary Duke recipes (Relic remains icon)
            foreach (int itemID in AnyDuke1.PrimaryItems.Where(id => id != ItemID.DukeFishronMasterTrophy))
            {
                Recipe.Create(itemID)
                    .AddRecipeGroup($"PvPAdventure:AnyDukePrimaryExclude{itemID}", 3)
                    .AddCondition(shimmerCondition)
                    .DisableDecraft()
                    .Register();
            }

            // Primary Empress recipes (Relic remains icon)
            foreach (int itemID in AnyEmpress1.PrimaryItems.Where(id => id != ItemID.FairyQueenMasterTrophy))
            {
                Recipe.Create(itemID)
                    .AddRecipeGroup($"PvPAdventure:AnyEmpressPrimaryExclude{itemID}", 2)
                    .AddCondition(shimmerCondition)
                    .DisableDecraft()
                    .Register();
            }

            // Primary Wall recipes (Relic remains icon)
            foreach (int itemID in AnyWall1.PrimaryItems.Where(id => id != ItemID.WallofFleshMasterTrophy))
            {
                Recipe.Create(itemID)
                    .AddRecipeGroup($"PvPAdventure:AnyWallPrimaryExclude{itemID}", 2)
                    .AddCondition(shimmerCondition)
                    .DisableDecraft()
                    .Register();
            }

            // Primary UFO  recipes (Relic remains icon)
            foreach (int itemID in AnySaucer1.PrimaryItems.Where(id => id != ItemID.UFOMasterTrophy))
            {
                Recipe.Create(itemID)
                    .AddRecipeGroup($"PvPAdventure:AnySaucerPrimaryExclude{itemID}", 3)
                    .AddCondition(shimmerCondition)
                    .DisableDecraft()
                    .Register();
            }

            // Primary Corro mimic recipes (Relic remains icon)
            foreach (int itemID in AnyCorruptionMimic1.PrimaryItems.Where(id => id != ItemID.Fake_CorruptionChest))
            {
                Recipe.Create(itemID)
                    .AddRecipeGroup($"PvPAdventure:AnyCorruptionMimicPrimaryExclude{itemID}", 3)
                    .AddCondition(shimmerCondition)
                    .DisableDecraft()
                    .Register();
            }

            // Primary Hallow mimic recipes (Relic remains icon)
            foreach (int itemID in AnyHallowedMimic1.PrimaryItems.Where(id => id != ItemID.Fake_HallowedChest))
            {
                Recipe.Create(itemID)
                    .AddRecipeGroup($"PvPAdventure:AnyHallowedMimicPrimaryExclude{itemID}", 2)
                    .AddCondition(shimmerCondition)
                    .DisableDecraft()
                    .Register();
            }
        }
    }
}