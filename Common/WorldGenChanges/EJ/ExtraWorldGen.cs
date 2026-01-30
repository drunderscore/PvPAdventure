using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Biomes;
using Terraria.GameContent.Generation;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace PvPAdventure.Common.WorldGenChanges.EJ;

public class ExtraWorldGen : ModSystem
{
    public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
    {
        AddExtraPyramids(tasks);
        AddExtraMahoganyTrees(tasks);
        AddExtraMinecartTracks(tasks);
        AddExtraHives(tasks);
        AddExtraSkyIslands(tasks);
        AddExtraCaveHouses(tasks);
        AddExtraTreasures(tasks);

        // New biome additions
        AddExtraDeadMansChests(tasks);
        AddExtraCorruptionPits(tasks);
        AddExtraGraniteBiomes(tasks);
        AddExtraMarbleBiomes(tasks);

        // New features
        AddExtraLivingTrees(tasks);

        ModContent.GetInstance<PvPAdventure>().Logger.Info("Added extra worldgen passes for multiple structures");
    }

    private void AddExtraPyramids(List<GenPass> tasks)
    {
        int pyramidsIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Pyramids"));
        if (pyramidsIndex == -1) return;

        tasks.Insert(pyramidsIndex + 1, new PassLegacy("Extra Pyramids", delegate (GenerationProgress progress, GameConfiguration passConfig)
        {
            progress.Message = "Adding extra pyramids...";
            int extraPyramids = WorldGen.genRand.Next(1, 2);

            for (int i = 0; i < extraPyramids; i++)
            {
                int attempts = 0;
                bool placed = false;

                while (!placed && attempts < 1000)
                {
                    int x = WorldGen.genRand.Next(300, Main.maxTilesX - 300);
                    int y = (int)Main.worldSurface - 10;

                    if (WorldGen.InWorld(x, y) && Main.tile[x, y].TileType == TileID.Sand)
                    {
                        placed = WorldGen.Pyramid(x, y);
                    }
                    attempts++;
                }
            }
        }));
    }
    // NOT CURRENTLY WORKING
    private void AddExtraMahoganyTrees(List<GenPass> tasks)
    {
        int jungleTreesIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Jungle Trees"));
        if (jungleTreesIndex == -1) return;

        tasks.Insert(jungleTreesIndex + 1, new PassLegacy("Extra Rich Mahogany Trees", delegate (GenerationProgress progress, GameConfiguration passConfig)
        {
            progress.Message = "Planting extra mahogany trees...";
            try
            {
                var mahoganyTreeBiome = new MahoganyTreeBiome();
                int extraTrees = WorldGen.genRand.Next(40, 80);
                int treesPlaced = 0;
                int attempts = 0;

                while (treesPlaced < extraTrees && attempts < 10000)
                {
                    int x = WorldGen.genRand.Next(200, Main.maxTilesX - 200);
                    int y = WorldGen.genRand.Next((int)Main.worldSurface + 100, (int)Main.rockLayer + 100);
                    Point point = new Point(x, y);

                    if (IsJungleArea(point.X, point.Y))
                    {
                        if (mahoganyTreeBiome.Place(point, GenVars.structures))
                        {
                            treesPlaced++;
                            attempts = 0; // Reset attempts counter on success
                        }
                    }
                    attempts++;
                }
                ModContent.GetInstance<PvPAdventure>().Logger.Info($"Placed {treesPlaced} extra mahogany trees");
            }
            catch (Exception ex)
            {
                ModContent.GetInstance<PvPAdventure>().Logger.Info($"Mahogany tree generation failed: {ex.Message}");
            }
        }));
    }

