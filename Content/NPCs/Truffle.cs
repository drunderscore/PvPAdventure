using PvPAdventure.Common.Npcs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Content.NPCs;

public class Truffle : BoundNPC
{
    public override int TransformInto => NPCID.Truffle;

    public override void SetDefaults()
    {
        base.SetDefaults();

        NPC.width = 34;
        NPC.height = 8;
        NPC.dontTakeDamage = true;
    }

    public override float SpawnChance(NPCSpawnInfo spawnInfo)
    {
        // Don't spawn if we've already been unlocked.
        // Note this MUST come BEFORE NPC.SpawnAllowed_ArmsDealer, as it short-circuits based on the value we check.
        if (NPC.unlockedTruffleSpawn)
            return 0.0f;

        // Don't spawn if we shouldn't.
        if (!Main.hardMode)
            return 0.0f;

        // Don't spawn if we aren't in the mushroom biome.
        if (!spawnInfo.Player.ZoneGlowshroom)
            return 0.0f;

        // FIXME: What is this doing...? this is what bound goblin does!
        if (spawnInfo.SpawnTileY >= Main.maxTilesY - 210)
            return 0.0f;

        return base.SpawnChance(spawnInfo);
    }

    protected override void Transform(int whoAmI)
    {
        base.Transform(whoAmI);
        NPC.unlockedTruffleSpawn = true;
        NetMessage.SendData(MessageID.WorldData);
    }
}
