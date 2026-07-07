using System;
using System.IO;

namespace PvPAdventure.Common.Spawnbox;

public readonly record struct SpawnBoxSettings(int Width, int Height, int XOffset, int YOffset, int Thickness)
{
    public const int MinSize = 5;
    public const int MaxSize = 50;
    public const int MinOffset = -20;
    public const int MaxOffset = 20;
    public const int DefaultOffset = 0;
    public const int MinThickness = 1;
    public const int MaxThickness = 10;
    public const int DefaultThickness = 1;
    public static readonly SpawnBoxSettings Default = new(MaxSize, MaxSize, DefaultOffset, DefaultOffset, DefaultThickness);

    public SpawnBoxSettings(int width, int height, int xOffset, int yOffset)
        : this(width, height, xOffset, yOffset, DefaultThickness)
    {
    }

    public SpawnBoxSettings Clamped() => new(
        Math.Clamp(Width, MinSize, MaxSize),
        Math.Clamp(Height, MinSize, MaxSize),
        Math.Clamp(XOffset, MinOffset, MaxOffset),
        Math.Clamp(YOffset, MinOffset, MaxOffset),
        Math.Clamp(Thickness, MinThickness, MaxThickness));

    public void Write(BinaryWriter writer)
    {
        writer.Write((byte)Width);
        writer.Write((byte)Height);
        writer.Write((sbyte)XOffset);
        writer.Write((sbyte)YOffset);
        writer.Write((byte)Thickness);
    }

    public static SpawnBoxSettings Read(BinaryReader reader) =>
        new SpawnBoxSettings(reader.ReadByte(), reader.ReadByte(), reader.ReadSByte(), reader.ReadSByte(), reader.ReadByte()).Clamped();
}
