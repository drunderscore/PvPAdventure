using PvPAdventure.Core.Net;
using System.IO;

namespace PvPAdventure.Common.Teams;

// This mod packet is required as opposed to MessageID.PlayerTeam, because the latter would be rejected during early
// connection, which is important for us.
public sealed class Team(byte player, Terraria.Enums.Team team) : IPacket<Team>
{
    public byte Player { get; set; } = player;
    public Terraria.Enums.Team Value { get; set; } = team;

    public static Team Deserialize(BinaryReader reader)
    {
        return new(reader.ReadByte(), (Terraria.Enums.Team)reader.ReadInt32());
    }

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(Player);
        writer.Write((int)Value);
    }
}
