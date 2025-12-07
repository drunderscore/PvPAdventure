using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace PvPAdventure.System
{
    // BossTrackingSystem.cs - Put this in its own file
    public class BossTrackingSystem : ModSystem
    {
        // Dictionary to track first kills for all bosses
        public static Dictionary<int, bool> FirstBossKills { get; set; } = new Dictionary<int, bool>();

        // Dictionary to track previous downed states
        public static Dictionary<string, bool> PreviousDownedStates { get; set; } = new Dictionary<string, bool>();

        // Initialize boss tracking
        public override void PostSetupContent()
        {
            InitializeBossTracking();
        }

        private void InitializeBossTracking()
        {
            // Initialize all boss first kill flags to false
            var bossTypes = new int[]
            {
            NPCID.KingSlime,
            NPCID.EyeofCthulhu,
            NPCID.EaterofWorldsHead,
            NPCID.BrainofCthulhu,
            NPCID.QueenBee,
            NPCID.SkeletronHead,
            NPCID.WallofFlesh,
            NPCID.QueenSlimeBoss,
            NPCID.TheDestroyer,
            NPCID.Retinazer,
            NPCID.Spazmatism,
            NPCID.SkeletronPrime,
            NPCID.Plantera,
            NPCID.Golem,
            NPCID.DukeFishron,
            NPCID.HallowBoss, // Empress of Light
            NPCID.CultistBoss,
            NPCID.MoonLordCore
            };

            foreach (int bossType in bossTypes)
            {
                if (!FirstBossKills.ContainsKey(bossType))
                    FirstBossKills[bossType] = false;
            }

            // Initialize previous downed states
            UpdatePreviousDownedStates();
        }

        private void UpdatePreviousDownedStates()
        {
            PreviousDownedStates["KingSlime"] = NPC.downedSlimeKing;
            PreviousDownedStates["EyeofCthulhu"] = NPC.downedBoss1;
            PreviousDownedStates["EvilBoss"] = NPC.downedBoss2; // Eater of Worlds or Brain of Cthulhu
            PreviousDownedStates["QueenBee"] = NPC.downedQueenBee;
            PreviousDownedStates["Skeletron"] = NPC.downedBoss3;
            PreviousDownedStates["WallofFlesh"] = Main.hardMode;
            PreviousDownedStates["QueenSlime"] = NPC.downedQueenSlime;
            PreviousDownedStates["Destroyer"] = NPC.downedMechBoss1;
            PreviousDownedStates["Twins"] = NPC.downedMechBoss2;
            PreviousDownedStates["SkeletronPrime"] = NPC.downedMechBoss3;
            PreviousDownedStates["Plantera"] = NPC.downedPlantBoss;
            PreviousDownedStates["Golem"] = NPC.downedGolemBoss;
            PreviousDownedStates["DukeFishron"] = NPC.downedFishron;
            PreviousDownedStates["EmpressOfLight"] = NPC.downedEmpressOfLight;
            PreviousDownedStates["Cultist"] = NPC.downedAncientCultist;
            PreviousDownedStates["MoonLord"] = NPC.downedMoonlord;
        }

        public override void PostUpdateWorld()
        {
            CheckForFirstBossDefeats();
        }

        private void CheckForFirstBossDefeats()
        {
            CheckBossDefeat("KingSlime", NPCID.KingSlime, NPC.downedSlimeKing);
            CheckBossDefeat("EyeofCthulhu", NPCID.EyeofCthulhu, NPC.downedBoss1);
            CheckBossDefeat("EvilBoss", NPC.downedBoss2 ? NPCID.EaterofWorldsHead : NPCID.BrainofCthulhu, NPC.downedBoss2);
            CheckBossDefeat("QueenBee", NPCID.QueenBee, NPC.downedQueenBee);
            CheckBossDefeat("Skeletron", NPCID.SkeletronHead, NPC.downedBoss3);
            CheckBossDefeat("WallofFlesh", NPCID.WallofFlesh, Main.hardMode);
            CheckBossDefeat("QueenSlime", NPCID.QueenSlimeBoss, NPC.downedQueenSlime);
            CheckBossDefeat("Destroyer", NPCID.TheDestroyer, NPC.downedMechBoss1);
            CheckBossDefeat("Twins", NPCID.Retinazer, NPC.downedMechBoss2); // Could also use Spazmatism
            CheckBossDefeat("SkeletronPrime", NPCID.SkeletronPrime, NPC.downedMechBoss3);
            CheckBossDefeat("Plantera", NPCID.Plantera, NPC.downedPlantBoss);
            CheckBossDefeat("Golem", NPCID.Golem, NPC.downedGolemBoss);
            CheckBossDefeat("DukeFishron", NPCID.DukeFishron, NPC.downedFishron);
            CheckBossDefeat("EmpressOfLight", NPCID.HallowBoss, NPC.downedEmpressOfLight);
            CheckBossDefeat("Cultist", NPCID.CultistBoss, NPC.downedAncientCultist);
            CheckBossDefeat("MoonLord", NPCID.MoonLordCore, NPC.downedMoonlord);
        }

        private void CheckBossDefeat(string bossKey, int npcID, bool currentDownedState)
        {
            if (!FirstBossKills.ContainsKey(npcID))
                return;

            // Get previous state, default to false if not found
            bool previousState = false;
            if (PreviousDownedStates != null && PreviousDownedStates.ContainsKey(bossKey))
                previousState = PreviousDownedStates[bossKey];

            // If boss wasn't defeated before but is now, mark first kill as complete
            if (!previousState && currentDownedState && !FirstBossKills[npcID])
            {
                FirstBossKills[npcID] = true;
            }

            // Update previous state
            if (PreviousDownedStates != null)
                PreviousDownedStates[bossKey] = currentDownedState;
        }

        private string GetBossName(int npcID)
        {
            return npcID switch
            {
                NPCID.KingSlime => "King Slime",
                NPCID.EyeofCthulhu => "Eye of Cthulhu",
                NPCID.EaterofWorldsHead => "Eater of Worlds",
                NPCID.BrainofCthulhu => "Brain of Cthulhu",
                NPCID.QueenBee => "Queen Bee",
                NPCID.SkeletronHead => "Skeletron",
                NPCID.WallofFlesh => "Wall of Flesh",
                NPCID.QueenSlimeBoss => "Queen Slime",
                NPCID.TheDestroyer => "The Destroyer",
                NPCID.Retinazer => "The Twins",
                NPCID.SkeletronPrime => "Skeletron Prime",
                NPCID.Plantera => "Plantera",
                NPCID.Golem => "Golem",
                NPCID.DukeFishron => "Duke Fishron",
                NPCID.HallowBoss => "Empress of Light",
                NPCID.CultistBoss => "Lunatic Cultist",
                NPCID.MoonLordCore => "Moon Lord",
                _ => "Unknown Boss"
            };
        }
    }
    public class FirstBossKillCondition : IItemDropRuleCondition
    {
        private readonly int bossType;

        public FirstBossKillCondition(int bossType)
        {
            this.bossType = bossType;
        }

        public bool CanDrop(DropAttemptInfo info)
        {
            return BossTrackingSystem.FirstBossKills.ContainsKey(bossType) && !BossTrackingSystem.FirstBossKills[bossType];
        }

        public bool CanShowItemDropInUI()
        {
            return BossTrackingSystem.FirstBossKills.ContainsKey(bossType) && !BossTrackingSystem.FirstBossKills[bossType];
        }

        public string GetConditionDescription()
        {
            return $"First {GetBossName(bossType)} kill in this world";
        }

        private string GetBossName(int npcID)
        {
            return npcID switch
            {
                NPCID.KingSlime => "King Slime",
                NPCID.EyeofCthulhu => "Eye of Cthulhu",
                NPCID.EaterofWorldsHead => "Eater of Worlds",
                NPCID.BrainofCthulhu => "Brain of Cthulhu",
                NPCID.QueenBee => "Queen Bee",
                NPCID.SkeletronHead => "Skeletron",
                NPCID.WallofFlesh => "Wall of Flesh",
                NPCID.QueenSlimeBoss => "Queen Slime",
                NPCID.TheDestroyer => "The Destroyer",
                NPCID.Retinazer => "The Twins",
                NPCID.SkeletronPrime => "Skeletron Prime",
                NPCID.Plantera => "Plantera",
                NPCID.Golem => "Golem",
                NPCID.DukeFishron => "Duke Fishron",
                NPCID.HallowBoss => "Empress of Light",
                NPCID.CultistBoss => "Lunatic Cultist",
                NPCID.MoonLordCore => "Moon Lord",
                _ => "Unknown Boss"
            };
        }
    }
}