    private bool IsJungleArea(int x, int y)
    {
        // Check a small area around the point for jungle tiles
        int jungleTiles = 0;
        int totalTiles = 0;

        for (int checkX = x - 10; checkX <= x + 10; checkX += 2)
        {
            for (int checkY = y - 10; checkY <= y + 10; checkY += 2)
            {
                if (WorldGen.InWorld(checkX, checkY))
                {
                    Tile tile = Framing.GetTileSafely(checkX, checkY);
                    if (tile.HasTile && (tile.TileType == TileID.JungleGrass || tile.TileType == TileID.Mud))
                    {
                        jungleTiles++;
                    }
                    totalTiles++;
                }
            }
        }

        return totalTiles > 0 && (jungleTiles * 100 / totalTiles) >= 25;
    }

    //NOT CURRENTLY WORKING
    private void AddExtraLivingTrees(List<GenPass> tasks)
    {
        int livingTreesIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Living Trees"));
        if (livingTreesIndex == -1) return;

        tasks.Insert(livingTreesIndex + 1, new PassLegacy("Extra Surface Living Trees", delegate (GenerationProgress progress, GameConfiguration passConfig)
        {
            progress.Message = "Growing extra living trees...";
            int extraTrees = WorldGen.genRand.Next(300, 600);
            int treesPlaced = 0;
            int attempts = 0;

            while (treesPlaced < extraTrees && attempts < 1000)
            {
                int x = WorldGen.genRand.Next(200, Main.maxTilesX - 200);
                int y = WorldGen.genRand.Next((int)Main.worldSurface - 20, (int)Main.worldSurface + 10);

                if (WorldGen.InWorld(x, y) && IsValidLivingTreeLocation(x, y))
                {
                    if (WorldGen.GrowTree(x, y) || WorldGen.GrowTree(x - 1, y) || WorldGen.GrowTree(x + 1, y))
                    {
                        treesPlaced++;
                        attempts = 0;
                    }
                }
                attempts++;
            }
            ModContent.GetInstance<PvPAdventure>().Logger.Info($"Placed {treesPlaced} extra living trees");
        }));
    }

