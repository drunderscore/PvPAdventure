using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.World;

/// <summary>
/// Plays boss roar sounds audibly to all players regardless of distance, with some bosses only roaring if they haven't been defeated yet.
/// </summary>
public class GlobalBossRoar : GlobalNPC
{
    public override bool InstancePerEntity => true;

    private bool _roarPlayed = false;
    private static readonly Dictionary<int, (SoundStyle Sound, bool AlwaysGlobal)> BossRoars = new()
    {
        // Pre-Hardmode
        [NPCID.KingSlime] = (SoundID.Roar, false),
        [NPCID.EyeofCthulhu] = (SoundID.Roar, false),
        [NPCID.BrainofCthulhu] = (SoundID.Roar, false),
        [NPCID.QueenBee] = (SoundID.NPCDeath66, false),
        [NPCID.SkeletronHead] = (SoundID.Roar, false),
        [NPCID.Deerclops] = (SoundID.DeerclopsScream, false),
        [NPCID.WallofFlesh] = (SoundID.NPCDeath10, false),

        // Hardmode
        [NPCID.QueenSlimeBoss] = (SoundID.NPCDeath64, false),
        [NPCID.TheDestroyer] = (SoundID.Roar, false),
        [NPCID.SkeletronPrime] = (SoundID.Roar, false),
        [NPCID.Spazmatism] = (SoundID.Roar, false), 
        [NPCID.Plantera] = (SoundID.Roar, true),   // always global 
        [NPCID.Golem] = (SoundID.Roar, false),
        [NPCID.HallowBoss] = (SoundID.Item161, true),  // always global
        [NPCID.DukeFishron] = (SoundID.Roar, true),   // always global
        [NPCID.CultistBoss] = (SoundID.Roar, true),   // always global
        [NPCID.MoonLordCore] = (SoundID.Roar, false),
    };

    private static bool IsBossDowned(int npcType) => npcType switch
    {
        NPCID.KingSlime => NPC.downedSlimeKing,
        NPCID.EyeofCthulhu => NPC.downedBoss1,
        NPCID.EaterofWorldsHead => NPC.downedBoss2,
        NPCID.BrainofCthulhu => NPC.downedBoss2,
        NPCID.QueenBee => NPC.downedQueenBee,
        NPCID.SkeletronHead => NPC.downedBoss3,
        NPCID.Deerclops => NPC.downedDeerclops,
        NPCID.WallofFlesh => Main.hardMode,
        NPCID.QueenSlimeBoss => NPC.downedQueenSlime,
        NPCID.TheDestroyer => NPC.downedMechBoss1,
        NPCID.Spazmatism => NPC.downedMechBoss2,
        NPCID.SkeletronPrime => NPC.downedMechBoss3,
        NPCID.Plantera => NPC.downedPlantBoss,
        NPCID.Golem => NPC.downedGolemBoss,
        NPCID.HallowBoss => NPC.downedEmpressOfLight,
        NPCID.DukeFishron => NPC.downedFishron,
        NPCID.CultistBoss => NPC.downedAncientCultist,
        NPCID.MoonLordCore => NPC.downedMoonlord,
        _ => true,
    };
    public override void PostAI(NPC npc)
    {
        if (Main.netMode == NetmodeID.Server)
            return;

        if (_roarPlayed)
            return;

        _roarPlayed = true;

        if (!BossRoars.TryGetValue(npc.type, out var entry))
            return;

        if (IsBossDowned(npc.type) && !entry.AlwaysGlobal)
            return;
        SoundEngine.PlaySound(entry.Sound with { Volume = 3f });
    }
}