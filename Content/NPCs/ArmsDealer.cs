using PvPAdventure.Common.Npcs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Content.NPCs;

public class ArmsDealer : BoundNPC
{
    public override int TransformInto => NPCID.ArmsDealer;

    public override float SpawnChance(NPCSpawnInfo spawnInfo)
    {
        // Don't spawn if we've already been unlocked.
        // Note this MUST come BEFORE NPC.SpawnAllowed_ArmsDealer, as it short-circuits based on the value we check.
        if (NPC.unlockedArmsDealerSpawn)
            return 0.0f;

        // Don't spawn if we shouldn't.
        if (!NPC.SpawnAllowed_ArmsDealer())
            return 0.0f;

        // Don't spawn if we aren't in the caverns layer.
        if (!spawnInfo.Player.ZoneRockLayerHeight)
            return 0.0f;

        // FIXME: What is this doing...? this is what bound goblin does!
        if (spawnInfo.SpawnTileY >= Main.maxTilesY - 210)
            return 0.0f;

        return base.SpawnChance(spawnInfo);
    }

    protected override void Transform(int whoAmI)
    {
        base.Transform(whoAmI);
        NPC.unlockedArmsDealerSpawn = true;
    }
}
