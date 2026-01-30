using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Generation;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace PvPAdventure.Common.WorldGenChanges.EJ;

public class JungleShrineModifier : ModSystem
{
    public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
    {
        // Find the Jungle Chests pass
        int jungleChestsIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Jungle Chests"));
        if (jungleChestsIndex == -1) return;

        // Replace it with our modified version
        tasks[jungleChestsIndex] = new PassLegacy("Jungle Chests", delegate (GenerationProgress progress, GameConfiguration passConfig)
        {
            // This is the EXACT vanilla code with ONE LINE changed
            int num = WorldGen.genRand.Next(40, Main.maxTilesX - 40);
            int num2 = WorldGen.genRand.Next((int)(Main.worldSurface + Main.rockLayer) / 2, Main.maxTilesY - 400);

            // ONLY CHANGE THIS LINE: Increased from (7, 12) to (70, 120)
            double num3 = (double)WorldGen.genRand.Next(10, 20);
            num3 *= (double)Main.maxTilesX / 4200.0;

            int num4 = 0;
            int num5 = 0;
            while ((double)num5 < num3)
            {
                bool flag = true;
                while (flag)
                {
                    num4++;
                    num = WorldGen.genRand.Next(40, Main.maxTilesX / 2 - 40);
                    if (GenVars.dungeonSide < 0)
                    {
                        num += Main.maxTilesX / 2;
                    }
                    num2 = WorldGen.genRand.Next((int)(Main.worldSurface + Main.rockLayer) / 2, Main.maxTilesY - 400);
                    int i = WorldGen.genRand.Next(2, 4);
                    int num6 = WorldGen.genRand.Next(2, 4);
                    Rectangle area = new Rectangle(num - i - 1, num2 - num6 - 1, i + 1, num6 + 1);

                    // Use Framing.GetTileSafely for reading
                    Tile tile = Framing.GetTileSafely(num, num2);
                    if (tile.HasTile && tile.TileType == 60)
                    {
                        int num7 = 30;
                        flag = false;
                        for (int j = num - num7; j < num + num7; j += 3)
                        {
                            for (int k = num2 - num7; k < num2 + num7; k += 3)
                            {
                                Tile checkTile = Framing.GetTileSafely(j, k);
                                if (checkTile.HasTile && (checkTile.TileType == 225 || checkTile.TileType == 229 || checkTile.TileType == 226 || checkTile.TileType == 119 || checkTile.TileType == 120))
                                {
                                    flag = true;
                                }
                                if (checkTile.WallType == 86 || checkTile.WallType == 87)
                                {
                                    flag = true;
                                }
                            }
                        }
                        if (!GenVars.structures.CanPlace(area, 1))
                        {
                            flag = true;
                        }
                    }
                    if (!flag)
                    {
                        ushort wall = 0;
                        if (GenVars.jungleHut == 119)
                        {
                            wall = 23;
                        }
                        else if (GenVars.jungleHut == 120)
                        {
                            wall = 24;
                        }
                        else if (GenVars.jungleHut == 158)
                        {
                            wall = 42;
                        }
                        else if (GenVars.jungleHut == 175)
                        {
                            wall = 45;
                        }
                        else if (GenVars.jungleHut == 45)
                        {
                            wall = 10;
                        }

                        // FIRST: Clear the entire area and remove liquids
                        for (int l = num - i - 1; l <= num + i + 1; l++)
                        {
                            for (int m = num2 - num6 - 1; m <= num2 + num6 + 1; m++)
                            {
                                WorldGen.KillTile(l, m, noItem: true); // Clear everything first
                                WorldGen.KillWall(l, m); // Clear walls
                                                         // Remove liquids
                                WorldGen.PlaceLiquid(l, m, (byte)LiquidID.Water, 0);
                            }
                        }

                        // SECOND: Build shrine outer walls
                        for (int l = num - i - 1; l <= num + i + 1; l++)
                        {
                            for (int m = num2 - num6 - 1; m <= num2 + num6 + 1; m++)
                            {
                                WorldGen.PlaceTile(l, m, GenVars.jungleHut, true, true);
                            }
                        }

                        // THIRD: Hollow out interior and set interior walls
                        for (int n = num - i; n <= num + i; n++)
                        {
                            for (int num8 = num2 - num6; num8 <= num2 + num6; num8++)
                            {
                                WorldGen.KillTile(n, num8, noItem: true);
                                WorldGen.PlaceWall(n, num8, wall, true);
                            }
                        }

                        bool flag2 = false;
                        int num9 = 0;
                        while (!flag2 && num9 < 100)
                        {
                            num9++;
                            int num10 = WorldGen.genRand.Next(num - i, num + i + 1);
                            int num11 = WorldGen.genRand.Next(num2 - num6, num2 + num6 - 2);
                            WorldGen.PlaceTile(num10, num11, 4, true, false, -1, 3);
                            if (Framing.GetTileSafely(num10, num11).TileType == 4)
                            {
                                flag2 = true;
                            }
                        }

                        // FOURTH: Create entrance - clear the bottom but KEEP the interior walls
                        for (int num12 = num - i - 1; num12 <= num + i + 1; num12++)
                        {
                            for (int num13 = num2 + num6 - 2; num13 <= num2 + num6; num13++)
                            {
                                WorldGen.KillTile(num12, num13, noItem: true);
                                // Don't kill the walls here - we want to keep the interior walls
                            }
                        }

                        // Make sure the entrance area has proper walls
                        for (int num12 = num - i; num12 <= num + i; num12++)
                        {
                            for (int num13 = num2 + num6 - 2; num13 <= num2 + num6 - 1; num13++)
                            {
                                WorldGen.PlaceWall(num12, num13, wall, true);
                            }
                        }

                        // FIFTH: Add supporting pillars
                        for (int num16 = num - i - 1; num16 <= num + i + 1; num16++)
                        {
                            int num17 = 4;
                            int num18 = num2 + num6 + 2;
                            while (num18 < Main.maxTilesY && num17 > 0)
                            {
                                // Clear then place support
                                WorldGen.KillTile(num16, num18, noItem: true);
                                WorldGen.PlaceTile(num16, num18, 59, true, true);
                                num18++;
                                num17--;
                            }
                        }

                        // SIXTH: Create pyramid roof
                        int currentWidth = i - WorldGen.genRand.Next(1, 3);
                        int roofY = num2 - num6 - 2;
                        while (currentWidth > -1)
                        {
                            for (int num20 = num - currentWidth - 1; num20 <= num + currentWidth + 1; num20++)
                            {
                                WorldGen.KillTile(num20, roofY, noItem: true); // Clear first
                                WorldGen.PlaceTile(num20, roofY, GenVars.jungleHut, true, true);
                            }
                            currentWidth -= WorldGen.genRand.Next(1, 3);
                            roofY--;
                        }

                        GenVars.JChestX[GenVars.numJChests] = num;
                        GenVars.JChestY[GenVars.numJChests] = num2;
                        GenVars.structures.AddProtectedStructure(area, 0);
                        GenVars.numJChests++;
                        num4 = 0;
                    }
                    else if (num4 > Main.maxTilesX * 10)
                    {
                        num5++;
                        num4 = 0;
                        break;
                    }
                }
                num5++;
            }
            Main.tileSolid[137] = false;
        });
    }
}