    private bool IsValidLivingTreeLocation(int x, int y)
    {
        if (!WorldGen.InWorld(x, y) || !Main.tile[x, y].HasTile)
            return false;

        Tile tile = Framing.GetTileSafely(x, y);
        if (tile.TileType != TileID.Grass && tile.TileType != TileID.HallowedGrass)
            return false;

        for (int checkY = y - 10; checkY >= y - 30; checkY--)
        {
            if (WorldGen.InWorld(x, checkY) && Main.tile[x, checkY].HasTile)
                return false;
        }

        return true;
    }
    //WORKING
    private void AddExtraCaveHouses(List<GenPass> tasks)
    {
        int buriedChestsIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Buried Chests"));
        if (buriedChestsIndex == -1) return;

        tasks[buriedChestsIndex] = new PassLegacy("Buried Chests", delegate (GenerationProgress progress, GameConfiguration passConfig)
        {
            progress.Message = Lang.gen[30].Value;
            Main.tileSolid[226] = true;
            Main.tileSolid[162] = true;
            Main.tileSolid[225] = true;
            CaveHouseBiome caveHouseBiome = GenVars.configuration.CreateBiome<CaveHouseBiome>();

            int random = passConfig.Get<WorldGenRange>("CaveHouseCount").GetRandom(WorldGen.genRand) * 4;
            int random2 = passConfig.Get<WorldGenRange>("UnderworldChestCount").GetRandom(WorldGen.genRand) * 1;
            int num = passConfig.Get<WorldGenRange>("CaveChestCount").GetRandom(WorldGen.genRand) * 2;
            int random3 = passConfig.Get<WorldGenRange>("AdditionalDesertHouseCount").GetRandom(WorldGen.genRand) * 2;

            if (Main.starGame)
            {
                num = (int)((double)num * Main.starGameMath(0.2));
            }
            int num2 = random + random2 + num + random3;
            int num3 = 10000;
            int num4 = 0;
            while (num4 < num && num3 > 0)
            {
                progress.Set((double)num4 / (double)num2);
                int num5 = WorldGen.genRand.Next(20, Main.maxTilesX - 20);
                int num6 = WorldGen.genRand.Next((int)((GenVars.worldSurfaceHigh + 20.0 + Main.rockLayer) / 2.0), Main.maxTilesY - 230);
                if (WorldGen.remixWorldGen)
                {
                    num6 = WorldGen.genRand.Next((int)Main.worldSurface, Main.maxTilesY - 400);
                }
                ushort wall = Framing.GetTileSafely(num5, num6).WallType;
                if (Main.wallDungeon[wall] || wall == 87 || WorldGen.oceanDepths(num5, num6) || !WorldGen.AddBuriedChest(num5, num6, 0, false, -1, false, 0))
                {
                    num3--;
                    num4--;
                }
                num4++;
            }
            num3 = 10000;
            int num7 = 0;
            while (num7 < random2 && num3 > 0)
            {
                progress.Set((double)(num7 + num) / (double)num2);
                int num8 = WorldGen.genRand.Next(20, Main.maxTilesX - 20);
                int num9 = WorldGen.genRand.Next(Main.UnderworldLayer, Main.maxTilesY - 50);
                if (Main.wallDungeon[Framing.GetTileSafely(num8, num9).WallType] || !WorldGen.AddBuriedChest(num8, num9, 0, false, -1, false, 0))
                {
                    num3--;
                    num7--;
                }
                num7++;
            }
            num3 = 10000;
            int num10 = 0;
            while (num10 < random && num3 > 0)
            {
                progress.Set((double)(num10 + num + random2) / (double)num2);
                int x = WorldGen.genRand.Next(80, Main.maxTilesX - 80);
                int y = WorldGen.genRand.Next((int)(GenVars.worldSurfaceHigh + 20.0), Main.maxTilesY - 230);
                if (WorldGen.remixWorldGen)
                {
                    y = WorldGen.genRand.Next((int)Main.worldSurface, Main.maxTilesY - 400);
                }
                if (WorldGen.oceanDepths(x, y) || !caveHouseBiome.Place(new Point(x, y), GenVars.structures))
                {
                    num3--;
                    num10--;
                }
                num10++;
            }
            num3 = 10000;
            Rectangle undergroundDesertHiveLocation = GenVars.UndergroundDesertHiveLocation;
            if ((double)undergroundDesertHiveLocation.Y < Main.worldSurface + 26.0)
            {
                int num11 = (int)Main.worldSurface + 26 - undergroundDesertHiveLocation.Y;
                undergroundDesertHiveLocation.Y += num11;
                undergroundDesertHiveLocation.Height -= num11;
            }
            int num12 = 0;
            while (num12 < random3 && num3 > 0)
            {
                progress.Set((double)(num12 + num + random2 + random) / (double)num2);
                if (!caveHouseBiome.Place(WorldGen.RandomRectanglePoint(undergroundDesertHiveLocation), GenVars.structures))
                {
                    num3--;
                    num12--;
                }
                num12++;
            }
            Main.tileSolid[226] = false;
            Main.tileSolid[162] = false;
            Main.tileSolid[225] = false;

            ModContent.GetInstance<PvPAdventure>().Logger.Info($"Placed cave houses: {num10} regular, {num12} desert, {num4} cave chests, {num7} underworld chests");
        });
    }

    private void AddExtraMinecartTracks(List<GenPass> tasks)
    {
        int trapsIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Traps"));
        if (trapsIndex == -1) return;

        tasks.Insert(trapsIndex + 1, new PassLegacy("Extra Minecart Tracks", delegate (GenerationProgress progress, GameConfiguration passConfig)
        {
            progress.Message = "Laying extra minecart tracks...";

            try
            {
                var trackGenerator = new TrackGenerator();
                GenerateTracks(trackGenerator, 15, 20, 300, 2000);
                GenerateTracks(trackGenerator, 15, 22, 100, 300);
            }
            catch
            {
                ModContent.GetInstance<PvPAdventure>().Logger.Info("TrackGenerator not available, using alternative minecart track generation");
                GenerateAlternativeTracks();
            }
        }));
    }
    // WORKING
    private void GenerateTracks(TrackGenerator trackGenerator, int minCount, int maxCount, int minLength, int maxLength)
    {
        int trackCount = WorldGen.genRand.Next(minCount, maxCount);
        int placed = 0;
        int attempts = 0;

        while (placed < trackCount && attempts < Main.maxTilesX)
        {
            Point point = WorldGen.RandomWorldPoint((int)Main.worldSurface, 10, 200, 10);
            if (trackGenerator.Place(point, minLength, maxLength))
            {
                placed++;
                attempts = 0;
            }
            else
            {
                attempts++;
            }
        }
    }

