using System.Linq;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Mono.Cecil.Cil;

namespace PvPAdventure;

public static class AdventureDropDatabase
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

        switch (npc.type)
        {
            case NPCID.BoneLee:
                foreach (var drop in drops)
                    ModifyDropRate(drop, ItemID.Tabi, 1, 4);
                break;

            case NPCID.Paladin:
                foreach (var drop in drops)
                {
                    ModifyDropRate(drop, ItemID.PaladinsHammer, 3, 20);
                    ModifyDropRate(drop, ItemID.PaladinsShield, 3, 20);
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
                    ModifyDropRate(drop, ItemID.ShadowbeamStaff, 1, 10);
                break;

            case NPCID.RaggedCaster:
            case NPCID.RaggedCasterOpenCoat:
                foreach (var drop in drops)
                    ModifyDropRate(drop, ItemID.SpectreStaff, 1, 10);
                break;

            case NPCID.DiabolistRed:
            case NPCID.DiabolistWhite:
                foreach (var drop in drops)
                    ModifyDropRate(drop, ItemID.InfernoFork, 1, 10);
                break;

            case NPCID.SkeletonSniper:
                foreach (var drop in drops)
                    ModifyDropRate(drop, ItemID.SniperRifle, 1, 10);
                break;

            case NPCID.TacticalSkeleton:
                foreach (var drop in drops)
                    ModifyDropRate(drop, ItemID.TacticalShotgun, 1, 10);
                npcLoot.Add(ItemDropRule.Common(ItemID.RifleScope, 6));
                break;

            case NPCID.SkeletonCommando:
                foreach (var drop in drops)
                    ModifyDropRate(drop, ItemID.RocketLauncher, 1, 10);
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
                    ModifyDropRate(drop, ItemID.MaceWhip, 3, 400);
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
                    ModifyDropRate(drop, ItemID.ShadowJoustingLance, 1, 12);
                break;

            case NPCID.Mothron:
                foreach (var drop in drops)
                    ModifyDropRate(drop, ItemID.BrokenHeroSword, 1, 2);
                break;

            case NPCID.SkeletonArcher:
                foreach (var drop in drops)
                    ModifyDropRate(drop, ItemID.MagicQuiver, 1, 30);
                break;

            case NPCID.RedDevil:
                foreach (var drop in drops)
                    ModifyDropRate(drop, ItemID.UnholyTrident, 1, 10);
                break;

            case NPCID.Lihzahrd:
            case NPCID.LihzahrdCrawler:
            case NPCID.FlyingSnake:
                foreach (var drop in drops)
                    ModifyDropRate(drop, ItemID.LihzahrdPowerCell, 1, 50);
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
                // Remove Picksaw drop, and the big loot pool -- we will re-create it ourselves.
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

                npcLoot.Add(ItemDropRule.OneFromOptions(1,
                    ItemID.Picksaw,
                    ItemID.EyeoftheGolem,
                    ItemID.SunStone,
                    ItemID.ShinyStone)
                );

                break;

            case NPCID.QueenSlimeBoss:
                // Remove the big loot pool -- we will re-create it ourselves.
                npcLoot.RemoveWhere(drop => drop is LeadingConditionRule);

                // Always get two pieces of the Crystal Ninja set, separate from other drops.
                npcLoot.Add(ItemDropRule.FewFromOptions(2, 1,
                        ItemID.CrystalNinjaHelmet,
                        ItemID.CrystalNinjaChestplate,
                        ItemID.CrystalNinjaLeggings
                    )
                );

                npcLoot.Add(ItemDropRule.OneFromOptions(1,
                        ItemID.Smolstar,
                        ItemID.QueenSlimeHook,
                        ItemID.QueenSlimeMountSaddle
                    )
                );

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
                npcLoot.Add(ItemDropRule.Common(ItemID.GoldenKey));
                foreach (var drop in drops)
                    ModifyDropRate(drop, ItemID.SkeletronHand, 1, 1);
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
                npcLoot.Add(new LeadingConditionRule(AdventureIsPreHardmode.The))
                    .OnSuccess(ItemDropRule.OneFromOptions(1, [
                        ItemID.WarriorEmblem,
                        ItemID.RangerEmblem,
                        ItemID.SorcererEmblem,
                        ItemID.SummonerEmblem
                    ]));
                break;

        }
    }

    public static IItemDropRule OnItemDropDatabaseRegisterToGlobal(On_ItemDropDatabase.orig_RegisterToGlobal orig,
        ItemDropDatabase self, IItemDropRule entry)
    {
        var adventureConfig = ModContent.GetInstance<AdventureConfig>();

        var disallowed = false;

        disallowed |= entry is MechBossSpawnersDropRule && adventureConfig.NpcBalance.NoMechanicalBossSummonDrops;
        disallowed |= entry is ItemDropWithConditionRule { itemId: ItemID.JungleKey };
        disallowed |= entry is ItemDropWithConditionRule { itemId: ItemID.CorruptionKey };
        disallowed |= entry is ItemDropWithConditionRule { itemId: ItemID.CrimsonKey };
        disallowed |= entry is ItemDropWithConditionRule { itemId: ItemID.HallowedKey };
        disallowed |= entry is ItemDropWithConditionRule { itemId: ItemID.FrozenKey };
        disallowed |= entry is ItemDropWithConditionRule { itemId: ItemID.DungeonDesertKey };

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
}