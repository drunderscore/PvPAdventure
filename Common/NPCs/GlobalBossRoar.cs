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
        [NPCID.Plantera] = (SoundID.Roar, true),
        [NPCID.Golem] = (SoundID.Roar, false),
        [NPCID.HallowBoss] = (SoundID.Item161, true),
        [NPCID.DukeFishron] = (SoundID.Roar, true),
        [NPCID.CultistBoss] = (SoundID.Roar, true),
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

/// <summary>
/// Handles global sound cues for world events
/// </summary>
public class WorldEventSounds : ModSystem
{
    private bool _goblinSoundPlayed = false;
    private bool _martianSoundPlayed = false;

    public override void OnWorldLoad()
    {
        _goblinSoundPlayed = false;
        _martianSoundPlayed = false;
    }

    public override void PostUpdateWorld()
    {
        if (Main.netMode == NetmodeID.Server)
            return;

        // Goblin Army
        if (Main.invasionType == InvasionID.GoblinArmy && !_goblinSoundPlayed)
        {
            _goblinSoundPlayed = true;
            SoundEngine.PlaySound(SoundID.Dolphin with { Volume = 3f });
        }
        else if (Main.invasionType != InvasionID.GoblinArmy)
        {
            _goblinSoundPlayed = false;
        }

        // Martian Invasion
        if (Main.invasionType == InvasionID.MartianMadness && !_martianSoundPlayed)
        {
            _martianSoundPlayed = true;
            SoundEngine.PlaySound(SoundID.Dolphin with { Volume = 3f });
        }
        else if (Main.invasionType != InvasionID.MartianMadness)
        {
            _martianSoundPlayed = false;
        }
    }
}

/// <summary>
/// Plays a sound when a demon altar is broken for the first time.
/// </summary>
public class DemonAltarSounds : GlobalTile
{
    public static bool FirstAltarBroken = false;

    public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
    {
        if (Main.netMode == NetmodeID.Server)
            return;

        if (type != TileID.DemonAltar)
            return;

        if (fail || effectOnly)
            return;

        if (FirstAltarBroken)
            return;

        FirstAltarBroken = true;
        SoundEngine.PlaySound(SoundID.DD2_FlameburstTowerShot with { Volume = 3f });
    }
}

public class DemonAltarSoundsSystem : ModSystem
{
    public override void OnWorldLoad()
    {
        DemonAltarSounds.FirstAltarBroken = false;
    }
}