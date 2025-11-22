using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using System.Collections.Generic;
using Terraria.Chat;
using Terraria.Localization;
using Microsoft.Xna.Framework;
using Terraria.ModLoader.IO;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Reflection;
using System;
using Mono.Cecil.Cil;
using Terraria.GameContent.Generation;
using Terraria.IO;
using Terraria.WorldBuilding;
using System.IO;
using Terraria.DataStructures;
using Terraria.GameContent.Biomes;
namespace PvPAdventure
{
    public class IncreasedRainSystem : ModSystem
    {
        private int rainCheckTimer = 0;
        private const int RainCheckInterval = 60;

        public override void PostUpdateWorld()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            rainCheckTimer++;

            if (rainCheckTimer >= RainCheckInterval)
            {
                rainCheckTimer = 0;

                if (!Main.raining && !Main.bloodMoon && !Main.eclipse)
                {
                    if (Main.rand.NextBool(1200)) //this is just the chance each second that rain starts, so basically 1/20 every minute, so 1 rain every 20 mins
                    {
                        StartRain();
                    }
                }
            }
        }

        private void StartRain()
        {
            Main.StartRain();

            // Set a reasonable rain duration
            Main.rainTime = Main.rand.Next(18000, 54000); // 5-15 minutes
            Main.maxRaining = Main.rand.NextFloat(0.3f, 0.9f);

            // Sync to clients in multiplayer
            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.WorldData);
            }
        }
    }
    public class MechBossSpawnSystem : ModSystem
    {
        private bool hasAnnouncedTonight = false;
        private bool hasSpawnedTonight = false;
        private bool wasDay = true;
        private int selectedBoss = -1;

        public override void PostUpdateWorld()
        {
            bool isNight = !Main.dayTime;

            if (isNight && wasDay)
            {
                hasAnnouncedTonight = false;
                hasSpawnedTonight = false;
                selectedBoss = -1;
                wasDay = false;
            }

            if (!isNight && !wasDay)
            {
                wasDay = true;
                hasAnnouncedTonight = false;
                hasSpawnedTonight = false;
                selectedBoss = -1;
            }

            if (!ShouldSpawnMechBoss())
                return;

            if (Main.time >= 0 && Main.time < 100 && !hasAnnouncedTonight)
            {
                selectedBoss = ChooseMechanicalBoss();
                if (selectedBoss != -1)
                {
                    ShowAnnouncementMessage(selectedBoss);
                    hasAnnouncedTonight = true;
                }
            }

            if (Main.time >= 3600 && Main.time < 3700 && hasAnnouncedTonight && !hasSpawnedTonight)
            {
                if (selectedBoss != -1)
                {
                    Player targetPlayer = FindClosestPlayerToSpawn();
                    if (targetPlayer != null)
                    {
                        SpawnMechanicalBoss(selectedBoss, targetPlayer);
                        hasSpawnedTonight = true;
                    }
                }
            }
        }

        private bool ShouldSpawnMechBoss()
        {
            return !Main.dayTime &&
                   Main.hardMode &&
                   !NPC.AnyNPCs(NPCID.TheDestroyer) &&
                   !NPC.AnyNPCs(NPCID.Retinazer) &&
                   !NPC.AnyNPCs(NPCID.Spazmatism) &&
                   !NPC.AnyNPCs(NPCID.SkeletronPrime) &&
                   HasUndefeatedMechBoss() &&
                   FindClosestPlayerToSpawn() != null;
        }

        private bool HasUndefeatedMechBoss()
        {
            return !NPC.downedMechBoss1 || !NPC.downedMechBoss2 || !NPC.downedMechBoss3;
        }

        private Player FindClosestPlayerToSpawn()
        {
            Player closestPlayer = null;
            float closestDistance = float.MaxValue;

            Vector2 worldSpawn = new Vector2(Main.spawnTileX * 16f, Main.spawnTileY * 16f);

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player.active && !player.dead && player.ZoneOverworldHeight)
                {
                    float distance = Vector2.Distance(player.position, worldSpawn);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestPlayer = player;
                    }
                }
            }
            return closestPlayer;
        }

        private int ChooseMechanicalBoss()
        {
            var availableBosses = new List<int>();

            if (!NPC.downedMechBoss1) availableBosses.Add(NPCID.TheDestroyer);
            if (!NPC.downedMechBoss2) availableBosses.Add(NPCID.Retinazer);
            if (!NPC.downedMechBoss3) availableBosses.Add(NPCID.SkeletronPrime);

            return availableBosses.Count > 0 ? availableBosses[Main.rand.Next(availableBosses.Count)] : -1;
        }

        private void ShowAnnouncementMessage(int bossType)
        {
            string message;
            switch (bossType)
            {
                case NPCID.TheDestroyer:
                    message = "You feel vibrations from deep below...";
                    break;
                case NPCID.Retinazer:
                    message = "This is going to be a terrible night...";
                    break;
                case NPCID.SkeletronPrime:
                    message = "The air is getting colder around you...";
                    break;
                default:
                    return;
            }

            ShowMessage(message, new Color(50, 255, 130));
        }

        private void SpawnMechanicalBoss(int bossType, Player player)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || player == null)
                return;

            string spawnMessage;

            switch (bossType)
            {
                case NPCID.TheDestroyer:
                    spawnMessage = "Uh oh!";
                    NPC.SpawnOnPlayer(player.whoAmI, NPCID.TheDestroyer);
                    break;

                case NPCID.Retinazer:
                    spawnMessage = "Better watch out!";
                    // Spawn both twins
                    NPC.SpawnOnPlayer(player.whoAmI, NPCID.Retinazer);
                    NPC.SpawnOnPlayer(player.whoAmI, NPCID.Spazmatism);
                    break;

                case NPCID.SkeletronPrime:
                    spawnMessage = "Yikes!";
                    NPC.SpawnOnPlayer(player.whoAmI, NPCID.SkeletronPrime);
                    break;

                default:
                    return;
            }

            ShowMessage(spawnMessage, new Color(175, 75, 255));

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, player.position);
        }

        private void ShowMessage(string message, Color color)
        {
            if (Main.netMode == NetmodeID.Server)
            {
                ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(message), color);
            }
            else
            {
                Main.NewText(message, color.R, color.G, color.B);
            }
        }
        public override void PostSetupContent()
        {
            hasAnnouncedTonight = false;
            hasSpawnedTonight = false;
            wasDay = true;
            selectedBoss = -1;
        }
    }
    public class HardmodeGoblinSystem : ModSystem
    {
        private bool wasHardmode = false;

        public override void PostUpdateWorld()
        {
            // Check if world just entered hardmode
            if (Main.hardMode && !wasHardmode)
            {
                // Check if goblin army hasn't been defeated yet
                if (!NPC.downedGoblins)
                {
                    // Start goblin invasion
                    Main.StartInvasion(InvasionID.GoblinArmy);

                    // Send message to all players
                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.WorldData);
                    }
                }

                wasHardmode = true;
            }

            // Update state
            if (!Main.hardMode)
            {
                wasHardmode = false;
            }
        }

        public override void SaveWorldData(TagCompound tag)
        {
            // Save whether we were in hardmode
            tag["wasHardmode"] = wasHardmode;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            // Load saved state
            wasHardmode = tag.GetBool("wasHardmode");

            // If loading into hardmode, set flag appropriately
            if (Main.hardMode)
            {
                wasHardmode = true;
            }
        }
    }

    public class AltarOreIncrease : ModSystem
    {
        private static ILHook altarHook;

        public override void PostSetupContent()
        {
            MethodInfo method = typeof(Terraria.WorldGen).GetMethod("SmashAltar",
                BindingFlags.Public | BindingFlags.Static);
            altarHook = new ILHook(method, AltarOreILEdit);
        }

        public override void Unload()
        {
            altarHook?.Dispose();
        }

        private static void AltarOreILEdit(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            try
            {
                if (cursor.TryGotoNext(MoveType.After,
                    i => i.MatchLdloc(2),      // Load num3
                    i => i.MatchLdloc(1),      // Load num2
                    i => i.MatchConvR8(),      // Convert num2 to double
                    i => i.MatchDiv(),         // Divide
                    i => i.MatchStloc(2)       // Store back to num3
                ))
                {
                    ModContent.GetInstance<PvPAdventure>().Logger.Info("Found num3 /= num2 calculation");

                    double multiplier = 2.0; // THIS NUMBER MULTIPLIES IT

                    cursor.Emit(OpCodes.Ldloc_2);                 // Load num3
                    cursor.Emit(OpCodes.Ldc_R8, multiplier);      // Load multiplier
                    cursor.Emit(OpCodes.Mul);                     // Multiply
                    cursor.Emit(OpCodes.Stloc_2);                 // Store back to num3

                    ModContent.GetInstance<PvPAdventure>().Logger.Info($"Successfully patched SmashAltar to increase ore generation by {multiplier}x");
                    return;
                }

                ModContent.GetInstance<PvPAdventure>().Logger.Error("Failed to find num3 calculation in SmashAltar method");
            }
            catch (Exception e)
            {
                ModContent.GetInstance<PvPAdventure>().Logger.Error($"Error patching SmashAltar: {e}");
            }
        }
    }
    public class RemoveTownNPCInvasionSpawns : ModSystem
    {
        private static ILHook spawnNPCHook;

        public override void PostSetupContent()
        {
            MethodInfo method = typeof(Terraria.NPC).GetMethod("SpawnNPC",
                BindingFlags.Public | BindingFlags.Static);
            spawnNPCHook = new ILHook(method, RemoveTownNPCCheck);
        }

        public override void Unload()
        {
            spawnNPCHook?.Dispose();
        }

        private static void RemoveTownNPCCheck(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            try
            {
                // Find where townNPC field is loaded for invasion check
                if (cursor.TryGotoNext(MoveType.After,
                    i => i.MatchLdsfld(typeof(Terraria.Main).GetField("npc")),
                    i => i.MatchLdloc(out _),
                    i => i.MatchLdelemRef(),
                    i => i.MatchLdfld(typeof(Terraria.NPC).GetField("townNPC"))
                ))
                {
                    ModContent.GetInstance<PvPAdventure>().Logger.Info("Found townNPC field load in invasion code");
                    cursor.Emit(OpCodes.Pop);
                    cursor.Emit(OpCodes.Ldc_I4_0);

                    ModContent.GetInstance<PvPAdventure>().Logger.Info("Successfully made townNPC always false for invasions");
                }
                else
                {
                    ModContent.GetInstance<PvPAdventure>().Logger.Error("Could not find townNPC field in SpawnNPC");
                }
            }
            catch (Exception e)
            {
                ModContent.GetInstance<PvPAdventure>().Logger.Error($"Error in IL edit: {e}");
            }
        }
    }
    internal class RemoveBanners : GlobalItem
    {
        public override void OnSpawn(Item item, IEntitySource source)
        {
            if (item.createTile == TileID.Banners)
            {
                item.active = false;
                item.TurnToAir();
            }
        }

        public override void UpdateInventory(Item item, Player player)
        {
            if (item.createTile == TileID.Banners)
            {
                item.TurnToAir();
            }
        }
    }
    //public class ExtraWorldGen : ModSystem
    //{
    //    public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
    //    {
    //        // We need to run AFTER most worldgen, so let's find later passes to insert after
    //        int shiniesIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Shinies"));
    //        int finalCleanupIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Final Cleanup"));

    //        // If we can't find those, use a safe default (near the end)
    //        int insertIndex = finalCleanupIndex != -1 ? finalCleanupIndex : tasks.Count - 1;

    //        // Add all our extra generation as a single pass that runs late
    //        tasks.Insert(insertIndex, new PassLegacy("Extra Structures Late Pass", delegate (GenerationProgress progress, GameConfiguration passConfig)
    //        {
    //            progress.Message = "Adding extra structures...";

    //            // Add extra pyramids
    //            progress.Set(0.1);
    //            int extraPyramids = WorldGen.genRand.Next(1, 1);
    //            for (int i = 0; i < extraPyramids; i++)
    //            {
    //                int attempts = 0;
    //                bool placed = false;
    //                while (!placed && attempts < 1000)
    //                {
    //                    int x = WorldGen.genRand.Next(300, Main.maxTilesX - 300);
    //                    int y = (int)Main.worldSurface - 10;
    //                    if (WorldGen.InWorld(x, y) && Main.tile[x, y].TileType == TileID.Sand)
    //                    {
    //                        placed = WorldGen.Pyramid(x, y);
    //                    }
    //                    attempts++;
    //                }
    //            }

    //            // Add extra pyramids
    //            progress.Set(0.2);
    //            int extraPatches = WorldGen.genRand.Next(3, 7);

    //            for (int i = 0; i < extraPatches; i++)
    //            {
    //                progress.Set((double)i / extraPatches);
    //                int attempts = 0;
    //                bool placed = false;

    //                while (!placed && attempts < 1000)
    //                {
    //                    int x = WorldGen.genRand.Next((int)(Main.maxTilesX * 0.2), (int)(Main.maxTilesX * 0.8));
    //                    int y = WorldGen.genRand.Next((int)Main.rockLayer + 50, Main.maxTilesY - 300);

    //                    if (WorldGen.InWorld(x, y))
    //                    {
    //                        WorldGen.ShroomPatch(x, y);
    //                        placed = true;
    //                    }
    //                    attempts++;
    //                }
    //            }

    //            // Add extra rich mahogany trees using the proper biome system
    //            progress.Set(0.3);
    //            try
    //            {
    //                var mahoganyTreeBiome = GenVars.configuration.CreateBiome<MahoganyTreeBiome>();
    //                int extraMahoganyTrees = WorldGen.genRand.Next(4, 6);
    //                int mahoganyPlaced = 0;
    //                int mahoganyAttempts = 0;

    //                while (mahoganyPlaced < extraMahoganyTrees && mahoganyAttempts < 20000)
    //                {
    //                    Point point = WorldGen.RandomWorldPoint((int)Main.worldSurface + 50, 50, 500, 50);
    //                    if (mahoganyTreeBiome.Place(point, GenVars.structures))
    //                    {
    //                        mahoganyPlaced++;
    //                    }
    //                    mahoganyAttempts++;
    //                }
    //            }
    //            catch
    //            {
    //                // Fallback if MahoganyTreeBiome isn't available
    //                ModContent.GetInstance<PvPAdventure>().Logger.Info("MahoganyTreeBiome not available, skipping extra mahogany trees");
    //            }

    //            // Add extra minecart tracks using TrackGenerator
    //            progress.Set(0.5);
    //            try
    //            {
    //                var trackGenerator = new TrackGenerator();

    //                // Add long tracks
    //                int extraLongTracks = WorldGen.genRand.Next(15, 20);
    //                int longTrackPlaced = 0;
    //                int longTrackAttempts = 0;

    //                while (longTrackPlaced < extraLongTracks && longTrackAttempts < Main.maxTilesX)
    //                {
    //                    Point point = WorldGen.RandomWorldPoint((int)Main.worldSurface, 10, 200, 10);
    //                    if (trackGenerator.Place(point, 300, 2000)) // Long track length range
    //                    {
    //                        longTrackPlaced++;
    //                        longTrackAttempts = 0;
    //                    }
    //                    else
    //                    {
    //                        longTrackAttempts++;
    //                    }
    //                }

    //                // Add standard tracks
    //                int extraStandardTracks = WorldGen.genRand.Next(15, 22);
    //                int standardTrackPlaced = 0;
    //                int standardTrackAttempts = 0;

    //                while (standardTrackPlaced < extraStandardTracks && standardTrackAttempts < Main.maxTilesX)
    //                {
    //                    Point point = WorldGen.RandomWorldPoint((int)Main.worldSurface, 10, 200, 10);
    //                    if (trackGenerator.Place(point, 100, 300)) // Standard track length range
    //                    {
    //                        standardTrackPlaced++;
    //                        standardTrackAttempts = 0;
    //                    }
    //                    else
    //                    {
    //                        standardTrackAttempts++;
    //                    }
    //                }
    //            }
    //            catch
    //            {
    //                // Fallback if TrackGenerator isn't available
    //                ModContent.GetInstance<PvPAdventure>().Logger.Info("TrackGenerator not available, using alternative minecart track generation");

    //                // Alternative minecart track generation
    //                int extraTracks = WorldGen.genRand.Next(10, 18);
    //                for (int i = 0; i < extraTracks; i++)
    //                {
    //                    int attempts = 0;
    //                    bool placed = false;
    //                    while (!placed && attempts < 500)
    //                    {
    //                        int x = WorldGen.genRand.Next(200, Main.maxTilesX - 200);
    //                        int y = WorldGen.genRand.Next((int)Main.worldSurface + 50, (int)Main.rockLayer + 100);

    //                        if (WorldGen.InWorld(x, y))
    //                        {
    //                            // Create underground tunnel with minecart tracks
    //                            WorldGen.TileRunner(x, y, (double)WorldGen.genRand.Next(8, 15), WorldGen.genRand.Next(50, 100),
    //                                -1, false, 0f, 0f, true, true);

    //                            // Place minecart tracks in the tunnel
    //                            for (int trackX = x - 5; trackX <= x + 5; trackX++)
    //                            {
    //                                if (WorldGen.InWorld(trackX, y) && !Main.tile[trackX, y].HasTile)
    //                                {
    //                                    WorldGen.PlaceTile(trackX, y, TileID.MinecartTrack, true, false);
    //                                }
    //                            }

    //                            placed = true;
    //                        }
    //                        attempts++;
    //                    }
    //                }
    //            }

    //            // Add extra hives using HiveBiome
    //            progress.Set(0.6);
    //            try
    //            {
    //                var hiveBiome = GenVars.configuration.CreateBiome<HiveBiome>();
    //                var honeyPatchBiome = GenVars.configuration.CreateBiome<HoneyPatchBiome>();
    //                double hiveNum = (double)Main.maxTilesX / 4200.0;
    //                int extraHives = WorldGen.genRand.Next((int)(7.0 * hiveNum), (int)(11.0 * hiveNum));

    //                for (int i = 0; i < extraHives; i++)
    //                {
    //                    int attempts = 0;
    //                    bool placed = false;
    //                    while (!placed && attempts < 1000)
    //                    {
    //                        Point point = WorldGen.RandomWorldPoint((int)(Main.worldSurface + Main.rockLayer) >> 1, 20, 300, 20);
    //                        if (hiveBiome.Place(point, GenVars.structures))
    //                        {
    //                            placed = true;
    //                            int honeyPatches = WorldGen.genRand.Next(3);
    //                            int honeyPlaced = 0;
    //                            int honeyAttempts = 0;

    //                            while (honeyPlaced < honeyPatches && honeyAttempts < 100)
    //                            {
    //                                double distance = WorldGen.genRand.NextDouble() * 60.0 + 30.0;
    //                                double angle = WorldGen.genRand.NextDouble() * 6.2831854820251465;
    //                                int honeyX = (int)(Math.Cos(angle) * distance) + point.X;
    //                                int honeyY = (int)(Math.Sin(angle) * distance) + point.Y;
    //                                honeyAttempts++;

    //                                if (honeyPatchBiome.Place(new Point(honeyX, honeyY), GenVars.structures))
    //                                {
    //                                    honeyPlaced++;
    //                                }
    //                            }
    //                        }
    //                        attempts++;
    //                    }
    //                }
    //            }
    //            catch
    //            {
    //                // Fallback if HiveBiome isn't available
    //                ModContent.GetInstance<PvPAdventure>().Logger.Info("HiveBiome not available, using alternative hive generation");

    //                // Alternative hive generation
    //                int extraHives = WorldGen.genRand.Next(4, 8);
    //                for (int i = 0; i < extraHives; i++)
    //                {
    //                    int attempts = 0;
    //                    bool placed = false;
    //                    while (!placed && attempts < 500)
    //                    {
    //                        int x = WorldGen.genRand.Next(200, Main.maxTilesX - 200);
    //                        int y = WorldGen.genRand.Next((int)((Main.worldSurface + Main.rockLayer) / 2), (int)Main.rockLayer + 200);

    //                        // Check if we're in jungle (mud blocks)
    //                        if (Main.tile[x, y].TileType == TileID.Mud || Main.tile[x, y].TileType == TileID.JungleGrass)
    //                        {
    //                            // Create a hive using TileRunner with Hive block (225)
    //                            int hiveSize = WorldGen.genRand.Next(30, 60);
    //                            WorldGen.TileRunner(x, y, (double)hiveSize, 1, TileID.Hive, false, 0f, 0f, true, true);

    //                            // Carve out the interior
    //                            WorldGen.TileRunner(x, y, (double)(hiveSize - 10), 1, -1, false, 0f, 0f, true, true);

    //                            // Add some honey
    //                            if (WorldGen.genRand.NextBool(3))
    //                            {
    //                                WorldGen.TileRunner(x, y, (double)WorldGen.genRand.Next(5, 15), 1, TileID.HoneyBlock, false, 0f, 0f, true, true);
    //                            }

    //                            placed = true;
    //                        }
    //                        attempts++;
    //                    }
    //                }
    //            }

    //            // Add extra sky islands with houses
    //            progress.Set(0.8);
    //            int extraIslands = WorldGen.genRand.Next(2, 4);
    //            for (int i = 0; i < extraIslands; i++)
    //            {
    //                int attempts = 0;
    //                bool placed = false;
    //                while (!placed && attempts < 1000)
    //                {
    //                    int x = WorldGen.genRand.Next((int)(Main.maxTilesX * 0.1), (int)(Main.maxTilesX * 0.9));
    //                    int y = WorldGen.genRand.Next(90, (int)Main.worldSurface - 150);

    //                    if (x > Main.maxTilesX / 2 - 150 && x < Main.maxTilesX / 2 + 150)
    //                    {
    //                        attempts++;
    //                        continue;
    //                    }

    //                    if (WorldGen.InWorld(x, y))
    //                    {
    //                        WorldGen.CloudIsland(x, y);

    //                        int islandStyle = 0;
    //                        if (WorldGen.remixWorldGen && WorldGen.drunkWorldGen)
    //                        {
    //                            bool flag = (GenVars.crimsonLeft && x < Main.maxTilesX / 2) || (!GenVars.crimsonLeft && x > Main.maxTilesX / 2);
    //                            islandStyle = flag ? 5 : 4;
    //                        }
    //                        else if (WorldGen.getGoodWorldGen || WorldGen.remixWorldGen)
    //                        {
    //                            islandStyle = WorldGen.crimson ? 5 : 4;
    //                        }
    //                        else if (Main.tenthAnniversaryWorld)
    //                        {
    //                            islandStyle = 6;
    //                        }

    //                        WorldGen.IslandHouse(x, y, islandStyle);
    //                        placed = true;
    //                    }
    //                    attempts++;
    //                }
    //            }

    //            // Add extra mine houses with buried chests
    //            progress.Set(0.9);
    //            int extraHouses = WorldGen.genRand.Next(40, 80);
    //            for (int i = 0; i < extraHouses; i++)
    //            {
    //                int attempts = 0;
    //                bool placed = false;
    //                while (!placed && attempts < 1000)
    //                {
    //                    int x = WorldGen.genRand.Next(100, Main.maxTilesX - 100);
    //                    int y = WorldGen.genRand.Next((int)Main.worldSurface + 100, (int)Main.rockLayer + 200);

    //                    if (WorldGen.InWorld(x, y) && !Main.tile[x, y].HasTile && Main.tile[x, y].WallType == 0)
    //                    {
    //                        // Generate the mine house
    //                        WorldGen.MineHouse(x, y);

    //                        // Now try to place a buried chest inside the mine house
    //                        bool chestPlaced = false;
    //                        int chestAttempts = 0;

    //                        // Search for a suitable spot inside the mine house area for the chest
    //                        while (!chestPlaced && chestAttempts < 100)
    //                        {
    //                            // Look within a 15 tile radius of the original placement point
    //                            int chestX = x + WorldGen.genRand.Next(-10, 11);
    //                            int chestY = y + WorldGen.genRand.Next(-5, 6);

    //                            if (WorldGen.InWorld(chestX, chestY))
    //                            {
    //                                // Check if this position is inside the mine house structure
    //                                // Mine houses typically have wooden walls (wall ID 27) and wood/stone tiles
    //                                if (Main.tile[chestX, chestY].WallType == 27 ||
    //                                    (Main.tile[chestX, chestY].HasTile &&
    //                                     (Main.tile[chestX, chestY].TileType == TileID.WoodBlock ||
    //                                      Main.tile[chestX, chestY].TileType == TileID.Stone)))
    //                                {
    //                                    // Try to place a buried chest with random style
    //                                    int chestStyle = WorldGen.genRand.Next(1, 11); // Regular underground chest styles
    //                                    if (WorldGen.AddBuriedChest(chestX, chestY, 0, false, 1))
    //                                    {
    //                                        chestPlaced = true;
    //                                        ModContent.GetInstance<PvPAdventure>().Logger.Info($"Placed buried chest at {chestX}, {chestY} in mine house");
    //                                    }
    //                                }
    //                            }
    //                            chestAttempts++;
    //                        }

    //                        placed = true;
    //                    }
    //                    attempts++;
    //                }
    //            }

    //            // Add extra life crystals and underground chests
    //            progress.Set(0.95);
    //            int extraLifeCrystals = Main.maxTilesX / 15;
    //            for (int i = 0; i < extraLifeCrystals; i++)
    //            {
    //                int attempts = 0;
    //                bool placed = false;
    //                while (!placed && attempts < 1000)
    //                {
    //                    int x = WorldGen.genRand.Next(Main.offLimitBorderTiles, Main.maxTilesX - Main.offLimitBorderTiles);
    //                    int y = WorldGen.genRand.Next((int)(Main.worldSurface * 2.0 + Main.rockLayer) / 3, Main.maxTilesY - 300);
    //                    if (WorldGen.AddLifeCrystal(x, y))
    //                    {
    //                        placed = true;
    //                    }
    //                    attempts++;
    //                }
    //            }

    //            int extraChests = Main.maxTilesX / 30;
    //            for (int i = 0; i < extraChests; i++)
    //            {
    //                int attempts = 0;
    //                bool placed = false;
    //                while (!placed && attempts < 1000)
    //                {
    //                    int x = WorldGen.genRand.Next(Main.offLimitBorderTiles, Main.maxTilesX - Main.offLimitBorderTiles);
    //                    int y = WorldGen.genRand.Next((int)Main.worldSurface + 50, Main.maxTilesY - 350);
    //                    if (WorldGen.AddBuriedChest(x, y, 0, false, 1))
    //                    {
    //                        placed = true;
    //                    }
    //                    attempts++;
    //                }
    //            }

    //            progress.Set(1.0);

    //        }));

    //        ModContent.GetInstance<PvPAdventure>().Logger.Info("Added extra worldgen pass for multiple structures");
    //    }
    //}
}