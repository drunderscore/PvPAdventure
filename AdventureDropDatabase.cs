using System.Linq;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Terraria;
using Terraria.Enums;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Mono.Cecil.Cil;
using static PvPAdventure.System.BountyManager;
using static PvPAdventure.AdventureConfig;
using PvPAdventure.System;
using System.Collections.Generic;

namespace PvPAdventure;

public class AdventureDropDatabase : ModSystem
{
    private class AdventureIsPreHardmode : IItemDropRuleCondition
    {
        private AdventureIsPreHardmode()
        {
        }

        public static AdventureIsPreHardmode The { get; } = new();

        public bool CanDrop(DropAttemptInfo info) => !Main.hardMode;
        public bool CanShowItemDropInUI() => true;
        public string GetConditionDescription() => "Drops pre-hardmode";
    }

    public override void Load()
    {
        On_CommonCode._DropItemFromNPC += (orig, npc, id, stack, scattered) =>
        {
            // If we are the server, check if the drops should be instanced instead.
            if (Main.dedServ && npc.boss)
            {
                var number = Item.NewItem(npc.GetSource_Loot(), npc.Hitbox, id, stack, true, -1);

                // FIXME: Not consistent with how the rest of the codebase determines the last hit.
                if (npc.lastInteraction != 255)
                {
                    var player = Main.player[npc.lastInteraction];
                    Main.item[number].GetGlobalItem<AdventureItem>()._team = (Team)player.team;
                }

                NetMessage.SendData(MessageID.SyncItem, number: number);
            }
            else
            {
                orig(npc, id, stack, scattered);
            }
        };
    }

    private static void ModifyDropRate(IItemDropRule rule, int type, int numerator, int denominator)
    {
        if (rule is CommonDrop commonDrop && commonDrop.itemId == type)
        {
            commonDrop.chanceNumerator = numerator;
            commonDrop.chanceDenominator = denominator;
        }
        else if (rule is DropBasedOnExpertMode dropBasedOnExpertMode)
        {
            ModifyDropRate(dropBasedOnExpertMode.ruleForNormalMode, type, numerator, denominator);
            ModifyDropRate(dropBasedOnExpertMode.ruleForExpertMode, type, numerator, denominator);
        }

        foreach (var ruleChainAttempt in rule.ChainedRules)
            ModifyDropRate(ruleChainAttempt.RuleToChain, type, numerator, denominator);
    }

