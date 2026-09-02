using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using PvPAdventure.Core.Config;
using Terraria.DataStructures;

namespace PvPAdventure.Common.World
{
    /// <summary>
    /// Triggers a Goblin Army invasion on the second dawn after Wall of Flesh is killed
    /// Also now spawns bound goblin from the last goblin in the invasion that is killed
    /// </summary>
    public class HardmodeGoblinInvasionSystem : ModSystem
    {
        private bool wasHardmode = false;
        private bool pendingGoblinInvasion = false;
        private bool hasSeenNightSinceHardmode = false;
        private bool wasDaytime = false;

        // Bound Goblin spawn state
        public bool hasSpawnedBoundGoblin = false;     
        public bool hasStoredKillPosition = false;   
        public Vector2 lastGoblinKillPosition;         
        public IEntitySource lastGoblinDeathSource;   

        public override void PostUpdateWorld()
        {
            if (!ModContent.GetInstance<ServerConfig>().StartHardmodeGoblinInvasion)
                return;

            if (Main.hardMode && !wasHardmode)
            {
                if (!NPC.downedGoblins)
                {
                    pendingGoblinInvasion = true;
                    hasSeenNightSinceHardmode = false;
                    hasSeenNightSinceHardmode = !Main.dayTime;
                }
                wasHardmode = true;
            }

            if (!Main.hardMode)
                wasHardmode = false;

            if (pendingGoblinInvasion)
            {
                bool isDaytime = Main.dayTime;
                if (!isDaytime)
                    hasSeenNightSinceHardmode = true;

                if (isDaytime && !wasDaytime && hasSeenNightSinceHardmode)
                {
                    Main.StartInvasion(InvasionID.GoblinArmy);
                    pendingGoblinInvasion = false;
                    hasSeenNightSinceHardmode = false;

                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.WorldData);
                }

                wasDaytime = isDaytime;
            }
            else
            {
                wasDaytime = Main.dayTime;
            }

            if (!hasSpawnedBoundGoblin && hasStoredKillPosition && Main.invasionSize <= 0)
            {
                IEntitySource source = lastGoblinDeathSource;
                int boundGoblinIndex = NPC.NewNPC(
                    source,
                    (int)lastGoblinKillPosition.X,
                    (int)lastGoblinKillPosition.Y,
                    NPCID.BoundGoblin
                );

                if (boundGoblinIndex >= 0 && boundGoblinIndex < Main.maxNPCs)
                {
                    Main.npc[boundGoblinIndex].netUpdate = true;
                }

                hasSpawnedBoundGoblin = true;
                hasStoredKillPosition = false;
                lastGoblinDeathSource = null;
            }
        }

        public override void SaveWorldData(TagCompound tag)
        {
            tag["wasHardmode"] = wasHardmode;
            tag["pendingGoblinInvasion"] = pendingGoblinInvasion;
            tag["hasSeenNightSinceHardmode"] = hasSeenNightSinceHardmode;
            tag["hasSpawnedBoundGoblin"] = hasSpawnedBoundGoblin;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            wasHardmode = tag.GetBool("wasHardmode");
            pendingGoblinInvasion = tag.GetBool("pendingGoblinInvasion");
            hasSeenNightSinceHardmode = tag.GetBool("hasSeenNightSinceHardmode");
            hasSpawnedBoundGoblin = tag.GetBool("hasSpawnedBoundGoblin");

            if (Main.hardMode)
                wasHardmode = true;

            wasDaytime = Main.dayTime;
        }
    }
    public class GoblinArmyBoundGoblin : GlobalNPC
    {
        public override void OnKill(NPC npc)
        {
            if (!IsGoblinArmyEnemy(npc.type))
                return;

            if (Main.invasionType != InvasionID.GoblinArmy)
                return;

            var system = ModContent.GetInstance<HardmodeGoblinInvasionSystem>();

            system.lastGoblinKillPosition = npc.Center;
            system.lastGoblinDeathSource = npc.GetSource_Death();
            system.hasStoredKillPosition = true;
        }

        private bool IsGoblinArmyEnemy(int npcType)
        {
            return npcType == NPCID.GoblinPeon ||
                   npcType == NPCID.GoblinArcher ||
                   npcType == NPCID.GoblinSorcerer ||
                   npcType == NPCID.GoblinThief ||
                   npcType == NPCID.GoblinWarrior ||
                   npcType == NPCID.GoblinSummoner;
        }
    }
}