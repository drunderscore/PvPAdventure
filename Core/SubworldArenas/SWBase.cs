using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.WorldBuilding;
using SubworldLibrary;

namespace PvPAdventure.Core.SubworldArenas;

public abstract class SWBase : Subworld
{
    public override int Width => 1000;
    public override int Height => 1000;
    public override bool ShouldSave => false;
    public override bool NoPlayerSaving => true;

    public override List<GenPass> Tasks => new()
    {
        new ArenaGenPass()
    };

    public override void OnLoad()
    {
        Main.dayTime = true;
        Main.time = 27000;
    }
}

public class ArenaGenPass : GenPass
{
    public ArenaGenPass() : base("Terrain", 1) { }

    protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
    {
        Main.worldSurface = Main.maxTilesY - 42;
        Main.rockLayer = Main.maxTilesY;
        for (int i = 0; i < Main.maxTilesX; i++)
        {
            for (int j = 0; j < Main.maxTilesY; j++)
            {
                progress.Set((j + i * Main.maxTilesY) / (float)(Main.maxTilesX * Main.maxTilesY));
                Tile tile = Main.tile[i, j];
                tile.HasTile = true;
                tile.TileType = TileID.Dirt;
            }
        }
    }
}

public class SW1 : SWBase { }
public class SW2 : SWBase { }
public class SW3 : SWBase { }
public class SW4 : SWBase { }
