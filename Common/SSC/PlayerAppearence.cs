using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.SSC;

public static class Appearance
{
    /// <summary>
    /// Used for copying the joined player's appearance to the server-side character.
    /// </summary>
    public struct PlayerAppearance
    {
        public bool Male;
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

    // Converts color to bytes because BinaryWriter does not support sending Color.
    private static void WriteColor(BinaryWriter w, Color c)
    {
        w.Write(c.R);
        w.Write(c.G);
        w.Write(c.B);
        w.Write(c.A);
    }

    private static Color ReadColor(BinaryReader r)
    {
        return new Color(
            r.ReadByte(),
            r.ReadByte(),
            r.ReadByte(),
            r.ReadByte()
        );
    }
    public static void WriteAppearance(ModPacket packet, Player player)
    {
        packet.Write(player.Male);
        packet.Write(player.skinVariant);
        packet.Write(player.hair);
        WriteColor(packet, player.skinColor);
        WriteColor(packet, player.eyeColor);
        WriteColor(packet, player.hairColor);
        WriteColor(packet, player.shirtColor);
        WriteColor(packet, player.underShirtColor);
        WriteColor(packet, player.pantsColor);
        WriteColor(packet, player.shoeColor);
    }

    public static void WriteAppearance(ModPacket packet, PlayerAppearance appearance)
    {
        packet.Write(appearance.Male);
        packet.Write(appearance.SkinVariant);
        packet.Write(appearance.Hair);
        WriteColor(packet, appearance.SkinColor);
        WriteColor(packet, appearance.EyeColor);
        WriteColor(packet, appearance.HairColor);
        WriteColor(packet, appearance.ShirtColor);
        WriteColor(packet, appearance.UnderShirtColor);
        WriteColor(packet, appearance.PantsColor);
        WriteColor(packet, appearance.ShoeColor);
    }

    public static PlayerAppearance ReadAppearance(BinaryReader reader)
    {
        return new PlayerAppearance
        {
            Male = reader.ReadBoolean(),
            SkinVariant = reader.ReadInt32(),
            Hair = reader.ReadInt32(),
            SkinColor = ReadColor(reader),
            EyeColor = ReadColor(reader),
            HairColor = ReadColor(reader),
            ShirtColor = ReadColor(reader),
            UnderShirtColor = ReadColor(reader),
            PantsColor = ReadColor(reader),
            ShoeColor = ReadColor(reader)
        };
    }
    public static void ApplyAppearance(Player p, PlayerAppearance a)
    {
        p.Male = a.Male;
        p.skinVariant = a.SkinVariant;
        p.hair = a.Hair;
        p.skinColor = a.SkinColor;
        p.eyeColor = a.EyeColor;
        p.hairColor = a.HairColor;
        p.shirtColor = a.ShirtColor;
        p.underShirtColor = a.UnderShirtColor;
        p.pantsColor = a.PantsColor;
        p.shoeColor = a.ShoeColor;
    }
}