    public static void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
    {
        var drops = npcLoot.Get();

        foreach (var drop in drops)
            ModifyDropRate(drop, ItemID.Kraken, 1, 20);

        switch (npc.type)
        {
            case NPCID.BoneLee:
                foreach (var drop in drops)
                    ModifyDropRate(drop, ItemID.Tabi, 1, 1);
                break;

            case NPCID.Paladin:
                foreach (var drop in drops)
                {
                    ModifyDropRate(drop, ItemID.PaladinsHammer, 3, 3);
                    ModifyDropRate(drop, ItemID.PaladinsShield, 3, 3);
                }

                break;

            case NPCID.MossHornet:
                foreach (var drop in drops)
                {
                    ModifyDropRate(drop, ItemID.Stinger, 1, 1);
                    ModifyDropRate(drop, ItemID.TatteredBeeWing, 1, 15);
                }
                break;

            case NPCID.GiantTortoise:
                foreach (var drop in drops)
                    ModifyDropRate(drop, ItemID.TurtleShell, 1, 3);
                break;
            case NPCID.Psycho:
                foreach (var drop in drops)
                    ModifyDropRate(drop, ItemID.PsychoKnife, 1, 4);
                break;
            case NPCID.Harpy:
                foreach (var drop in drops)
                    ModifyDropRate(drop, ItemID.GiantHarpyFeather, 1, 75);
                break;
            case NPCID.IceGolem:
                foreach (var drop in drops)
                    ModifyDropRate(drop, ItemID.IceFeather, 1, 1);
                break;

            case NPCID.Necromancer:
            case NPCID.NecromancerArmored:
                foreach (var drop in drops)
                    ModifyDropRate(drop, ItemID.ShadowbeamStaff, 1, 1);
                break;

            case NPCID.RaggedCaster:
            case NPCID.RaggedCasterOpenCoat:
                foreach (var drop in drops)
                    ModifyDropRate(drop, ItemID.SpectreStaff, 1, 1);
                break;

            case NPCID.DiabolistRed:
            case NPCID.DiabolistWhite:
                foreach (var drop in drops)
                    ModifyDropRate(drop, ItemID.InfernoFork, 1, 1);
                break;

            case NPCID.SkeletonSniper:
                foreach (var drop in drops)
                    ModifyDropRate(drop, ItemID.SniperRifle, 1, 1);
                npcLoot.Add(ItemDropRule.Common(ItemID.RifleScope, 1));
                break;

            case NPCID.TacticalSkeleton:
                foreach (var drop in drops)
                    ModifyDropRate(drop, ItemID.TacticalShotgun, 1, 1);
                npcLoot.Add(ItemDropRule.Common(ItemID.RifleScope, 1));
                break;

            case NPCID.SkeletonCommando:
                foreach (var drop in drops)
                    ModifyDropRate(drop, ItemID.RocketLauncher, 1, 1);
                break;
            case NPCID.Vampire:
                foreach (var drop in drops)
                    ModifyDropRate(drop, ItemID.MoonStone, 1, 5);
                break;

            case NPCID.RustyArmoredBonesAxe:
            case NPCID.RustyArmoredBonesFlail:
            case NPCID.RustyArmoredBonesSword:
            case NPCID.RustyArmoredBonesSwordNoArmor:
            case NPCID.BlueArmoredBones:
            case NPCID.BlueArmoredBonesMace:
            case NPCID.BlueArmoredBonesNoPants:
            case NPCID.BlueArmoredBonesSword:
            case NPCID.HellArmoredBones:
            case NPCID.HellArmoredBonesSpikeShield:
            case NPCID.HellArmoredBonesMace:
            case NPCID.HellArmoredBonesSword:
                foreach (var drop in drops)
                {
                    ModifyDropRate(drop, ItemID.MaceWhip, 1, 10);
                    ModifyDropRate(drop, ItemID.BoneFeather, 1, 150);
                }
                break;

            case NPCID.BlackRecluse:
            case NPCID.BlackRecluseWall:
                foreach (var drop in drops)
                    ModifyDropRate(drop, ItemID.SpiderFang, 4, 5);
                break;

            case NPCID.GoblinSummoner:
                foreach (var drop in drops)
                {
                    if (drop is DropBasedOnExpertMode dropBasedOnExpertMode)
                    {
                        ((OneFromOptionsDropRule)dropBasedOnExpertMode.ruleForNormalMode).chanceNumerator = 1;
                        ((OneFromOptionsDropRule)dropBasedOnExpertMode.ruleForNormalMode).chanceDenominator = 1;

                        ((OneFromOptionsDropRule)dropBasedOnExpertMode.ruleForExpertMode).chanceNumerator = 1;
                        ((OneFromOptionsDropRule)dropBasedOnExpertMode.ruleForExpertMode).chanceDenominator = 1;
                    }
                }

                break;

            case NPCID.Moth:
                foreach (var drop in drops)
                    ModifyDropRate(drop, ItemID.ButterflyDust, 1, 1);
                break;

            case NPCID.GiantCursedSkull:
                foreach (var drop in drops)
                    ModifyDropRate(drop, ItemID.ShadowJoustingLance, 1, 1);
                break;

            case NPCID.Mothron:
                foreach (var drop in drops)
                {
                    ModifyDropRate(drop, ItemID.BrokenHeroSword, 1, 2);
                    ModifyDropRate(drop, ItemID.TheEyeOfCthulhu, 1, 2);
                    ModifyDropRate(drop, ItemID.MothronWings, 1, 6);
                }
                break;

            case NPCID.SkeletonArcher:

                npcLoot.RemoveWhere(drop =>
                    (drop is CommonDrop commonDrop && commonDrop.itemId == ItemID.MagicQuiver) ||
                    drop is LeadingConditionRule);
                npcLoot.Add(ItemDropRule.Common(ItemID.MagicQuiver, 10));
                break;

            case NPCID.RedDevil:
                foreach (var drop in drops)
                    ModifyDropRate(drop, ItemID.UnholyTrident, 1, 10);
                npcLoot.Add(ItemDropRule.Common(ItemID.FireFeather, 10, 1, 1));

                break;

            case NPCID.Lihzahrd:
            case NPCID.LihzahrdCrawler:
            case NPCID.FlyingSnake:
                foreach (var drop in drops)
                    ModifyDropRate(drop, ItemID.LihzahrdPowerCell, 1, 75);
                break;

            case NPCID.MartianEngineer:
            case NPCID.MartianOfficer:
            case NPCID.BrainScrambler:
            case NPCID.MartianWalker:
            case NPCID.GrayGrunt:
            case NPCID.GigaZapper:
            case NPCID.RayGunner:
                foreach (var drop in drops)
                {
                    ModifyDropRate(drop, ItemID.ChargedBlasterCannon, 1, 300);
                    ModifyDropRate(drop, ItemID.LaserDrill, 1, 300);
                }
                break;

            case NPCID.AngryNimbus:
                foreach (var drop in drops)
                {
                    ModifyDropRate(drop, ItemID.NimbusRod, 1, 6);
                }
                break;

            case NPCID.AngryTrapper:
                foreach (var drop in drops)
                {
                    ModifyDropRate(drop, ItemID.Vine, 1, 1);
                }
                break;

            case NPCID.Hornet:
                foreach (var drop in drops)
                {
                    ModifyDropRate(drop, ItemID.Stinger, 1, 1);
                }
                break;

            case NPCID.Nailhead:
                foreach (var drop in drops)
                {
                    ModifyDropRate(drop, ItemID.NailGun, 1, 6);
                }
                break;

            case NPCID.Reaper:
                foreach (var drop in drops)
                {
                    ModifyDropRate(drop, ItemID.DeathSickle, 1, 20);
                }
                break;

            case NPCID.DeadlySphere:
                foreach (var drop in drops)
                {
                    ModifyDropRate(drop, ItemID.DeadlySphereStaff, 1, 10);
                }
                break;

            case NPCID.DrManFly:
                foreach (var drop in drops)
                {
                    ModifyDropRate(drop, ItemID.ToxicFlask, 1, 6);
                }
                break;

            case NPCID.PirateCaptain:
                npcLoot.Add(ItemDropRule.OneFromOptions(1,
                     ItemID.CoinGun,
                     ItemID.LuckyCoin,
                     ItemID.DiscountCard,
                     ItemID.PirateStaff
                            )
                );
                foreach (var drop in drops)
                {

                    ModifyDropRate(drop, ItemID.Cutlass, 1, 10);
                    ModifyDropRate(drop, ItemID.GoldRing, 1, 10);
                }
                break;

            case NPCID.EyeofCthulhu:
                foreach (var drop in drops)
                    ModifyDropRate(drop, ItemID.Binoculars, 1, 1);
                break;

            case NPCID.DungeonSpirit:
                npcLoot.RemoveWhere(drop =>
    (drop is CommonDrop commonDrop && commonDrop.itemId == ItemID.Ectoplasm) ||
    drop is LeadingConditionRule);
                npcLoot.Add(ItemDropRule.Common(ItemID.Ectoplasm, 1, 3, 5));
                break;

            case NPCID.KingSlime:
                foreach (var drop in drops)
                    ModifyDropRate(drop, ItemID.SlimySaddle, 1, 1);
                break;

            case NPCID.Golem:
                npcLoot.RemoveWhere(drop =>
                    (drop is CommonDrop commonDrop && commonDrop.itemId == ItemID.Picksaw) ||
                    drop is LeadingConditionRule);

                var stynger = ItemDropRule.Common(ItemID.Stynger);
                stynger.OnSuccess(ItemDropRule.Common(ItemID.StyngerBolt, 1, 60, 99), hideLootReport: true);
                npcLoot.Add(
                    new OneFromRulesRule(1,
                        stynger,
                        ItemDropRule.Common(ItemID.PossessedHatchet),
                        ItemDropRule.Common(ItemID.GolemFist),
                        ItemDropRule.Common(ItemID.HeatRay),
                        ItemDropRule.Common(ItemID.StaffofEarth)
                    )
                );


                var golemFirstKillRule = new LeadingConditionRule(new FirstBossKillCondition(NPCID.Golem));
                golemFirstKillRule.OnSuccess(ItemDropRule.Common(ItemID.Picksaw, 1));
                golemFirstKillRule.OnFailedConditions(ItemDropRule.OneFromOptions(1,
                    ItemID.Picksaw,
                    ItemID.EyeoftheGolem,
                    ItemID.SunStone,
                    ItemID.ShinyStone
                ));

                npcLoot.Add(golemFirstKillRule);
                break;

            case NPCID.QueenSlimeBoss:

                npcLoot.RemoveWhere(drop => drop is LeadingConditionRule);


                var QSfirstKillRule = new LeadingConditionRule(new FirstBossKillCondition(NPCID.QueenSlimeBoss));
                QSfirstKillRule.OnSuccess(ItemDropRule.Common(ItemID.QueenSlimeMountSaddle, 1));
                QSfirstKillRule.OnFailedConditions(ItemDropRule.OneFromOptions(1,
                    ItemID.Smolstar,
                    ItemID.QueenSlimeHook,
                    ItemID.QueenSlimeMountSaddle
                ));

                npcLoot.Add(ItemDropRule.FewFromOptions(3, 1,
                        ItemID.CrystalNinjaHelmet,
                        ItemID.CrystalNinjaChestplate,
                        ItemID.CrystalNinjaLeggings
                    )
                );
                npcLoot.Add(QSfirstKillRule);
                break;

            case NPCID.QueenBee:
                npcLoot.Add(ItemDropRule.Common(ItemID.BeeWax, 1, 51, 87));
                npcLoot.Add(ItemDropRule.Common(ItemID.HoneyComb, 1, 1, 2));
                npcLoot.Add(ItemDropRule.Common(ItemID.GoldCoin, 1, 10, 15));
                npcLoot.Add(ItemDropRule.Common(ItemID.Beenade, 1, 22, 32));
                npcLoot.RemoveWhere(drop =>
                    (drop is CommonDrop commonDrop && commonDrop.itemId == ItemID.HoneyComb) ||
                    drop is DropBasedOnExpertMode);

                npcLoot.Add(ItemDropRule.OneFromOptions(1,
                        ItemID.BeeKeeper,
                        ItemID.BeesKnees
                    )
                );

                npcLoot.Add(ItemDropRule.OneFromOptions(1,
                        ItemID.BeeGun,
                        ItemID.HoneyComb
                    )
                );
                break;

            case NPCID.BigMimicHallow:
                npcLoot.RemoveWhere(drop => true); // Removes all drops
                npcLoot.Add(ItemDropRule.Common(ItemID.IlluminantHook, 4, 1, 1));
                npcLoot.Add(ItemDropRule.Common(ItemID.GoldCoin, 1, 10, 25));
                npcLoot.Add(ItemDropRule.Common(ItemID.GreaterHealingPotion, 1, 20, 30));
                npcLoot.Add(ItemDropRule.Common(ItemID.GreaterManaPotion, 1, 75, 150));

                npcLoot.Add(ItemDropRule.OneFromOptions(1,
                    ItemID.DaedalusStormbow,
                    ItemID.CrystalVileShard,
                    ItemID.FlyingKnife
                ));
                break;
            case NPCID.BigMimicCorruption:
                npcLoot.RemoveWhere(drop => true); // Removes all drops
                npcLoot.Add(ItemDropRule.Common(ItemID.GoldCoin, 1, 10, 25));
                npcLoot.Add(ItemDropRule.Common(ItemID.GreaterHealingPotion, 1, 20, 30));
                npcLoot.Add(ItemDropRule.Common(ItemID.GreaterManaPotion, 1, 75, 150));
                npcLoot.Add(ItemDropRule.OneFromOptions(1,
                    ItemID.ClingerStaff,
                    ItemID.DartRifle,
                    ItemID.ChainGuillotines
                ));
                npcLoot.Add(ItemDropRule.OneFromOptionsWithNumerator(5, 2,
                    ItemID.PutridScent,
                    ItemID.WormHook
                ));
                break;

            case NPCID.SkeletronHead:
                npcLoot.Add(ItemDropRule.Common(ItemID.GoldenKey, 1, 3, 3));
                npcLoot.Add(ItemDropRule.Common(ItemID.Marrow, 1000, 1000, 1000));

                foreach (var drop in drops)
                    ModifyDropRate(drop, ItemID.SkeletronHand, 1, 1);
                break;
            case NPCID.DemonEye:
            case NPCID.WanderingEye:
                foreach (var drop in drops)
                    ModifyDropRate(drop, ItemID.Lens, 1, 1);
                break;

            case NPCID.IceTortoise:
                npcLoot.RemoveWhere(drop =>
                    (drop is CommonDrop commonDrop && commonDrop.itemId == ItemID.FrozenTurtleShell));
                npcLoot.Add(ItemDropRule.Common(ItemID.FrozenKey, 40, 1, 1));
                foreach (var drop in drops) ;
                break;

            case NPCID.MartianSaucerCore:
                foreach (var drop in drops)
                {
                    if (drop is OneFromOptionsNotScaledWithLuckDropRule oneFromOptionsNotScaledWithLuckDropRule)
                    {
                        oneFromOptionsNotScaledWithLuckDropRule.dropIds = oneFromOptionsNotScaledWithLuckDropRule
                            .dropIds.Where(id => id != ItemID.CosmicCarKey).ToArray();
                    }
                }

                break;

            case NPCID.WallofFlesh:

                npcLoot.RemoveWhere(drop => drop is LeadingConditionRule);

                var wofFirstKillRule = new LeadingConditionRule(new FirstBossKillCondition(NPCID.WallofFlesh));
                wofFirstKillRule.OnSuccess(ItemDropRule.OneFromOptions(1,
                    ItemID.WarriorEmblem,
                    ItemID.RangerEmblem,
                    ItemID.SorcererEmblem,
                    ItemID.SummonerEmblem
                ));
                npcLoot.Add(wofFirstKillRule);
                npcLoot.Add(
                    new OneFromRulesRule(1,
                        ItemDropRule.Common(ItemID.WarriorEmblem),
                        ItemDropRule.Common(ItemID.RangerEmblem),
                        ItemDropRule.Common(ItemID.SorcererEmblem),
                        ItemDropRule.Common(ItemID.SummonerEmblem)
                    )
                );
                npcLoot.Add(
                    new OneFromRulesRule(1,
                        ItemDropRule.Common(ItemID.FireWhip),
                        ItemDropRule.Common(ItemID.BreakerBlade)
                    )
                );
                npcLoot.Add(
                    new OneFromRulesRule(1,
                        ItemDropRule.Common(ItemID.LaserRifle),
                        ItemDropRule.Common(ItemID.ClockworkAssaultRifle)
                    )
                );
                break;

            case NPCID.DukeFishron:

                npcLoot.RemoveWhere(drop => drop is LeadingConditionRule);

                foreach (var drop in drops)
                {
                    if (drop is OneFromOptionsNotScaledWithLuckDropRule oneFromOptionsNotScaledWithLuckDropRule)
                    {
                        oneFromOptionsNotScaledWithLuckDropRule.dropIds = oneFromOptionsNotScaledWithLuckDropRule
                            .dropIds.Where(id => id != ItemID.FishronWings).ToArray();
                    }
                }

                var dukefirstKillRule = new LeadingConditionRule(new FirstBossKillCondition(NPCID.DukeFishron));
                dukefirstKillRule.OnSuccess(ItemDropRule.OneFromOptions(1,
                    ItemID.BubbleGun,
                    ItemID.Tsunami
                ));
                dukefirstKillRule.OnFailedConditions(ItemDropRule.OneFromOptions(1,
                    ItemID.RazorbladeTyphoon,
                    ItemID.Tsunami,
                    ItemID.TempestStaff,
                    ItemID.BubbleGun,
                    ItemID.Flairon
                ));
                npcLoot.Add(dukefirstKillRule);
                break;


            case NPCID.HallowBoss:
                npcLoot.RemoveWhere(drop =>
                    (drop is CommonDrop commonDrop && commonDrop.itemId == ItemID.RainbowWings));
                break;

            case NPCID.TheDestroyer:
                npcLoot.RemoveWhere(drop =>
                    (drop is CommonDrop commonDrop && commonDrop.itemId == ItemID.HallowedBar));
                npcLoot.Add(ItemDropRule.Common(ItemID.HallowedBar, 1, 18, 30));

                // Remove existing soul drops
                npcLoot.RemoveWhere(drop =>
                    (drop is CommonDrop commonDrop && commonDrop.itemId == ItemID.SoulofMight));
                npcLoot.Add(ItemDropRule.Common(ItemID.SoulofMight, 1, 40, 40));
                break;

            case NPCID.SkeletronPrime:
                npcLoot.RemoveWhere(drop =>
                    (drop is CommonDrop commonDrop && commonDrop.itemId == ItemID.HallowedBar));
                npcLoot.Add(ItemDropRule.Common(ItemID.HallowedBar, 1, 18, 30));

                npcLoot.RemoveWhere(drop =>
                    (drop is CommonDrop commonDrop && commonDrop.itemId == ItemID.SoulofFright));
                npcLoot.Add(ItemDropRule.Common(ItemID.SoulofFright, 1, 40, 40));
                break;

            case NPCID.Retinazer:
            case NPCID.Spazmatism:
                npcLoot.RemoveWhere(rule => true);

                npcLoot.Add(ItemDropRule.ByCondition(
                    new Conditions.LegacyHack_IsBossAndNotExpert(),
                    ItemID.HallowedBar, 1, 18, 30));

                npcLoot.Add(ItemDropRule.ByCondition(
                    new Conditions.LegacyHack_IsBossAndNotExpert(),
                    ItemID.SoulofSight, 1, 40, 40));
                break;

            case NPCID.Plantera:
                npcLoot.RemoveWhere(drop => drop is LeadingConditionRule);
                npcLoot.Add(ItemDropRule.Common(ItemID.TempleKey, 1, 1, 1));
                npcLoot.Add(
                    new OneFromRulesRule(1,
                        ItemDropRule.Common(ItemID.Seedler),
                        ItemDropRule.Common(ItemID.LeafBlower),
                        ItemDropRule.Common(ItemID.WaspGun),
                        ItemDropRule.Common(ItemID.PygmyStaff)
                    )
                );
                var grenadelauncher = ItemDropRule.Common(ItemID.GrenadeLauncher);
                grenadelauncher.OnSuccess(ItemDropRule.Common(ItemID.RocketIII, 1, 400, 500), hideLootReport: true);
                npcLoot.Add(
                    new OneFromRulesRule(1,
                        grenadelauncher,
                        ItemDropRule.Common(ItemID.NettleBurst),
                        ItemDropRule.Common(ItemID.FlowerPow),
                        ItemDropRule.Common(ItemID.VenusMagnum)
                    )
                );
                break;
        }
    }

