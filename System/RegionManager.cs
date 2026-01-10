using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.System;

[Autoload(Side = ModSide.Both)]
public class RegionManager : ModSystem
{
    private readonly List<Region> _regions = [];
    public IReadOnlyList<Region> Regions => _regions;

    public class Region
    {
        public Rectangle Area { get; set; }
        public int Order { get; set; }

        public bool CanModifyTiles { get; set; }
        public bool AllowCombat { get; set; }
        public bool CanUseWormhole { get; set; }
        public bool CanRandomTeleport { get; set; }
        public bool CanRecall { get; set; }
        public bool CanEnter { get; set; }
        public bool CanExit { get; set; }
        public Rectangle CollisionRingBounds =>
        new(Area.X - 1, Area.Y - 1, Area.Width + 2, Area.Height + 2);
    }

    public override void Load()
    {
        On_Collision.TileCollision += OnCollisionTileCollision;
    }

    private Vector2 OnCollisionTileCollision(On_Collision.orig_TileCollision orig, Vector2 position, Vector2 velocity,
    int width, int height, bool fallthrough, bool fall2, int gravdir)
    {
        // Always call to the original to ensure we set Collision.up and Collision.down, and collide with real tiles
        // as expected.
        var collisionVelocity = orig(position, velocity, width, height, fallthrough, fall2, gravdir);

        var sourceHitbox = new Rectangle((int)position.X, (int)position.Y, width, height).ToTileRectangle();
        var sourceIntersections = GetRegionsIntersecting(sourceHitbox).ToHashSet();

        var horizontalDestinationHitbox =
            new Rectangle((int)(position.X + velocity.X), (int)position.Y, width, height).ToTileRectangle();
        var verticalDestinationHitbox = new Rectangle((int)position.X, (int)(position.Y + velocity.Y), width, height)
            .ToTileRectangle();

        // Check both +velocity.X and +velocity.Y to know what direction is causing us to collide.

        if (ShouldPrevent(sourceIntersections, GetRegionsIntersecting(horizontalDestinationHitbox).ToHashSet()))
            collisionVelocity.X = 0.0f;

        if (ShouldPrevent(sourceIntersections, GetRegionsIntersecting(verticalDestinationHitbox).ToHashSet()))
            collisionVelocity.Y = 0.0f;

        // extra tightening from inside: stop 1 tile earlier than the outer ring
        var destHitboxX = new Rectangle((int)(position.X + collisionVelocity.X), (int)position.Y, width, height)
            .ToTileRectangle();
        var destHitboxY = new Rectangle((int)position.X, (int)(position.Y + collisionVelocity.Y), width, height)
            .ToTileRectangle();

        foreach (var region in sourceIntersections)
        {
            if (!region.CanExit)
            {
                var innerBounds = region.CollisionRingBounds;
                innerBounds.Inflate(-1, -1); // shrink 1 tile on all sides

                if (innerBounds.Contains(sourceHitbox) && !innerBounds.Contains(destHitboxX))
                    collisionVelocity.X = 0.0f;

                if (innerBounds.Contains(sourceHitbox) && !innerBounds.Contains(destHitboxY))
                    collisionVelocity.Y = 0.0f;
            }
        }

        return collisionVelocity;

        bool ShouldPrevent(HashSet<Region> source, HashSet<Region> destination)
        {
            var differingIntersections = new HashSet<Region>(source);
            differingIntersections.SymmetricExceptWith(destination);

            foreach (var region in differingIntersections)
            {
                // If this region is in the source intersections, it must not be in the destination intersections.
                // This means you have exited this region.
                if (sourceIntersections.Contains(region))
                {
                    if (!region.CanExit)
                        return true;
                }
                // This region is not in the source intersections, it must be in the destination intersections.
                // This means you have entered this region.
                else
                {
                    if (!region.CanEnter)
                        return true;
                }
            }

            return false;
        }
    }

    public override void NetSend(BinaryWriter writer)
    {
        writer.Write(_regions.Count);

        foreach (var region in _regions)
        {
            writer.Write(region.Area.X);
            writer.Write(region.Area.Y);
            writer.Write(region.Area.Width);
            writer.Write(region.Area.Height);
            writer.Write(region.Order);

            writer.Write(region.CanModifyTiles);
            writer.Write(region.AllowCombat);
            writer.Write(region.CanUseWormhole);
            writer.Write(region.CanRandomTeleport);
            writer.Write(region.CanRecall);
            writer.Write(region.CanEnter);
            writer.Write(region.CanExit);
        }
    }

    public override void NetReceive(BinaryReader reader)
    {
        _regions.Clear();

        var count = reader.ReadInt32();

        for (var i = 0; i < count; i++)
        {
            var x = reader.ReadInt32();
            var y = reader.ReadInt32();
            var width = reader.ReadInt32();
            var height = reader.ReadInt32();

            var order = reader.ReadInt32();

            var canModifyTiles = reader.ReadBoolean();
            var allowCombat = reader.ReadBoolean();
            var canUseWormhole = reader.ReadBoolean();
            var canRandomTeleport = reader.ReadBoolean();
            var canRecall = reader.ReadBoolean();
            var canEnter = reader.ReadBoolean();
            var canExit = reader.ReadBoolean();

            _regions.Add(new()
            {
                Area = new(x, y, width, height),
                Order = order,
                CanModifyTiles = canModifyTiles,
                AllowCombat = allowCombat,
                CanUseWormhole = canUseWormhole,
                CanRandomTeleport = canRandomTeleport,
                CanRecall = canRecall,
                CanEnter = canEnter,
                CanExit = canExit
            });
        }

        SortRegions();
    }

    private void SortRegions()
    {
        _regions.Sort((a, b) => b.Order.CompareTo(a));
    }

    public Region GetRegionContaining(Point point) => GetRegionsContaining(point).FirstOrDefault();
    public Region GetRegionIntersecting(Rectangle point) => GetRegionsIntersecting(point).FirstOrDefault();

    public IEnumerable<Region> GetRegionsContaining(Point point) =>
        _regions.Where(region => region.Area.Contains(point));

    public IEnumerable<Region> GetRegionsIntersecting(Rectangle rectangle) =>
    _regions.Where(region => region.CollisionRingBounds.Intersects(rectangle));

    public override void ClearWorld()
    {
        _regions.Clear();
    }

    public override void OnWorldLoad()
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            _regions.Add(new Region
            {
                Area = new(Main.spawnTileX - 25, Main.spawnTileY - 25, 50, 50),
                Order = 10
            });

            SortRegions();
        }
    }
}