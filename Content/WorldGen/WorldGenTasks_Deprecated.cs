//using PvPAdventure.Core.Helpers;
//using System.Collections.Generic;
//using Terraria;
//using Terraria.DataStructures;
//using Terraria.GameContent.Generation;
//using Terraria.ID;
//using Terraria.IO;
//using Terraria.ModLoader;
//using Terraria.WorldBuilding;

//namespace PvPAdventure.Content.WorldGen;

//public sealed class WorldGenTasks : ModSystem
//{
//    public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
//    {
//        // Here, we can modify which task we want to insert our pass after.
//        // https://github.com/tModLoader/tModLoader/wiki/Vanilla-World-Generation-Steps
//        int index = tasks.FindIndex(gp => gp.Name == "Final Cleanup");

//        // Null check
//        if (index == -1)
//        {
//            Log.Error("Failed to find 'Final Cleanup' worldgen task.");
//            return;
//        }

//        // Insert a new pass after the found index.
//        tasks.Insert(index + 1,
//            new PassLegacy(
//                name: $"{Mod.Name}: Two Sunflower Trees", 
//                method: (progress, config) => PlaceTwoSunflowerTrees(progress)));
//    }

//    private static void PlaceTwoSunflowerTrees(GenerationProgress progress)
//    {
//        // Set progress message
//        progress.Message = "Planting two sunflower trees...";

//        // Find starting X
//        int startX = Main.spawnTileX - 25;

//        // Find ground Y by scanning downwards from spawn point
//        int y = Main.spawnTileY;
//        while (y < Main.maxTilesY - 10)
//        {
//            //&& Main.tile[startX, y].TileType == TileID.Grass
//            // ^^ This condition was removed to allow placement on any solid tile
//            if (Main.tile[startX, y].HasTile)
//                break;
//            y++;
//        }

//        var mod = ModContent.GetInstance<PvPAdventure>();
//        string structurePath = "Content/WorldGen/Spawnbox";

//        // Set structure size
//        Point16 size = StructureHelper.API.Generator.GetStructureDimensions(structurePath, mod);
//        int placeX = startX;
//        int placeY = y - size.Y;

//        // TEMP REPLACE WITH -25
//        placeX = Main.spawnTileX - 25;
//        placeY = Main.spawnTileY - 25;

//        // Generate the structure at the calculated position
//        StructureHelper.API.Generator.GenerateStructure(
//            path: structurePath,
//            pos: new Point16(placeX, placeY),
//            mod: mod
//        );

//        Log.Info($"Placed {structurePath} at: ({placeX}, {placeY}) with size: {size.X}x{size.Y}");
//    }
//}
