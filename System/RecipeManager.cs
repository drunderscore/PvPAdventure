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
            ItemID.MoonlordArrow,
            ItemID.ShroomiteBar,
            ItemID.WormFood,
            ItemID.ChlorophyteBar,
            ItemID.FlaskofCursedFlames,
            ItemID.FlaskofIchor,
            ItemID.FlaskofPoison,
            ItemID.FlaskofVenom,
            ItemID.FlaskofFire,
            ItemID.FlaskofNanites,
            ItemID.FlaskofParty,
            ItemID.SpectreBar,
            ItemID.SuspiciousLookingEye

    };

        for (int i = 0; i < Main.recipe.Length; i++)
        {
            Recipe recipe = Main.recipe[i];
            if (recipe.createItem.type != ItemID.None && itemsToRemove.Contains(recipe.createItem.type))
            {
                recipe.DisableRecipe();
            }
        }

        //shroomite bar crafting cost reduction

        Recipe.Create(ItemID.ShroomiteBar)
            .AddIngredient(ItemID.GlowingMushroom, 3)
            .AddIngredient(ItemID.ChlorophyteBar, 1)
            .AddTile(TileID.Autohammer)
            .Register();

        Recipe.Create(ItemID.SuspiciousLookingEye)
            .AddIngredient(ItemID.Lens, 3)
            .AddTile(TileID.DemonAltar)
            .Register();

        Recipe.Create(ItemID.WormFood)
            .AddIngredient(ItemID.RottenChunk, 6)
            .AddIngredient(ItemID.VilePowder, 10)
            .AddTile(TileID.DemonAltar)
            .Register();

        Recipe.Create(ItemID.ChlorophyteBar)
            .AddIngredient(ItemID.ChlorophyteOre, 4)
            .AddTile(TileID.AdamantiteForge)
            .Register();

        Recipe.Create(ItemID.SpectreBar, 3)
            .AddIngredient(ItemID.ChlorophyteBar, 3)
            .AddIngredient(ItemID.Ectoplasm, 1)
            .AddTile(TileID.AdamantiteForge)
            .Register();

        Recipe.Create(ItemID.Megashark)
            .AddIngredient(ItemID.SharkFin, 1)
            .AddIngredient(ItemID.IllegalGunParts, 1)
            .AddIngredient(ItemID.Minishark, 1)
            .AddIngredient(ItemID.SoulofMight, 20)
            .AddTile(TileID.MythrilAnvil)
            .Register();

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
            ItemID.FishronWings,
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

        public class AnyMimic1 : ModSystem
        {
            public static RecipeGroup AnyMimicPrimary;
            public static int[] PrimaryItems => new int[] {
            ItemID.DeadMansChest,
            ItemID.TitanGlove,
            ItemID.CrossNecklace,
            ItemID.StarCloak,
            ItemID.PhilosophersStone,
            ItemID.MagicDagger,
            ItemID.DualHook,
        };

            public override void AddRecipeGroups()
            {
                // Main group with trophy as first item (for icon)
                AnyMimicPrimary = new RecipeGroup(() => Language.GetTextValue("Any Mimic Primary"), PrimaryItems);
                RecipeGroup.RegisterGroup("PvPAdventure:AnyMimicPrimary", AnyMimicPrimary);

                // Create exclude subgroups while maintaining icon as first item
                foreach (int itemID in PrimaryItems.Where(id => id != ItemID.DeadMansChest))
                {
                    var validItems = PrimaryItems
                        .Where(id => id != itemID && id != ItemID.DeadMansChest)
                        .Prepend(ItemID.DeadMansChest) // Keep item first for icon
                        .ToArray();

                    RecipeGroup group = new RecipeGroup(
                        () => Language.GetTextValue("Any Mimic Primary"),
                        validItems
                    );
                    RecipeGroup.RegisterGroup($"PvPAdventure:AnyMimicPrimaryExclude{itemID}", group);
                }
            }
        }

        public class AnyBrickwall1: ModSystem
        {
            public static RecipeGroup AnyBrickwall;
            public static int[] PrimaryItems => new int[] {
            ItemID.NecromanticSign,
            ItemID.ShadowbeamStaff,
            ItemID.RocketLauncher,
            ItemID.PaladinsHammer
        };

            public override void AddRecipeGroups()
            {
                // Main group with trophy as first item (for icon)
                AnyBrickwall = new RecipeGroup(() => Language.GetTextValue("Any Brick Wall"), PrimaryItems);
                RecipeGroup.RegisterGroup("PvPAdventure:AnyBrickWall", AnyBrickwall);

                // Create exclude subgroups while maintaining icon as first item
                foreach (int itemID in PrimaryItems.Where(id => id != ItemID.NecromanticSign))
                {
                    var validItems = PrimaryItems
                        .Where(id => id != itemID && id != ItemID.NecromanticSign)
                        .Prepend(ItemID.NecromanticSign) // Keep item first for icon
                        .ToArray();

                    RecipeGroup group = new RecipeGroup(
                        () => Language.GetTextValue("Any Brick Wall"),
                        validItems
                    );
                    RecipeGroup.RegisterGroup($"PvPAdventure:AnyBrickWallExclude{itemID}", group);
                }
            }
        }

        public class AnyGold1 : ModSystem
        {
            public static RecipeGroup AnyGold;
            public static int[] PrimaryItems => new int[] {
            ItemID.CloudinaBottle,
            ItemID.HermesBoots,
            ItemID.FlareGun,
            ItemID.ShoeSpikes,
            ItemID.BandofRegeneration,
            ItemID.Mace
        };

            public override void AddRecipeGroups()
            {
                // Main group with trophy as first item (for icon)
                AnyGold = new RecipeGroup(() => Language.GetTextValue("Any Gold Chest Item"), PrimaryItems);
                RecipeGroup.RegisterGroup("PvPAdventure:AnyGold", AnyGold);

                // Create exclude subgroups while maintaining icon as first item
                foreach (int itemID in PrimaryItems.Where(id => id != ItemID.GoldenChest))
                {
                    var validItems = PrimaryItems
                        .Where(id => id != itemID && id != ItemID.GoldenChest)
                        .Prepend(ItemID.GoldenChest) // Keep item first for icon
                        .ToArray();

                    RecipeGroup group = new RecipeGroup(
                        () => Language.GetTextValue("Any Gold Chest Item"),
                        validItems
                    );
                    RecipeGroup.RegisterGroup($"PvPAdventure:AnyGoldExclude{itemID}", group);
                }
            }
        }

        public class AnyJungle1 : ModSystem
        {
            public static RecipeGroup AnyJungle;
            public static int[] PrimaryItems => new int[] {
            ItemID.AnkletoftheWind,
            ItemID.Boomstick,
            ItemID.StaffofRegrowth,
            ItemID.FlowerBoots,
            ItemID.FiberglassFishingPole,
            ItemID.FeralClaws
        };

            public override void AddRecipeGroups()
            {
                // Main group with trophy as first item (for icon)
                AnyJungle = new RecipeGroup(() => Language.GetTextValue("Any Gold Chest Item"), PrimaryItems);
                RecipeGroup.RegisterGroup("PvPAdventure:AnyJungle", AnyJungle);

                // Create exclude subgroups while maintaining icon as first item
                foreach (int itemID in PrimaryItems.Where(id => id != ItemID.SwampThingBanner))
                {
                    var validItems = PrimaryItems
                        .Where(id => id != itemID && id != ItemID.SwampThingBanner)
                        .Prepend(ItemID.SwampThingBanner) // Keep item first for icon
                        .ToArray();

                    RecipeGroup group = new RecipeGroup(
                        () => Language.GetTextValue("Any Jungle Chest Item"),
                        validItems
                    );
                    RecipeGroup.RegisterGroup($"PvPAdventure:AnyJungleExclude{itemID}", group);
                }
            }
        }

        public class AnyDesert1 : ModSystem
        {
            public static RecipeGroup AnyHighDesert;
            public static int[] PrimaryItems => new int[] {
            ItemID.MysticCoilSnake,
            ItemID.SandBoots,
            ItemID.AncientChisel
        };

            public override void AddRecipeGroups()
            {
                // Main group with trophy as first item (for icon)
                AnyHighDesert = new RecipeGroup(() => Language.GetTextValue("Any High Desert Chest Item"), PrimaryItems);
                RecipeGroup.RegisterGroup("PvPAdventure:AnyHighDesert", AnyHighDesert);

                // Create exclude subgroups while maintaining icon as first item
                foreach (int itemID in PrimaryItems.Where(id => id != ItemID.PharaohsMask))
                {
                    var validItems = PrimaryItems
                        .Where(id => id != itemID && id != ItemID.PharaohsMask)
                        .Prepend(ItemID.PharaohsMask) // Keep item first for icon
                        .ToArray();

                    RecipeGroup group = new RecipeGroup(
                        () => Language.GetTextValue("Any High Desert Chest Item"),
                        validItems
                    );
                    RecipeGroup.RegisterGroup($"PvPAdventure:AnyHighDesertExclude{itemID}", group);
                }
            }
        }

        public class AnyDesert2 : ModSystem
        {
            public static RecipeGroup AnyLowDesert;
            public static int[] PrimaryItems => new int[] {
            ItemID.SandstorminaBottle,
            ItemID.ThunderSpear,
            ItemID.ThunderStaff
        };

            public override void AddRecipeGroups()
            {
                // Main group with trophy as first item (for icon)
                AnyLowDesert = new RecipeGroup(() => Language.GetTextValue("Any Low Desert Chest Item"), PrimaryItems);
                RecipeGroup.RegisterGroup("PvPAdventure:AnyLowDesert", AnyLowDesert);

                // Create exclude subgroups while maintaining icon as first item
                foreach (int itemID in PrimaryItems.Where(id => id != ItemID.PharaohsRobe))
                {
                    var validItems = PrimaryItems
                        .Where(id => id != itemID && id != ItemID.PharaohsRobe)
                        .Prepend(ItemID.PharaohsRobe) // Keep item first for icon
                        .ToArray();

                    RecipeGroup group = new RecipeGroup(
                        () => Language.GetTextValue("Any Low Desert Chest Item"),
                        validItems
                    );
                    RecipeGroup.RegisterGroup($"PvPAdventure:AnyLowDesertExclude{itemID}", group);
                }
            }
        }

        public class AnyIce1 : ModSystem
        {
            public static RecipeGroup AnyIce;
            public static int[] PrimaryItems => new int[] {
            ItemID.FlurryBoots,
            ItemID.BlizzardinaBottle,
            ItemID.SnowballCannon,
            ItemID.IceSkates,
            ItemID.IceBlade,
            ItemID.IceBoomerang,
            ItemID.Fish

        };

            public override void AddRecipeGroups()
            {
                // Main group with trophy as first item (for icon)
                AnyIce = new RecipeGroup(() => Language.GetTextValue("Any Ice Chest Item"), PrimaryItems);
                RecipeGroup.RegisterGroup("PvPAdventure:AnyIce", AnyIce);

                // Create exclude subgroups while maintaining icon as first item
                foreach (int itemID in PrimaryItems.Where(id => id != ItemID.SnowballLauncher))
                {
                    var validItems = PrimaryItems
                        .Where(id => id != itemID && id != ItemID.SnowballLauncher)
                        .Prepend(ItemID.SnowballLauncher) // Keep item first for icon
                        .ToArray();

                    RecipeGroup group = new RecipeGroup(
                        () => Language.GetTextValue("Any Ice Chest Item"),
                        validItems
                    );
                    RecipeGroup.RegisterGroup($"PvPAdventure:AnyIceExclude{itemID}", group);
                }
            }
        }

        public class AnySky1 : ModSystem
        {
            public static RecipeGroup AnySky;
            public static int[] PrimaryItems => new int[] {
            ItemID.ShinyRedBalloon,
            ItemID.Starfury,
            ItemID.CelestialMagnet,
            ItemID.CreativeWings,
            ItemID.LuckyHorseshoe

        };

            public override void AddRecipeGroups()
            {
                // Main group with trophy as first item (for icon)
                AnySky = new RecipeGroup(() => Language.GetTextValue("Any Sky Chest Item"), PrimaryItems);
                RecipeGroup.RegisterGroup("PvPAdventure:AnySky", AnySky);

                // Create exclude subgroups while maintaining icon as first item
                foreach (int itemID in PrimaryItems.Where(id => id != ItemID.CreativeWings))
                {
                    var validItems = PrimaryItems
                        .Where(id => id != itemID && id != ItemID.CreativeWings)
                        .Prepend(ItemID.CreativeWings) // Keep item first for icon
                        .ToArray();

                    RecipeGroup group = new RecipeGroup(
                        () => Language.GetTextValue("Any Sky Chest Item"),
                        validItems
                    );
                    RecipeGroup.RegisterGroup($"PvPAdventure:AnySkyExclude{itemID}", group);
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
                        .AddRecipeGroup($"PvPAdventure:AnyQueenSlimePrimaryExclude{itemID}", 2)
                        .AddCondition(shimmerCondition)
                        .DisableDecraft()
                        .Register();
                }

                // Primary Plantera recipes (Relic remains icon)
                foreach (int itemID in AnyPlantera1.PrimaryItems.Where(id => id != ItemID.PlanteraMasterTrophy))
                {
                    Recipe.Create(itemID)
                        .AddRecipeGroup($"PvPAdventure:AnyPlanteraPrimaryExclude{itemID}", 3)
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

                // Primary Mimic recipes (Relic remains icon)
                foreach (int itemID in AnyMimic1.PrimaryItems.Where(id => id != ItemID.DeadMansChest))
                {
                    Recipe.Create(itemID)
                        .AddRecipeGroup($"PvPAdventure:AnyMimicPrimaryExclude{itemID}", 3)
                        .AddCondition(shimmerCondition)
                        .DisableDecraft()
                        .Register();
                }
                // Primary Mimic recipes (Relic remains icon)
                foreach (int itemID in AnyBrickwall1.PrimaryItems.Where(id => id != ItemID.NecromanticSign))
                {
                    Recipe.Create(itemID)
                        .AddRecipeGroup($"PvPAdventure:AnyBrickWallExclude{itemID}", 3)
                        .AddCondition(shimmerCondition)
                        .DisableDecraft()
                        .Register();
                }
                // Primary Mimic recipes (Relic remains icon)
                foreach (int itemID in AnyGold1.PrimaryItems.Where(id => id != ItemID.GoldChest))
                {
                    Recipe.Create(itemID)
                        .AddRecipeGroup($"PvPAdventure:AnyGoldExclude{itemID}", 3)
                        .AddCondition(shimmerCondition)
                        .DisableDecraft()
                        .Register();
                }
                // Primary Mimic recipes (Relic remains icon)
                foreach (int itemID in AnyJungle1.PrimaryItems.Where(id => id != ItemID.SwampThingBanner))
                {
                    Recipe.Create(itemID)
                        .AddRecipeGroup($"PvPAdventure:AnyJungleExclude{itemID}", 3)
                        .AddCondition(shimmerCondition)
                        .DisableDecraft()
                        .Register();
                }
                // Primary Mimic recipes (Relic remains icon)
                foreach (int itemID in AnyDesert1.PrimaryItems.Where(id => id != ItemID.PharaohsMask))
                {
                    Recipe.Create(itemID)
                        .AddRecipeGroup($"PvPAdventure:AnyHighDesertExclude{itemID}", 3)
                        .AddCondition(shimmerCondition)
                        .DisableDecraft()
                        .Register();
                }
                // Primary Mimic recipes (Relic remains icon)
                foreach (int itemID in AnyDesert2.PrimaryItems.Where(id => id != ItemID.PharaohsRobe))
                {
                    Recipe.Create(itemID)
                        .AddRecipeGroup($"PvPAdventure:AnyLowDesertExclude{itemID}", 3)
                        .AddCondition(shimmerCondition)
                        .DisableDecraft()
                        .Register();
                }
                // Primary Mimic recipes (Relic remains icon)
                foreach (int itemID in AnyIce1.PrimaryItems.Where(id => id != ItemID.SnowballLauncher))
                {
                    Recipe.Create(itemID)
                        .AddRecipeGroup($"PvPAdventure:AnyIceExclude{itemID}", 2)
                        .AddCondition(shimmerCondition)
                        .DisableDecraft()
                        .Register();
                }
                // Primary Mimic recipes (Relic remains icon)
                foreach (int itemID in AnySky1.PrimaryItems.Where(id => id != ItemID.CreativeWings))
                {
                    Recipe.Create(itemID)
                        .AddRecipeGroup($"PvPAdventure:AnySkyExclude{itemID}", 2)
                        .AddCondition(shimmerCondition)
                        .DisableDecraft()
                        .Register();
                }
            }
        }
    }
}