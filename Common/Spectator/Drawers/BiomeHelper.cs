using AssGen;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Utilities;
using ReLogic.Content;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Biomes = Terraria.GameContent.Bestiary.BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes;

namespace PvPAdventure.Common.Spectator.Drawers;

internal static class BiomeHelper
{
    private static readonly FieldInfo FilterIconFrameInfo = typeof(FilterProviderInfoElement).GetField("_filterIconFrame", BindingFlags.NonPublic | BindingFlags.Instance);

    internal static readonly SpawnConditionBestiaryInfoElement ShimmerBiome = new("Mods.PvPAdventure.Biomes.Shimmer", 0, "PvPAdventure/Assets/Custom/BG_Shimmer");
    internal static readonly SpawnConditionBestiaryInfoElement ForestBiome = new("Mods.PvPAdventure.Biomes.Forest", 0, "Terraria/Images/MapBG0");

    internal readonly struct PlayerBiomeVisual
    {
        public readonly SpawnConditionBestiaryInfoElement BestiaryBiome;
        public readonly Color BackgroundColor;
        public readonly int BackgroundIndex;

        public PlayerBiomeVisual(SpawnConditionBestiaryInfoElement bestiaryBiome, Color backgroundColor, int backgroundIndex)
        {
            BestiaryBiome = bestiaryBiome;
            BackgroundColor = backgroundColor;
            BackgroundIndex = backgroundIndex;
        }
    }

    internal static PlayerBiomeVisual GetBiomeVisual(Player player)
    {
        if (player == null || !player.active)
            return new(ForestBiome, Color.White, 0);

        if (player.ZoneShimmer)
            return new(ShimmerBiome, Color.White, -1);

        int tileX = (int)(player.Center.X / 16f);
        int tileY = (int)(player.Center.Y / 16f);
        Color color = player.dead ? new Color(50, 50, 50, 255) : Color.White;

        if (tileX < 0 || tileX >= Main.maxTilesX || tileY < 0 || tileY >= Main.maxTilesY)
            return new(ForestBiome, color, 0);

        Tile tile = Main.tile[tileX, tileY];
        if (tile == null)
            return new(ForestBiome, color, 0);

        int wall = tile.WallType;
        bool ocean = player.ZoneOverworldHeight && (tileX < 380 || tileX > Main.maxTilesX - 380);
        bool underground = player.ZoneDirtLayerHeight || player.ZoneRockLayerHeight;

        if (player.ZoneUnderworldHeight)
            return new(Biomes.TheUnderworld, color, 2);

        if (player.ZoneDungeon)
            return new(Biomes.TheDungeon, color, 4);

        if (wall == 87)
            return new(Biomes.SpiderNest, color, 13);

        if (underground)
        {
            if (wall is 86 or 108)
                return new(Biomes.Granite, color, 15);

            if (wall is 180 or 184)
                return new(Biomes.Marble, color, 16);

            if (player.ZoneGlowshroom)
                return new(Biomes.UndergroundMushroom, color, 20);

            if (player.ZoneCorrupt)
            {
                if (player.ZoneDesert)
                    return new(Biomes.CorruptUndergroundDesert, color, 39);

                if (player.ZoneSnow)
                    return new(Biomes.CorruptIce, color, 33);

                return new(Biomes.UndergroundCorruption, color, 22);
            }

            if (player.ZoneCrimson)
            {
                if (player.ZoneDesert)
                    return new(Biomes.CrimsonUndergroundDesert, color, 40);

                if (player.ZoneSnow)
                    return new(Biomes.CrimsonIce, color, 34);

                return new(Biomes.UndergroundCrimson, color, 23);
            }

            if (player.ZoneHallow)
            {
                if (player.ZoneDesert)
                    return new(Biomes.HallowUndergroundDesert, color, 41);

                if (player.ZoneSnow)
                    return new(Biomes.HallowIce, color, 35);

                return new(Biomes.UndergroundHallow, color, 21);
            }

            if (player.ZoneSnow)
                return new(Biomes.UndergroundSnow, color, 3);

            if (player.ZoneJungle)
                return new(Biomes.UndergroundJungle, color, 12);

            if (player.ZoneDesert)
                return new(Biomes.UndergroundDesert, color, 14);

            if (player.ZoneRockLayerHeight)
                return new(Biomes.Caverns, color, 31);

            return new(Biomes.Underground, color, 1);
        }

        if (player.ZoneGlowshroom)
            return new(Biomes.SurfaceMushroom, color, 19);

        if (player.ZoneSkyHeight)
            return new(Biomes.Sky, color, 32);

        if (player.ZoneCorrupt)
            return new(player.ZoneDesert ? Biomes.CorruptDesert : Biomes.TheCorruption, color, player.ZoneDesert ? 36 : 5);

        if (player.ZoneCrimson)
            return new(player.ZoneDesert ? Biomes.CrimsonDesert : Biomes.TheCrimson, color, player.ZoneDesert ? 37 : 6);

        if (player.ZoneHallow)
            return new(player.ZoneDesert ? Biomes.HallowDesert : Biomes.TheHallow, color, player.ZoneDesert ? 38 : 7);

        if (ocean)
            return new(Biomes.Ocean, color, 10);

        if (player.ZoneSnow)
            return new(Biomes.Snow, color, 11);

        if (player.ZoneJungle)
            return new(Biomes.Jungle, color, 8);

        if (player.ZoneDesert)
            return new(Biomes.Desert, color, 9);

        if (player.ZoneGraveyard)
            return new(Biomes.Graveyard, color, 26);

        if (Main.bloodMoon)
            return new(ForestBiome, Color.White, 0);

        return new(ForestBiome, Color.White, 0);
    }