    private void GenerateAlternativeTracks()
    {
        int extraTracks = WorldGen.genRand.Next(10, 18);
        for (int i = 0; i < extraTracks; i++)
        {
            int attempts = 0;
            bool placed = false;

            while (!placed && attempts < 500)
            {
                int x = WorldGen.genRand.Next(200, Main.maxTilesX - 200);
                int y = WorldGen.genRand.Next((int)Main.worldSurface + 50, (int)Main.rockLayer + 100);

                if (WorldGen.InWorld(x, y))
                {
                    WorldGen.TileRunner(x, y, WorldGen.genRand.Next(8, 15), WorldGen.genRand.Next(50, 100), -1, false, 0f, 0f, true, true);

                    for (int trackX = x - 5; trackX <= x + 5; trackX++)
                    {
                        if (WorldGen.InWorld(trackX, y) && !Main.tile[trackX, y].HasTile)
                        {
                            WorldGen.PlaceTile(trackX, y, TileID.MinecartTrack, true, false);
                        }
                    }
                    placed = true;
                }
                attempts++;
            }
        }
    }
    //WORKING
    private void AddExtraHives(List<GenPass> tasks)
    {
        int hivesIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Hives"));
        if (hivesIndex == -1) hivesIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Life Crystals"));
        if (hivesIndex == -1) return;

        tasks.Insert(hivesIndex + 1, new PassLegacy("Extra Hives", delegate (GenerationProgress progress, GameConfiguration passConfig)
        {
            progress.Message = "Building extra hives...";

            try
            {
                var hiveBiome = GenVars.configuration.CreateBiome<HiveBiome>();
                var honeyPatchBiome = GenVars.configuration.CreateBiome<HoneyPatchBiome>();
                double hiveNum = (double)Main.maxTilesX / 4200.0;
                int extraHives = WorldGen.genRand.Next((int)(7.0 * hiveNum), (int)(7.0 * hiveNum));

                for (int i = 0; i < extraHives; i++)
                {
                    int attempts = 0;
                    bool placed = false;

                    while (!placed && attempts < 1000)
                    {
                        Point point = WorldGen.RandomWorldPoint((int)(Main.worldSurface + Main.rockLayer) >> 1, 20, 300, 20);

                        if (hiveBiome.Place(point, GenVars.structures))
                        {
                            placed = true;
                            AddHoneyPatches(honeyPatchBiome, point);
                        }
                        attempts++;
                    }
                }
            }
            catch
            {
                ModContent.GetInstance<PvPAdventure>().Logger.Info("HiveBiome not available, using alternative hive generation");
                GenerateAlternativeHives();
            }
        }));
    }

    private void AddHoneyPatches(HoneyPatchBiome honeyPatchBiome, Point point)
    {
        int honeyPatches = WorldGen.genRand.Next(3);
        int honeyPlaced = 0;
        int honeyAttempts = 0;

        while (honeyPlaced < honeyPatches && honeyAttempts < 100)
        {
            double distance = WorldGen.genRand.NextDouble() * 60.0 + 30.0;
            double angle = WorldGen.genRand.NextDouble() * 6.2831854820251465;
            int honeyX = (int)(Math.Cos(angle) * distance) + point.X;
            int honeyY = (int)(Math.Sin(angle) * distance) + point.Y;
            honeyAttempts++;

            if (honeyPatchBiome.Place(new Point(honeyX, honeyY), GenVars.structures))
            {
                honeyPlaced++;
            }
        }
    }

