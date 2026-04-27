using Terraria;
using Terraria.GameContent.Events;
using Terraria.ID;

namespace PvPAdventure.Common.Spectator.UI.Tabs.World;

public static class WorldBossInfoHelper
{
    public readonly record struct BossEntry(int NpcId, string Name, bool Downed);
    public static int CountActiveBosses()
    {
        int count = 0;

        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC npc = Main.npc[i];
            if (npc?.active == true && npc.boss)
                count++;
        }

        return count;
    }

    public static BossEntry[] GetBossEntries()
    {
        int evilBossId;
        string evilBossName;

        if (WorldGen.crimson)
        {
            evilBossId = NPCID.BrainofCthulhu;
            evilBossName = "Brain of Cthulhu";
        }
        else
        {
            evilBossId = NPCID.EaterofWorldsHead;
            evilBossName = "Eater of Worlds";
        }

        return
        [
            new BossEntry(NPCID.KingSlime, "King Slime", NPC.downedSlimeKing),
            new BossEntry(NPCID.EyeofCthulhu, "Eye of Cthulhu", NPC.downedBoss1),
            new BossEntry(evilBossId, evilBossName, NPC.downedBoss2),
            new BossEntry(NPCID.QueenBee, "Queen Bee", NPC.downedQueenBee),
            new BossEntry(NPCID.SkeletronHead, "Skeletron", NPC.downedBoss3),
            new BossEntry(NPCID.WallofFlesh, "Wall of Flesh", Main.hardMode),
            new BossEntry(NPCID.QueenSlimeBoss, "Queen Slime", NPC.downedQueenSlime),
            new BossEntry(NPCID.TheDestroyer, "The Destroyer", NPC.downedMechBoss1),
            new BossEntry(NPCID.Retinazer, "The Twins", NPC.downedMechBoss2),
            new BossEntry(NPCID.SkeletronPrime, "Skeletron Prime", NPC.downedMechBoss3),
            new BossEntry(NPCID.Plantera, "Plantera", NPC.downedPlantBoss),
            new BossEntry(NPCID.Golem, "Golem", NPC.downedGolemBoss),
            new BossEntry(NPCID.DukeFishron, "Duke Fishron", NPC.downedFishron),
            new BossEntry(NPCID.CultistBoss, "Lunatic Cultist", NPC.downedAncientCultist),
            new BossEntry(NPCID.MoonLordCore, "Moon Lord", NPC.downedMoonlord)
        ];
    }

    public static int GetBossHeadNpcId(int npcId)
    {
        return npcId == NPCID.Golem ? NPCID.GolemHead : npcId;
    }

    public static string GetBossesDefeatedText()
    {
        int defeated = 0;
        int total = 18;

        defeated += NPC.downedBoss1 ? 1 : 0; // Eye of Cthulhu
        defeated += NPC.downedBoss2 ? 1 : 0; // Eater / Brain
        defeated += NPC.downedQueenBee ? 1 : 0;
        defeated += NPC.downedBoss3 ? 1 : 0; // Skeletron
        defeated += Main.hardMode ? 1 : 0; // Wall of Flesh
        defeated += NPC.downedMechBoss1 ? 1 : 0;
        defeated += NPC.downedMechBoss2 ? 1 : 0;
        defeated += NPC.downedMechBoss3 ? 1 : 0;
        defeated += NPC.downedPlantBoss ? 1 : 0;
        defeated += NPC.downedGolemBoss ? 1 : 0;
        defeated += NPC.downedFishron ? 1 : 0;
        defeated += NPC.downedAncientCultist ? 1 : 0;
        defeated += NPC.downedMoonlord ? 1 : 0;
        defeated += NPC.downedSlimeKing ? 1 : 0;
        defeated += NPC.downedQueenSlime ? 1 : 0;
        defeated += NPC.downedEmpressOfLight ? 1 : 0;
        defeated += NPC.downedDeerclops ? 1 : 0;
        defeated += NPC.downedTowerSolar && NPC.downedTowerVortex && NPC.downedTowerNebula && NPC.downedTowerStardust ? 1 : 0;

        return $"{defeated}/{total}";
    }

}