    internal static PlayerBiomeVisual GetBiomeVisual(NPC npc)
    {
        if (npc == null || !npc.active)
            return new(ForestBiome, Color.White, 0);

        int tileX = Utils.Clamp((int)(npc.Center.X / 16f), 0, Main.maxTilesX - 1);
        int tileY = Utils.Clamp((int)(npc.Center.Y / 16f), 0, Main.maxTilesY - 1);
        Tile tile = Main.tile[tileX, tileY];
        int wall = tile == null ? WallID.None : tile.WallType;
        int sampledTileType = FindNearbyTileType(tileX, tileY);
        bool ocean = npc.Center.Y / 16f < Main.worldSurface + 10.0 && (tileX < 380 || tileX > Main.maxTilesX - 380);
        bool underground = npc.Center.Y > Main.worldSurface * 16.0;

        if (sampledTileType == TileID.ShimmerBlock)
            return new(ShimmerBiome, Color.White, -1);

        if (npc.Center.Y > (Main.maxTilesY - 232) * 16f)
            return new(Biomes.TheUnderworld, Color.White, 2);

        if (wall == WallID.BlueDungeonUnsafe || wall == WallID.GreenDungeonUnsafe || wall == WallID.PinkDungeonUnsafe)
            return new(Biomes.TheDungeon, Color.White, 4);

        if (wall == 87)
            return new(Biomes.SpiderNest, Color.White, 13);

        if (underground)
        {
            if (wall is 86 or 108)
                return new(Biomes.Granite, Color.White, 15);

            if (wall is 180 or 184)
                return new(Biomes.Marble, Color.White, 16);

            if (sampledTileType is TileID.MushroomGrass or TileID.MushroomPlants)
                return new(Biomes.UndergroundMushroom, Color.White, 20);

            if (IsCorruptTile(sampledTileType))
                return new(Biomes.UndergroundCorruption, Color.White, 22);

            if (IsCrimsonTile(sampledTileType))
                return new(Biomes.UndergroundCrimson, Color.White, 23);

            if (IsHallowTile(sampledTileType))
                return new(Biomes.UndergroundHallow, Color.White, 21);

            if (IsSnowTile(sampledTileType))
                return new(Biomes.UndergroundSnow, Color.White, 3);

            if (IsJungleTile(sampledTileType))
                return new(Biomes.UndergroundJungle, Color.White, 12);

            if (IsDesertTile(sampledTileType))
                return new(Biomes.UndergroundDesert, Color.White, 14);

            if (npc.Center.Y > Main.rockLayer * 16.0)
                return new(Biomes.Caverns, Color.White, 31);

            return new(Biomes.Underground, Color.White, 1);
        }

        if (sampledTileType is TileID.MushroomGrass or TileID.MushroomPlants)
            return new(Biomes.SurfaceMushroom, Color.White, 19);

        if (npc.Center.Y / 16f < Main.worldSurface * 0.45f)
            return new(Biomes.Sky, Color.White, 32);

        if (IsCorruptTile(sampledTileType))
            return new(Biomes.TheCorruption, Color.White, 5);

        if (IsCrimsonTile(sampledTileType))
            return new(Biomes.TheCrimson, Color.White, 6);

        if (IsHallowTile(sampledTileType))
            return new(Biomes.TheHallow, Color.White, 7);

        if (ocean)
            return new(Biomes.Ocean, Color.White, 10);

        if (IsSnowTile(sampledTileType))
            return new(Biomes.Snow, Color.White, 11);

        if (IsJungleTile(sampledTileType))
            return new(Biomes.Jungle, Color.White, 8);

        if (IsDesertTile(sampledTileType))
            return new(Biomes.Desert, Color.White, 9);

        return new(ForestBiome, Main.bloodMoon ? Color.White * 2f : Color.White, Main.bloodMoon ? 25 : 0);
    }

