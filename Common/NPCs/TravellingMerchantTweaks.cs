using System.Collections.Generic;
using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PvPAdventure.Common.NPCs;

/// <summary>
/// Forces the Travelling Merchant to spawn every day at day start.
/// Removes vanilla restrictions (invasions, etc.) on his spawning.
/// In hardmode, guarantees one weapon is always in his shop.
/// Removes Pho and Pad Thai from his shop.
/// </summary>
public class TravellingMerchantTweaks : GlobalNPC
{
    public override void Load()
    {
        On_Main.UpdateTime_StartDay += OnUpdateTimeStartDay;
    }

    private void OnUpdateTimeStartDay(On_Main.orig_UpdateTime_StartDay orig, ref bool stopEvents)
    {
        orig(ref stopEvents);

        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        if (NPC.AnyNPCs(NPCID.TravellingMerchant))
            return;

        Chest.SetupTravelShop();

        // Remove Pho and Pad Thai by zeroing their slots.
        for (int i = 0; i < Main.travelShop.Length; i++)
        {
            if (Main.travelShop[i] == ItemID.Pho || Main.travelShop[i] == ItemID.PadThai)
                Main.travelShop[i] = ItemID.None;
        }

        // Always pick one item from the standard pool and add it to the shop.
        {
            var standardPool = new List<int>
            {
                ItemID.ZapinatorGray,
                ItemID.Code1,
                ItemID.Katana,
                ItemID.Revolver,
                ItemID.SittingDucksFishingRod,
                ItemID.SittingDucksFishingRod, // weighted 2x
            };

            int standardWeapon = standardPool[Main.rand.Next(standardPool.Count)];

            for (int i = 0; i < Main.travelShop.Length; i++)
            {
                if (Main.travelShop[i] == ItemID.None)
                {
                    Main.travelShop[i] = standardWeapon;
                    break;
                }
            }
        }

        // In hardmode, pick a weapon from the hardmode pool and place it in the first empty slot.
        if (Main.hardMode)
        {
            var pool = new List<int>
            {
                ItemID.ZapinatorOrange,
                ItemID.BouncingShield,
                ItemID.Gatligator,
            };

            // Pulse Bow enters the pool twice after all three mechanical bosses are defeated,doubling its weight
            if (NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3)
            {
                pool.Add(ItemID.PulseBow);
                pool.Add(ItemID.PulseBow);
            }

            int weapon = pool[Main.rand.Next(pool.Count)];

            for (int i = 0; i < Main.travelShop.Length; i++)
            {
                if (Main.travelShop[i] == ItemID.None)
                {
                    Main.travelShop[i] = weapon;
                    break;
                }
            }
        }

        if (Main.netMode == NetmodeID.Server)
            NetMessage.SendTravelShop(-1);

        int[] npcIndices = new int[Main.maxNPCs];
        Microsoft.Xna.Framework.Point[] homeTiles = new Microsoft.Xna.Framework.Point[Main.maxNPCs];
        int count = 0;

        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC npc = Main.npc[i];
            if (npc.active && npc.townNPC && npc.type != NPCID.Guide && !npc.homeless)
            {
                npcIndices[count] = i;
                homeTiles[count] = new Microsoft.Xna.Framework.Point(npc.homeTileX, npc.homeTileY);
                count++;
            }
        }

        if (count == 0)
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && npc.townNPC && npc.type != NPCID.Guide && npc.homeless)
                {
                    if (WorldGen.TownManager.HasRoom(npc.type, out Microsoft.Xna.Framework.Point roomPoint))
                    {
                        npcIndices[count] = i;
                        homeTiles[count] = roomPoint;
                        count++;
                    }
                }
            }
        }

        if (count == 0)
        {
            Mod.Logger.Info("TravellingMerchant: no valid town NPCs to spawn near, skipping.");
            return;
        }

        int chosen = Main.rand.Next(count);
        WorldGen.bestX = homeTiles[chosen].X;
        WorldGen.bestY = homeTiles[chosen].Y;

        int spawnTileX = WorldGen.bestX;
        int spawnTileY = WorldGen.bestY;
        bool foundSpot = false;

        if ((double)spawnTileY <= Main.worldSurface)
        {
            for (int dist = 20; dist < 500 && !foundSpot; dist++)
            {
                for (int side = 0; side < 2 && !foundSpot; side++)
                {
                    int tryX = side == 0 ? WorldGen.bestX + dist * 2 : WorldGen.bestX - dist * 2;
                    if (tryX <= 10 || tryX >= Main.maxTilesX - 10)
                        continue;

                    int minY = System.Math.Max(10, WorldGen.bestY - dist);
                    int maxY = (int)System.Math.Min(Main.worldSurface, WorldGen.bestY + dist);

                    for (int tryY = minY; tryY < maxY; tryY++)
                    {
                        if (!Main.tile[tryX, tryY].HasTile || !Main.tileSolid[Main.tile[tryX, tryY].TileType])
                            continue;

                        if (Main.tile[tryX, tryY - 1].LiquidAmount > 0 ||
                            Main.tile[tryX, tryY - 2].LiquidAmount > 0 ||
                            Main.tile[tryX, tryY - 3].LiquidAmount > 0 ||
                            Collision.SolidTiles(tryX - 1, tryX + 1, tryY - 3, tryY - 1))
                            break;

                        var safeZone = new Microsoft.Xna.Framework.Rectangle(
                            tryX * 16 + 8 - NPC.sWidth / 2 - NPC.safeRangeX,
                            tryY * 16 + 8 - NPC.sHeight / 2 - NPC.safeRangeY,
                            NPC.sWidth + NPC.safeRangeX * 2,
                            NPC.sHeight + NPC.safeRangeY * 2);

                        bool playerTooClose = false;
                        for (int p = 0; p < Main.maxPlayers; p++)
                        {
                            if (!Main.player[p].active) continue;
                            var playerRect = new Microsoft.Xna.Framework.Rectangle(
                                (int)Main.player[p].position.X, (int)Main.player[p].position.Y,
                                Main.player[p].width, Main.player[p].height);
                            if (playerRect.Intersects(safeZone))
                            {
                                playerTooClose = true;
                                break;
                            }
                        }

                        if (!playerTooClose)
                        {
                            spawnTileX = tryX;
                            spawnTileY = tryY;
                            foundSpot = true;
                        }
                        break;
                    }
                }
            }
        }

        if (!foundSpot)
        {
            Mod.Logger.Info("TravellingMerchant: could not find a valid spawn tile, skipping.");
            return;
        }

        int index = NPC.NewNPC(
            NPC.GetSpawnSourceForTownSpawn(),
            spawnTileX * 16,
            spawnTileY * 16,
            NPCID.TravellingMerchant,
            1
        );

        if (index >= Main.maxNPCs)
        {
            Mod.Logger.Warn("TravellingMerchant: NPC.NewNPC failed to return a valid index.");
            return;
        }

        Main.npc[index].homeTileX = WorldGen.bestX;
        Main.npc[index].homeTileY = WorldGen.bestY;
        Main.npc[index].homeless = true;
        Main.npc[index].direction = spawnTileX < WorldGen.bestX ? 1 : -1;
        Main.npc[index].netUpdate = true;

        if (Main.netMode == NetmodeID.Server)
            NetMessage.SendData(MessageID.SyncNPC, number: index);

        ChatHelper.BroadcastChatMessage(
            NetworkText.FromKey("Announcement.HasArrived", Main.npc[index].GetFullNetName()),
            new Microsoft.Xna.Framework.Color(50, 125, 255));
    }
}