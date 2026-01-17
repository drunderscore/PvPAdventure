using PvPAdventure.Common.Npcs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Content.NPCs;

// FIXME: Don't actually face towards with aiStyle 0 -- probably need an PreAI override
public class Cyborg : BoundNPC
{
    public override int TransformInto => NPCID.Cyborg;

    public override void SetDefaults()
    {
        base.SetDefaults();

        NPC.width = 34;
        NPC.height = 8;
    }

    public override float SpawnChance(NPCSpawnInfo spawnInfo)
    {
        // FIXME: We need to check if the Cyborg has already moved in once before.

        // Don't spawn if we shouldn't.
        if (!NPC.downedPlantBoss)
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