    private void GenerateAlternativeHives()
    {
        int extraHives = WorldGen.genRand.Next(4, 8);
        for (int i = 0; i < extraHives; i++)
        {
            int attempts = 0;
            bool placed = false;

            while (!placed && attempts < 500)
            {
                int x = WorldGen.genRand.Next(200, Main.maxTilesX - 200);
                int y = WorldGen.genRand.Next((int)((Main.worldSurface + Main.rockLayer) / 2), (int)Main.rockLayer + 200);

                if (IsJungleTile(Main.tile[x, y]))
                {
                    int hiveSize = WorldGen.genRand.Next(30, 60);
                    WorldGen.TileRunner(x, y, hiveSize, 1, TileID.Hive, false, 0f, 0f, true, true);
                    WorldGen.TileRunner(x, y, hiveSize - 10, 1, -1, false, 0f, 0f, true, true);

                    if (WorldGen.genRand.NextBool(3))
                    {
                        WorldGen.TileRunner(x, y, WorldGen.genRand.Next(5, 15), 1, TileID.HoneyBlock, false, 0f, 0f, true, true);
                    }
                    placed = true;
                }
                attempts++;
            }
        }
    }
    //WORKING
    private void AddExtraSkyIslands(List<GenPass> tasks)
    {
        int floatingIslandHousesIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Floating Island Houses"));
        if (floatingIslandHousesIndex == -1) return;

        tasks.Insert(floatingIslandHousesIndex + 1, new PassLegacy("Extra Sky Islands", delegate (GenerationProgress progress, GameConfiguration passConfig)
        {
            progress.Message = "Creating extra sky islands...";
            int extraIslands = WorldGen.genRand.Next(2, 4);

            for (int i = 0; i < extraIslands; i++)
            {
                int attempts = 0;
                bool placed = false;

                while (!placed && attempts < 1000)
                {
                    int x = WorldGen.genRand.Next((int)(Main.maxTilesX * 0.1), (int)(Main.maxTilesX * 0.9));
                    int y = WorldGen.genRand.Next(90, (int)Main.worldSurface - 150);

                    if (IsValidIslandLocation(x) && WorldGen.InWorld(x, y))
                    {
                        CreateSkyIsland(x, y);
                        placed = true;
                    }
                    attempts++;
                }
            }
        }));
    }

    private bool IsValidIslandLocation(int x)
    {
        return !(x > Main.maxTilesX / 2 - 150 && x < Main.maxTilesX / 2 + 150);
    }

    private void CreateSkyIsland(int x, int y)
    {
        WorldGen.CloudIsland(x, y);

        int islandStyle = GetIslandStyle(x);
        WorldGen.IslandHouse(x, y, islandStyle);
        PlantIslandGrass(x, y);
    }

    private int GetIslandStyle(int x)
    {
        if (WorldGen.remixWorldGen && WorldGen.drunkWorldGen)
        {
            bool flag = (GenVars.crimsonLeft && x < Main.maxTilesX / 2) || (!GenVars.crimsonLeft && x > Main.maxTilesX / 2);
            return flag ? 5 : 4;
        }
        else if (WorldGen.getGoodWorldGen || WorldGen.remixWorldGen)
        {
            return WorldGen.crimson ? 5 : 4;
        }
        else if (Main.tenthAnniversaryWorld)
        {
            return 6;
        }
        return 0;
    }

    private void PlantIslandGrass(int x, int y)
    {
        for (int grassX = x - 50; grassX < x + 50; grassX++)
        {
            for (int grassY = y - 20; grassY < y + 50; grassY++)
            {
                if (WorldGen.InWorld(grassX, grassY))
                {
                    Tile tile = Main.tile[grassX, grassY];
                    if (tile.HasTile && tile.TileType == TileID.Dirt)
                    {
                        WorldGen.PlaceTile(grassX, grassY, TileID.Grass, true, true);
                    }
                }
            }
        }
    }

