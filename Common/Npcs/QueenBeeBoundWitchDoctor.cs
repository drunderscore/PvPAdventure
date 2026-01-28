using PvPAdventure.Content.NPCs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.NPCs;

/// <summary>
/// - Listens for Queen Bee death events <br/>
/// - Spawns a bound Witch Doctor NPC on first Queen Bee kill <br/>
/// - Marks the spawn as completed via BoundWitchDoctorSpawnSystem <br/>
/// - Syncs the spawned NPC in multiplayer <br/>
/// </summary>
public class QueenBeeBoundWitchDoctor : GlobalNPC
{
    public override void OnKill(NPC npc)
    {
        if (npc.type == NPCID.QueenBee)
        {
            var spawner = ModContent.GetInstance<BoundWitchDoctorSpawnSystem>();

            if (!spawner.hasSpawnedBoundWitchDoctor)
            {
                int npcIndex = NPC.NewNPC(
                    npc.GetSource_Death(),
                    (int)npc.Center.X,
                    (int)npc.Center.Y,
                    ModContent.NPCType<WitchDoctor>()
                );

                if (npcIndex >= 0 && npcIndex < Main.maxNPCs)
                {
                    NPC boundWitchDoctor = Main.npc[npcIndex];
                    boundWitchDoctor.netUpdate = true;
                }

                spawner.hasSpawnedBoundWitchDoctor = true;
            }
        }
    }
}

