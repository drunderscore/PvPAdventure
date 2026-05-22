using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
/// <summary>
/// Overrides the vanilla Prismatic Lacewing spawn conditions and replaces it with our own, as well as letting us boost the spawnrate.
/// </summary>
namespace PvPAdventure.Common.NPCs;
public class LacewingSpawnPool : GlobalNPC
{
    private const float SpawnWeight = 0.5f;

    public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
    {
        if (Main.dayTime)
            return;
        if (!NPC.downedGolemBoss)
            return;
        if (!spawnInfo.Player.ZoneHallow)
            return;
        if (spawnInfo.SpawnTileY > (int)Main.worldSurface)
            return;
        bool validTile = spawnInfo.SpawnTileType == TileID.HallowedGrass
            || spawnInfo.SpawnTileType == TileID.Pearlstone
            || spawnInfo.SpawnTileType == TileID.Pearlsand
            || spawnInfo.SpawnTileType == TileID.HallowedIce;
        if (!validTile)
            return;
        // Overwrite whatever vanilla put in the pool for this NPC type
        pool[NPCID.EmpressButterfly] = SpawnWeight;
    }
}