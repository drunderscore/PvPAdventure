using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace PvPAdventure.Common.NPCs;

/// <summary>
/// - Tracks whether the bound Witch Doctor has spawned <br/>
/// - Persists spawn state in world save data <br/>
/// - Ensures the bound NPC only spawns once per world <br/>
/// </summary>
//yes this code is half AI slop dogshit and some of it is stolen from fargos I dont care its temporary anyways
public class BoundWitchDoctorSpawnSystem : ModSystem
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

