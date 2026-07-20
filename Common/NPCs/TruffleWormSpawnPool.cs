using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

/// <summary>
/// Changes vanilla truffle worm spawn conditions
/// </summary>
namespace PvPAdventure.Common.NPCs;

public class TruffleWormSpawnPool : GlobalNPC
{
    private const float SpawnWeight = 0.2f;

    public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
    {
        if (!NPC.downedGolemBoss)
            return;

        if (!spawnInfo.Player.ZoneGlowshroom)
            return;

        if (spawnInfo.SpawnTileY <= (int)Main.worldSurface)
            return;

        bool validTile = spawnInfo.SpawnTileType == TileID.MushroomGrass
            || spawnInfo.SpawnTileType == TileID.Mud;

        if (!validTile)
            return;

        pool[NPCID.TruffleWorm] = SpawnWeight;
    }
}