using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using System.Collections.Generic;
using Terraria.Chat;
using Terraria.Localization;
using Microsoft.Xna.Framework;
using Terraria.ModLoader.IO;
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


}