    private void AddExtraTreasures(List<GenPass> tasks)
    {
        int finalCleanupIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Final Cleanup"));
        if (finalCleanupIndex == -1) return;

        tasks.Insert(finalCleanupIndex, new PassLegacy("Extra Life Crystals and Chests", delegate (GenerationProgress progress, GameConfiguration passConfig)
        {
            progress.Message = "Hiding extra treasures...";
            GenerateExtraLifeCrystals();
        }));
    }

    private void GenerateExtraLifeCrystals()
    {
        int extraLifeCrystals = Main.maxTilesX / 20;
        for (int i = 0; i < extraLifeCrystals; i++)
        {
            int attempts = 0;
            bool placed = false;

            while (!placed && attempts < 1000)
            {
                int x = WorldGen.genRand.Next(Main.offLimitBorderTiles, Main.maxTilesX - Main.offLimitBorderTiles);
                int y = WorldGen.genRand.Next((int)(Main.worldSurface * 2.0 + Main.rockLayer) / 3, Main.maxTilesY - 300);

                if (WorldGen.AddLifeCrystal(x, y)) placed = true;
                attempts++;
            }
        }
    }
    //NOT WORKING (needs to be tested)
    private void AddExtraDeadMansChests(List<GenPass> tasks)
    {
        int deadMansChestIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Dead Mans Chest"));
        if (deadMansChestIndex == -1) return;

        tasks.Insert(deadMansChestIndex + 1, new PassLegacy("Extra Dead Mans Chests", delegate (GenerationProgress progress, GameConfiguration passConfig)
        {
            progress.Message = "Placing extra dead mans chests...";

            int extraChests = WorldGen.genRand.Next(50, 150);
            int chestsPlaced = 0;
            int attempts = 0;

            while (chestsPlaced < extraChests && attempts < 5000)
            {
                int x = WorldGen.genRand.Next(200, Main.maxTilesX - 200);
                int y = WorldGen.genRand.Next((int)Main.worldSurface + 50, (int)Main.rockLayer + 200);

                if (WorldGen.InWorld(x, y) && IsValidSurfaceLocation(x, y))
                {
                    // Use vanilla dead man's chest placement
                    if (WorldGen.AddBuriedChest(x, y, 0, false, 2))
                    {
                        chestsPlaced++;
                    }
                }
                attempts++;
            }

            ModContent.GetInstance<PvPAdventure>().Logger.Info($"Placed {chestsPlaced} extra dead mans chests");
        }));
    }
    //NOT WORKING
    private void AddExtraCorruptionPits(List<GenPass> tasks)
    {
        int corruptionPitsIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Corruption Pits"));
        if (corruptionPitsIndex == -1) return;

        tasks.Insert(corruptionPitsIndex + 1, new PassLegacy("Extra Corruption Pits", delegate (GenerationProgress progress, GameConfiguration passConfig)
        {
            progress.Message = "Digging extra corruption pits...";

            try
            {
                // Use the built-in CorruptionPitBiome system
                var corruptionPitBiome = GenVars.configuration.CreateBiome<CorruptionPitBiome>();
                int extraPits = WorldGen.genRand.Next(50, 150);
                int pitsPlaced = 0;
                int attempts = 0;

                while (pitsPlaced < extraPits && attempts < 10000)
                {
                    Point point = WorldGen.RandomWorldPoint((int)Main.worldSurface, 10, 200, 10);

                    if (corruptionPitBiome.Place(point, GenVars.structures))
                    {
                        pitsPlaced++;
                    }
                    attempts++;
                }

                ModContent.GetInstance<PvPAdventure>().Logger.Info($"Placed {pitsPlaced} extra corruption pits");
            }
            catch (Exception ex)
            {
                ModContent.GetInstance<PvPAdventure>().Logger.Info($"Corruption pit generation failed: {ex.Message}");
            }
        }));
    }
    //WORKING
    private void AddExtraGraniteBiomes(List<GenPass> tasks)
    {
        int graniteIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Granite"));
        if (graniteIndex == -1) return;

        tasks.Insert(graniteIndex + 1, new PassLegacy("Extra Granite Biomes", delegate (GenerationProgress progress, GameConfiguration passConfig)
        {
            progress.Message = "Forming extra granite biomes...";

            try
            {
                var graniteBiome = GenVars.configuration.CreateBiome<GraniteBiome>();
                int extraBiomes = WorldGen.genRand.Next(3, 7);
                int biomesPlaced = 0;
                int attempts = 0;

                while (biomesPlaced < extraBiomes && attempts < 10000)
                {
                    Point point = WorldGen.RandomWorldPoint((int)Main.rockLayer, 50, 200, 50);

                    if (graniteBiome.Place(point, GenVars.structures))
                    {
                        biomesPlaced++;
                    }
                    attempts++;
                }

                ModContent.GetInstance<PvPAdventure>().Logger.Info($"Placed {biomesPlaced} extra granite biomes");
            }
            catch
            {
                ModContent.GetInstance<PvPAdventure>().Logger.Info("GraniteBiome not available, skipping extra granite biomes");
            }
        }));
    }
    //WORKING

