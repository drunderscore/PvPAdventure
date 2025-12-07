using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using PvPAdventure;

namespace PvPAdventure
{

    //yes this code is half AI slop dogshit and some of it is stolen from fargos I dont care its temporary anyways
    public class BoundWitchDoctorSpawner : ModSystem
    {
        public bool hasSpawnedBoundWitchDoctor = false;

        public override void SaveWorldData(TagCompound tag)
        {
            tag["BoundWitchDoctorSpawned"] = hasSpawnedBoundWitchDoctor;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            hasSpawnedBoundWitchDoctor = tag.GetBool("BoundWitchDoctorSpawned");
        }
    }

    public class QueenBeeGlobalNPC : GlobalNPC
    {
        public override void OnKill(NPC npc)
        {
            if (npc.type == NPCID.QueenBee)
            {
                var spawner = ModContent.GetInstance<BoundWitchDoctorSpawner>();

                if (!spawner.hasSpawnedBoundWitchDoctor)
                {
                    int npcIndex = NPC.NewNPC(npc.GetSource_Death(), (int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<BoundNpc.WitchDoctor>());

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
}