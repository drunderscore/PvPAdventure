using System.Linq;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

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

            case NPCID.Mothron:
                foreach (var drop in drops)
                    ModifyDropRate(drop, ItemID.BrokenHeroSword, 1, 2);
                break;

            case NPCID.SkeletonArcher:
                foreach (var drop in drops)
                    ModifyDropRate(drop, ItemID.MagicQuiver, 1, 30);
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

            case NPCID.GiantTortoise:
                foreach (var drop in drops)
                    ModifyDropRate(drop, ItemID.TurtleShell, 1, 5);
                break;

            case NPCID.GiantCursedSkull:
                foreach (var drop in drops)
                    ModifyDropRate(drop, ItemID.ShadowJoustingLance, 1, 12);
                break;
        }
    }

    public static IItemDropRule OnItemDropDatabaseRegisterToGlobal(On_ItemDropDatabase.orig_RegisterToGlobal orig,
        ItemDropDatabase self, IItemDropRule entry)
    {
        var adventureConfig = ModContent.GetInstance<AdventureConfig>();

        var disallowed = false;

        disallowed |= entry is MechBossSpawnersDropRule && adventureConfig.NpcBalance.NoMechanicalBossSummonDrops;

        if (adventureConfig.NpcBalance.NoBiomeKeyDrops)
        {
            disallowed |= entry is ItemDropWithConditionRule { itemId: ItemID.JungleKey };
            disallowed |= entry is ItemDropWithConditionRule { itemId: ItemID.CorruptionKey };
            disallowed |= entry is ItemDropWithConditionRule { itemId: ItemID.CrimsonKey };
            disallowed |= entry is ItemDropWithConditionRule { itemId: ItemID.HallowedKey };
            disallowed |= entry is ItemDropWithConditionRule { itemId: ItemID.FrozenKey };
            disallowed |= entry is ItemDropWithConditionRule { itemId: ItemID.DungeonDesertKey };
        }

        if (!disallowed)
            orig(self, entry);

        return entry;
    }
}