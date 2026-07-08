using Microsoft.Xna.Framework;
using PvPAdventure.Common.Game;
using PvPAdventure.Core.Config;
using PvPAdventure.Core.Utilities;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace PvPAdventure.Common.Spawnbox;

[Autoload(Side = ModSide.Both)]
public sealed class SpawnBoxSystem : ModSystem
{
    private const string WidthKey = "SpawnBoxWidth";
    private const string HeightKey = "SpawnBoxHeight";
    private const string XOffsetKey = "SpawnBoxXOffset";
    private const string YOffsetKey = "SpawnBoxYOffset";
    private const string ThicknessKey = "SpawnBoxThickness";
    private const int TileSize = 16;

    public SpawnBoxSettings Settings { get; private set; } = SpawnBoxSettings.Default;
    public bool CanEnter => false;
    public bool CanExit => ModContent.GetInstance<GameManager>().CurrentPhase == GameManager.Phase.Playing;

    internal static SpawnBoxSettings DefaultSettings =>
        ModContent.GetInstance<ServerConfig>()?.SpawnBox?.ToSettings() ?? SpawnBoxSettings.Default;

    public Rectangle TileArea
    {
        get
        {
            SpawnBoxSettings s = Settings.Clamped();
            int x = Main.spawnTileX - s.Width / 2 + s.XOffset;
            int y = Main.spawnTileY - s.Height / 2 + s.YOffset;

            if (Main.maxTilesX > 0 && Main.maxTilesY > 0)
            {
                x = Math.Clamp(x, 0, Math.Max(0, Main.maxTilesX - s.Width));
                y = Math.Clamp(y, 0, Math.Max(0, Main.maxTilesY - s.Height));
            }

            return new Rectangle(x, y, s.Width, s.Height);
        }
    }

    public int Thickness => Settings.Clamped().Thickness;

    public Rectangle BorderOuterTileArea
    {
        get
        {
            Rectangle area = TileArea;
            area.Inflate(Thickness, Thickness);
            return area;
        }
    }

    public Rectangle[] BorderTileAreas
    {
        get
        {
            Rectangle inner = TileArea;
            Rectangle outer = BorderOuterTileArea;
            int t = Thickness;

            return new[]
            {
                new Rectangle(outer.X, outer.Y, outer.Width, t),
                new Rectangle(outer.X, inner.Bottom, outer.Width, t),
                new Rectangle(outer.X, inner.Y, t, inner.Height),
                new Rectangle(inner.Right, inner.Y, t, inner.Height)
            };
        }
    }

    public Rectangle[] BorderWorldAreas
    {
        get
        {
            Rectangle[] tiles = BorderTileAreas;
            for (int i = 0; i < tiles.Length; i++)
                tiles[i] = TileToWorld(tiles[i]);

            return tiles;
        }
    }

    public bool ContainsTile(int x, int y) => TileArea.Contains(x, y);
    public bool ContainsTile(Point tile) => ContainsTile(tile.X, tile.Y);
    public bool TouchesWorldHitbox(Rectangle hitbox) => TileArea.Intersects(hitbox.ToTileRectangle());
    public bool TouchesTileRectangle(Rectangle tiles) => TileArea.Intersects(tiles);
    public bool TouchesBorderWorldHitbox(Rectangle hitbox)
    {
        foreach (Rectangle border in BorderWorldAreas)
            if (border.Intersects(hitbox))
                return true;

        return false;
    }

    public static Rectangle TileToWorld(Rectangle tiles) =>
        new(tiles.X * TileSize, tiles.Y * TileSize, tiles.Width * TileSize, tiles.Height * TileSize);

    internal void SetFromTool(SpawnBoxSettings settings, bool sync = false)
    {
        Settings = settings.Clamped();

        if (sync && Main.netMode == NetmodeID.Server)
            SpawnBoxNetHandler.SendSync(Settings);
    }

    internal void ReceiveSync(SpawnBoxSettings settings) => Settings = settings.Clamped();

    public override void ClearWorld() => Settings = DefaultSettings;

    public override void SaveWorldData(TagCompound tag)
    {
        tag[WidthKey] = Settings.Width;
        tag[HeightKey] = Settings.Height;
        tag[XOffsetKey] = Settings.XOffset;
        tag[YOffsetKey] = Settings.YOffset;
        tag[ThicknessKey] = Settings.Thickness;
    }

    public override void LoadWorldData(TagCompound tag)
    {
        Settings = tag.ContainsKey(WidthKey)
            ? new SpawnBoxSettings(
                tag.GetInt(WidthKey),
                tag.GetInt(HeightKey),
                tag.GetInt(XOffsetKey),
                tag.GetInt(YOffsetKey),
                tag.ContainsKey(ThicknessKey) ? tag.GetInt(ThicknessKey) : SpawnBoxSettings.DefaultThickness).Clamped()
            : DefaultSettings;
    }

    public override void NetSend(BinaryWriter writer) => Settings.Write(writer);
    public override void NetReceive(BinaryReader reader) => Settings = SpawnBoxSettings.Read(reader);
}
