using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.Map;
using Terraria.ModLoader;

namespace PvPAdventure.System;

[Autoload(Side = ModSide.Client)]
public class WorldMapSyncManager : ModSystem
{
    private readonly Dictionary<Point16, byte> _betterMapTileUpdates = new();

    // FIXME: not field not public not dumb
    public bool ignore = false;

    public sealed class Lighting(Dictionary<Point16, byte> tileLight) : IPacket<Lighting>
    {
        public Dictionary<Point16, byte> TileLight { get; } = tileLight;

        public static Lighting Deserialize(BinaryReader reader)
        {
            var tileLight = new Dictionary<Point16, byte>();

            var count = reader.ReadInt32();

            for (var i = 0; i < count; i++)
            {
                var x = reader.ReadInt16();
                var y = reader.ReadInt16();
                var light = reader.ReadByte();

                tileLight[new(x, y)] = light;
            }

            return new(tileLight);
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(TileLight.Count);

            foreach (var (point, light) in TileLight)
            {
                writer.Write(point.X);
                writer.Write(point.Y);
                writer.Write(light);
            }
        }
    }

    public override void Load()
    {
        On_WorldMap.UpdateLighting += OnWorldMapUpdateLighting;
    }

    private bool OnWorldMapUpdateLighting(On_WorldMap.orig_UpdateLighting orig, WorldMap self, int x, int y, byte light)
    {
        var updated = orig(self, x, y, light);

        // FIXME: Relying on the return value means that one player traversing a revealed area for them will not
        //        properly reveal the area for a player who hasn't revealed it. Causes a lot of false lighting and
        //        black tiles. Without this check though, it's very verbose I believe. Also causes black streaks across
        //        the map.
        if (updated && !ignore)
        {
            lock (_betterMapTileUpdates)
            {
                var mapTile = Main.Map[x, y];
                _betterMapTileUpdates[new(x, y)] = mapTile.Light;
            }
        }

        return updated;
    }

    // FIXME: Somewhere that makes sense
    public override void PostUpdatePlayers()
    {
        lock (_betterMapTileUpdates)
        {
            if (_betterMapTileUpdates.Count == 0)
                return;

            try
            {
                var packet = Mod.GetPacket();
                packet.Write((byte)AdventurePacketIdentifier.WorldMapLighting);
                new Lighting(_betterMapTileUpdates).Serialize(packet);
                packet.Send();
            }
            finally
            {
                _betterMapTileUpdates.Clear();
            }
        }
    }
}