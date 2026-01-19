using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Items;

public class ConsumableShadowKeyPlayer : ModPlayer
{
    private Dictionary<Point, int> trackedLockedChests = new Dictionary<Point, int>();

    public override void PostUpdate()
    {
        // Only track and consume on the client that owns this player
        if (Player.whoAmI != Main.myPlayer)
            return;

        // Scan every frame for consistency
        int scanRange = 10;
        int playerTileX = (int)(Player.Center.X / 16);
        int playerTileY = (int)(Player.Center.Y / 16);

        HashSet<Point> foundLockedChests = new HashSet<Point>();

        for (int i = playerTileX - scanRange; i < playerTileX + scanRange; i++)
        {
            for (int j = playerTileY - scanRange; j < playerTileY + scanRange; j++)
            {
                if (!WorldGen.InWorld(i, j))
                    continue;

                Tile tile = Main.tile[i, j];

                if (tile != null && tile.TileType == TileID.Containers)
                {
                    int left = i - (tile.TileFrameX % 36) / 18;
                    int top = j - (tile.TileFrameY % 36) / 18;
                    Point chestPos = new Point(left, top);

                    if (foundLockedChests.Contains(chestPos))
                        continue;

                    Tile topLeftTile = Main.tile[left, top];
                    int frameX = topLeftTile.TileFrameX;

                    if (frameX == 144)
                    {
                        foundLockedChests.Add(chestPos);

                        if (!trackedLockedChests.ContainsKey(chestPos))
                        {
                            trackedLockedChests.Add(chestPos, (int)Main.GameUpdateCount);
                        }
                    }
                }
            }
        }

        // Check if any tracked chests are now unlocked
        List<Point> chestsToRemove = new List<Point>();
        foreach (var kvp in trackedLockedChests)
        {
            Point chestPos = kvp.Key;

            if (!foundLockedChests.Contains(chestPos))
            {
                if (WorldGen.InWorld(chestPos.X, chestPos.Y))
                {
                    float distance = Vector2.Distance(new Vector2(chestPos.X * 16, chestPos.Y * 16), Player.Center);

                    // Only consume key if chest is within interaction range
                    // Terraria's tile interaction range is about 6.5 tiles (104 pixels)
                    if (distance <= 104)
                    {
                        // Consume Shadow Key - only players within interaction range could have unlocked it
                        if (Player.HasItem(ItemID.ShadowKey))
                        {
                            Player.ConsumeItem(ItemID.ShadowKey);
                        }
                    }
                }

                chestsToRemove.Add(chestPos);
            }
        }

        // Remove processed chests from tracking
        foreach (Point pos in chestsToRemove)
        {
            trackedLockedChests.Remove(pos);
        }

        // Clean up chests that are far away
        List<Point> distantChests = new List<Point>();
        foreach (var kvp in trackedLockedChests)
        {
            float distance = Vector2.Distance(new Vector2(kvp.Key.X * 16, kvp.Key.Y * 16), Player.Center);
            if (distance > scanRange * 16 * 1.5f)
            {
                distantChests.Add(kvp.Key);
            }
        }
        foreach (Point pos in distantChests)
        {
            trackedLockedChests.Remove(pos);
        }
    }
}