    public static IItemDropRule OnItemDropDatabaseRegisterToGlobal(On_ItemDropDatabase.orig_RegisterToGlobal orig,
        ItemDropDatabase self, IItemDropRule entry)
    {
        var adventureConfig = ModContent.GetInstance<AdventureConfig>();

        var disallowed = false;

        disallowed |= entry is MechBossSpawnersDropRule && adventureConfig.NpcBalance.NoMechanicalBossSummonDrops;

        if (!disallowed)
            orig(self, entry);

        return entry;
    }

    public class BiomeKeyDropEdit : ModSystem
    {
        private static ILHook globalRulesHook;

        public override void PostSetupContent()
        {
            // Apply the IL edit to change biome key drop rates from 2500 to 250
            MethodInfo method = typeof(Terraria.GameContent.ItemDropRules.ItemDropDatabase).GetMethod("RegisterGlobalRules",
                BindingFlags.NonPublic | BindingFlags.Instance);
            globalRulesHook = new ILHook(method, BiomeKeyDropILEdit);
        }

        public override void Unload()
        {
            globalRulesHook?.Dispose();
        }

        private static void BiomeKeyDropILEdit(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            int replacedCount = 0;

            // We need to find all instances of 2500 that are used for biome keys
            // We'll look for the pattern where 2500 is loaded right before creating ItemDropWithConditionRule
            while (cursor.TryGotoNext(MoveType.Before,
                i => i.MatchLdcI4(2500))) // Match loading the constant 2500
            {
                // Check if this is followed by the pattern that indicates it's a biome key drop rule
                // We'll look ahead to see if this leads to ItemDropWithConditionRule construction
                var nextCursor = cursor.Clone();
                bool isBiomeKey = false;

                // Look for the ItemDropWithConditionRule constructor call within a reasonable distance
                for (int j = 0; j < 250; j++) // Look ahead up to 250 instructions
                {
                    if (nextCursor.TryGotoNext(MoveType.After,
                        i => i.MatchNewobj<Terraria.GameContent.ItemDropRules.ItemDropWithConditionRule>()))
                    {
                        isBiomeKey = true;
                        break;
                    }
                }

                if (isBiomeKey)
                {
                    // Replace the 2500 with 250
                    cursor.Remove(); // Remove the ldc.i4 2500 instruction
                    cursor.Emit(OpCodes.Ldc_I4, 250); // Emit ldc.i4 250 instead
                    replacedCount++;

                    ModContent.GetInstance<PvPAdventure>().Logger.Info($"Replaced biome key drop rate 2500 with 250 (instance {replacedCount})");
                }
                else
                {
                    // Move past this 2500 if it's not a biome key
                    cursor.Index++;
                }
            }

            if (replacedCount > 0)
            {
                ModContent.GetInstance<PvPAdventure>().Logger.Info($"Successfully changed {replacedCount} biome key drop rates from 2500 to 250");
            }
            else
            {
                ModContent.GetInstance<PvPAdventure>().Logger.Error("Failed to find any biome key drop rates (2500) in RegisterGlobalRules method");
            }
        }
    }
}