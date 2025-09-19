using System.Linq;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Mono.Cecil.Cil;
using static PvPAdventure.System.BountyManager;
using static PvPAdventure.AdventureConfig;
using PvPAdventure.System;

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

                // FIXME: magic time unit
                Main.timeItemSlotCannotBeReusedFor[number] = 54000;
                NetMessage.SendData(MessageID.InstancedItem, remoteClient: npc.lastInteraction, number: number);
                Main.item[number].active = false;
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
                    ModifyDropRate(drop, ItemID.Stinger, 1, 1);
                break;

            case NPCID.GiantTortoise:
                foreach (var drop in drops)
                    ModifyDropRate(drop, ItemID.TurtleShell, 1, 4);
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
                    ModifyDropRate(drop, ItemID.MaceWhip, 1, 10);
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
                    ModifyDropRate(drop, ItemID.BrokenHeroSword, 1, 2);
                break;

            case NPCID.SkeletonArcher:

                npcLoot.RemoveWhere(drop =>
                    (drop is CommonDrop commonDrop && commonDrop.itemId == ItemID.MagicQuiver) ||
                    drop is LeadingConditionRule);
                if (NPC.downedPlantBoss == true)
                {
                    npcLoot.Add(ItemDropRule.Common(ItemID.StalkersQuiver, 7));
                }
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

            case NPCID.EyeofCthulhu:
                foreach (var drop in drops)
                    ModifyDropRate(drop, ItemID.Binoculars, 1, 1);
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
                // Remove Honey Comb drop, and the big loot pool -- we will re-create it ourselves.
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

            case NPCID.SkeletronHead:
                npcLoot.Add(ItemDropRule.Common(ItemID.GoldenKey, 1, 3, 3));
                npcLoot.Add(ItemDropRule.Common(ItemID.Marrow, 1000, 1000, 1000));

                foreach (var drop in drops)
                    ModifyDropRate(drop, ItemID.SkeletronHand, 1, 1);
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

                var wofFirstKillRule = new LeadingConditionRule(new FirstBossKillCondition(NPCID.WallofFlesh));
                wofFirstKillRule.OnSuccess(ItemDropRule.OneFromOptions(1,
                    ItemID.WarriorEmblem,
                    ItemID.RangerEmblem,
                    ItemID.SorcererEmblem,
                    ItemID.SummonerEmblem
                ));
                npcLoot.Add(wofFirstKillRule);
                break;

            case NPCID.DukeFishron:

                npcLoot.RemoveWhere(drop => drop is LeadingConditionRule);


                var dukefirstKillRule = new LeadingConditionRule(new FirstBossKillCondition(NPCID.QueenSlimeBoss));
                dukefirstKillRule.OnSuccess(ItemDropRule.Common(ItemID.Tsunami, 1));
                dukefirstKillRule.OnFailedConditions(ItemDropRule.OneFromOptions(1,
                    ItemID.BubbleGun,
                    ItemID.RazorbladeTyphoon,
                    ItemID.Tsunami,
                    ItemID.TempestStaff,
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

                break;

            case NPCID.SkeletronPrime:
                npcLoot.RemoveWhere(drop =>
                    (drop is CommonDrop commonDrop && commonDrop.itemId == ItemID.HallowedBar));
                npcLoot.Add(ItemDropRule.Common(ItemID.HallowedBar, 1, 18, 30));

                break;

            case NPCID.Retinazer or NPCID.Spazmatism:
                {
                    npcLoot.RemoveWhere(drop => (drop is CommonDrop commonDrop && commonDrop.itemId == ItemID.HallowedBar));

                    LeadingConditionRule noTwin = new(new Conditions.MissingTwin());
                    npcLoot.Add(noTwin);

                    noTwin.OnSuccess(ItemDropRule.Common(ItemID.HallowedBar, 1, 3, 3));
                }
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

    public class PlanteraDropEdit : ModSystem
    {
        private static ILHook planteraHook;

        public override void PostSetupContent()
        {
            // Apply the IL edit to change Plantera's first-time drop from item 758 to 1255
            MethodInfo method = typeof(Terraria.GameContent.ItemDropRules.ItemDropDatabase).GetMethod("RegisterBoss_Plantera",
                BindingFlags.NonPublic | BindingFlags.Instance);

            planteraHook = new ILHook(method, PlanteraDropILEdit);
        }

        public override void Unload()
        {
            planteraHook?.Dispose();
        }

        private static void PlanteraDropILEdit(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            // Look for the instruction that loads the value 758 (Grenade Launcher ID)
            // This should be: ldc.i4 758 (or ldc.i4.s 758 if it's a short form)
            if (cursor.TryGotoNext(MoveType.Before,
                i => i.MatchLdcI4(758))) // Match loading the constant 758
            {
                // Replace the 758 with 1255
                cursor.Remove(); // Remove the ldc.i4 758 instruction
                cursor.Emit(OpCodes.Ldc_I4, 1255); // Emit ldc.i4 1255 instead

                ModContent.GetInstance<PvPAdventure>().Logger.Info("Successfully changed Plantera's first-time drop from item 758 to 1255");
            }
            else
            {
                ModContent.GetInstance<PvPAdventure>().Logger.Error("Failed to find item ID 758 in RegisterBoss_Plantera method");
            }
        }
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