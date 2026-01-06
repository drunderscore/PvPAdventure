using System.IO;

namespace PvPAdventure.Core.SSC;

/// <summary>
/// Used for copying the joined player's appearance to the server-side character.
/// </summary>
public struct PlayerAppearance
{
    public int SkinVariant;
    public int Hair;

    public Color SkinColor;
    public Color EyeColor;
    public Color HairColor;

    public Color ShirtColor;
    public Color UnderShirtColor;
    public Color PantsColor;
    public Color ShoeColor;
}

// Converts color to/from binary because Terraria's BinaryReader does not support Color directly.
public static class ColorReader
{
    public static void WriteColor(BinaryWriter w, Color c)
    {
        w.Write(c.R);
        w.Write(c.G);
        w.Write(c.B);
        w.Write(c.A);
    }

    public static Color ReadColor(BinaryReader r)
    {
        return new Color(
            r.ReadByte(),
            r.ReadByte(),
            r.ReadByte(),
            r.ReadByte()
        );
    }
}

