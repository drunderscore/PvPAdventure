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
namespace PvPAdventure
{
    public class RainSystem : ModSystem
    {
        private bool triggeredToday;

        public override void PreUpdateWorld()
        {
            if (Main.netMode == NetmodeID.Server) return;

            if (Main.dayTime && Main.time == 0)
            {
                triggeredToday = false;
            }

            if (Main.dayTime &&
                !triggeredToday &&
                Main.time >= 17524)
            {

                if (Main.rand.NextBool(3))
                {
                    StartRain();
                }
                triggeredToday = true;
            }
        }

        private void StartRain()
        {
            Main.rainTime = Main.rand.Next(3600, 18000);
            Main.raining = true;

            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.WorldData);
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
    //    public class ExtraWorldGen : ModSystem
    //    {
    //        public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
    //        {
    //            // Find the original passes to insert after them
    //            int lifeCrystalIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Life Crystals"));
    //            int minecartIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Minecart Tracks"));
    //            int undergroundChestIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Buried Chests"));

    //            // Add extra life crystals (2x more)
    //            if (lifeCrystalIndex != -1)
    //            {
    //                tasks.Insert(lifeCrystalIndex + 1, new PassLegacy("Extra Life Crystals", delegate (GenerationProgress progress, GameConfiguration passConfig)
    //                {
    //                    progress.Message = "Adding extra life crystals...";

    //                    double num = (double)(Main.maxTilesX * Main.maxTilesY) * 2E-05; // Same formula as vanilla but e5 for now

    //                    for (int i = 0; i < (int)num; i++)
    //                    {
    //                        progress.Set((double)i / num);

    //                        bool placed = false;
    //                        int attempts = 0;

    //                        while (!placed && attempts < 10000)
    //                        {
    //                            int x = WorldGen.genRand.Next(Main.offLimitBorderTiles, Main.maxTilesX - Main.offLimitBorderTiles);
    //                            int y = WorldGen.genRand.Next((int)(Main.worldSurface * 2.0 + Main.rockLayer) / 3, Main.maxTilesY - 300);

    //                            if (WorldGen.getGoodWorldGen) //for the worthy
    //                            {
    //                                y = WorldGen.genRand.Next((int)Main.worldSurface, Main.maxTilesY - 400);
    //                            }

    //                            if (WorldGen.AddLifeCrystal(x, y))
    //                            {
    //                                placed = true;
    //                            }
    //                            attempts++;
    //                        }
    //                    }
    //                }));
    //            }
    //            // Add extra underground chests (2x more)
    //            if (undergroundChestIndex != -1)
    //            {
    //                tasks.Insert(undergroundChestIndex + 1, new PassLegacy("Extra Underground Chests", delegate (GenerationProgress progress, GameConfiguration passConfig)
    //                {
    //                    progress.Message = "Adding extra underground chests...";

    //                    // Vanilla formula is roughly maxTilesX / 34
    //                    int extraChests = Main.maxTilesX / 34;

    //                    for (int i = 0; i < extraChests; i++)
    //                    {
    //                        progress.Set((double)i / extraChests);

    //                        bool placed = false;
    //                        int attempts = 0;

    //                        while (!placed && attempts < 1000)
    //                        {
    //                            int x = WorldGen.genRand.Next(Main.offLimitBorderTiles, Main.maxTilesX - Main.offLimitBorderTiles);
    //                            int y = WorldGen.genRand.Next((int)Main.worldSurface + 50, Main.maxTilesY - 350);

    //                            if (WorldGen.AddBuriedChest(x, y, 0, false, 1))
    //                            {
    //                                placed = true;
    //                            }
    //                            attempts++;
    //                        }
    //                    }
    //                }));
    //            }

    //            ModContent.GetInstance<PvPAdventure>().Logger.Info("Added extra worldgen passes for life crystals, and underground chests");
    //        }
    //    }
}