    private void AddExtraMarbleBiomes(List<GenPass> tasks)
    {
        int marbleIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Marble"));
        if (marbleIndex == -1) return;

        tasks.Insert(marbleIndex + 1, new PassLegacy("Extra Marble Biomes", delegate (GenerationProgress progress, GameConfiguration passConfig)
        {
            progress.Message = "Sculpting extra marble biomes...";

            try
            {
                var marbleBiome = GenVars.configuration.CreateBiome<MarbleBiome>();
                int extraBiomes = WorldGen.genRand.Next(3, 7);
                int biomesPlaced = 0;
                int attempts = 0;

                while (biomesPlaced < extraBiomes && attempts < 10000)
                {
                    Point point = WorldGen.RandomWorldPoint((int)Main.rockLayer, 50, 200, 50);

                    if (marbleBiome.Place(point, GenVars.structures))
                    {
                        biomesPlaced++;
                    }
                    attempts++;
                }

                ModContent.GetInstance<PvPAdventure>().Logger.Info($"Placed {biomesPlaced} extra marble biomes");
            }
            catch
            {
                ModContent.GetInstance<PvPAdventure>().Logger.Info("MarbleBiome not available, skipping extra marble biomes");
            }
        }));
    }
    // Helper Methods
    private bool IsJungleBiome(Point point)
    {
        for (int checkX = point.X - 5; checkX <= point.X + 5; checkX++)
        {
            for (int checkY = point.Y - 5; checkY <= point.Y + 5; checkY++)
            {
                if (WorldGen.InWorld(checkX, checkY))
                {
                    Tile tile = Main.tile[checkX, checkY];
                    if (tile.TileType == TileID.JungleGrass || tile.TileType == TileID.Mud)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private bool IsJungleTile(Tile tile)
    {
        return tile.TileType == TileID.Mud || tile.TileType == TileID.JungleGrass;
    }

    private bool IsCorruptionTile(Tile tile)
    {
        return tile.TileType == TileID.Ebonstone || tile.TileType == TileID.CorruptGrass;
    }

    private bool IsValidSurfaceLocation(int x, int y)
    {
        // Check if location is on surface with grass
        return Main.tile[x, y].TileType == TileID.Grass || Main.tile[x, y].TileType == TileID.Dirt;
    }

    private bool IsValidUndergroundLocation(int x, int y)
    {
        // Check if location is in underground with stone or dirt
        return Main.tile[x, y].TileType == TileID.Stone || Main.tile[x, y].TileType == TileID.Dirt;
    }
}
