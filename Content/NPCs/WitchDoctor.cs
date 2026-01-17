using PvPAdventure.Common.Npcs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Content.NPCs;

public class WitchDoctor : BoundNPC
{
    public override int TransformInto => NPCID.WitchDoctor;

    public override float SpawnChance(NPCSpawnInfo spawnInfo)
    {
        // FIXME: We need to check if the Witch Doctor has already moved in once before.

        // Don't spawn if we shouldn't.
        if (!NPC.downedQueenBee)
            return 0.0f;

        // Don't spawn if we aren't in the caverns layer.
        if (!spawnInfo.Player.ZoneRockLayerHeight)
            return 0.0f;

        // FIXME: What is this doing...? this is what bound goblin does!
        if (spawnInfo.SpawnTileY >= Main.maxTilesY - 210)
            return 0.0f;

        return base.SpawnChance(spawnInfo);
    }
}
