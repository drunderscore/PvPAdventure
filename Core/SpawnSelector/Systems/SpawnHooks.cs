using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Core.SpawnSelector.Systems;

/// <summary>
/// This allows the player to click on teammates to teleport to them.
/// </summary>
public class SpawnHooks : ModSystem
{
    public override void Load()
    {
        On_Player.Spawn_SetPosition += ForceWorldSpawn;
    }

    public override void Unload()
    {
        On_Player.Spawn_SetPosition -= ForceWorldSpawn;
    }

    private void ForceWorldSpawn(On_Player.orig_Spawn_SetPosition orig, Player self, int floorX, int floorY)
    {
        //orig(self, floorX, floorY);

        // Force all respawns to world spawn
        int spawnX = Main.spawnTileX;
        int spawnY = Main.spawnTileY;
        //bool num = Player.Spawn_GetPositionAtWorldSpawn(ref floorX, ref floorY);
        //this.Spawn_SetPosition(floorX, floorY);
        //if (num && !this.Spawn_IsAreaAValidWorldSpawn(floorX, floorY))
        //{
        //Player.Spawn_ForceClearArea(floorX, floorY);
        //}

        if (self == Main.LocalPlayer)
        {
        Main.LocalPlayer.position.X = spawnX * 16 + 8 - Main.LocalPlayer.width / 2;
        Main.LocalPlayer.position.Y = spawnY * 16 - Main.LocalPlayer.height;
    }
        
    }
}