    internal static bool MatchesBiome(Player player, SpawnConditionBestiaryInfoElement biome)
    {
        return player != null && player.active && GetBiomeVisual(player).BestiaryBiome == biome;
    }

    internal static bool TryGetBestiaryIconDrawData(SpawnConditionBestiaryInfoElement biome, out Asset<Texture2D> texture, out Rectangle source)
    {
        // Custom shimmer
        if (biome == ShimmerBiome)
        {
            texture = Ass.Shimmer;
            source = texture.Value.Frame();
            return true;
        }

        // Custom forest
        if (biome == ForestBiome)
            biome = Biomes.Surface;

        texture = Main.Assets.Request<Texture2D>("Images/UI/Bestiary/Icon_Tags_Shadow");
        source = default;

        if (biome == null)
            return false;

        object frame = FilterIconFrameInfo?.GetValue(biome);

        if (frame is Point point)
        {
            source = texture.Frame(16, 5, point.X, point.Y);
            return true;
        }

        if (frame is int index)
        {
            source = texture.Frame(16, 5, index % 16, index / 16);
            return true;
        }

        return false;
    }

    private static int FindNearbyTileType(int tileX, int tileY)
    {
        for (int y = -2; y <= 2; y++)
        {
            for (int x = -2; x <= 2; x++)
            {
                int sampleX = Utils.Clamp(tileX + x, 0, Main.maxTilesX - 1);
                int sampleY = Utils.Clamp(tileY + y, 0, Main.maxTilesY - 1);
                Tile sample = Main.tile[sampleX, sampleY];
                if (sample != null && sample.HasTile)
                    return sample.TileType;
            }
        }

        return TileID.Dirt;
    }

    private static bool IsSnowTile(int tileType) => tileType is TileID.SnowBlock or TileID.IceBlock or TileID.CorruptIce or TileID.FleshIce or TileID.HallowedIce;
    private static bool IsDesertTile(int tileType) => tileType is TileID.Sand or TileID.HardenedSand or TileID.Sandstone or TileID.Ebonsand or TileID.Crimsand or TileID.Pearlsand;
    private static bool IsJungleTile(int tileType) => tileType is TileID.JungleGrass or TileID.Mud or TileID.JunglePlants or TileID.JungleThorns or TileID.Hive;
    private static bool IsCorruptTile(int tileType) => tileType is TileID.CorruptGrass or TileID.Ebonstone or TileID.Ebonsand or TileID.CorruptJungleGrass;
    private static bool IsCrimsonTile(int tileType) => tileType is TileID.CrimsonGrass or TileID.Crimstone or TileID.Crimsand or TileID.CrimsonJungleGrass;
    private static bool IsHallowTile(int tileType) => tileType is TileID.HallowedGrass or TileID.Pearlstone or TileID.Pearlsand;
}
