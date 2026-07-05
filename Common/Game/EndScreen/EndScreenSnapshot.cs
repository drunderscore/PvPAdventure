using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Enums;

namespace PvPAdventure.Common.Game.EndScreen;

/// <summary>Match outcome for this team.</summary>
public enum EndScreenResult : byte
{
    Victory,
    Defeat,
    Tie
}

/// <summary>One active team's running score for the clickable end-screen scoreline.</summary>
public readonly record struct TeamScoreEntry(Team Team, int Score);

/// <summary>Serializable end screen data for one team.</summary>
public class EndScreenSnapshot
{
    public Team Team;
    public EndScreenResult Result;
    public int TeamScore;
    public int OpponentScore;
    public uint LocalPlayerReward;
    public List<EndScreenPlayerStats> Players = [];

    /// <summary>Every team's points (including teams with no players), in team order — drawn as e.g. 7-5-5-5.</summary>
    public List<TeamScoreEntry> AllScores = [];

    public static EndScreenSnapshot Deserialize(BinaryReader reader)
    {
        EndScreenSnapshot snapshot = new()
        {
            Team = (Team)reader.ReadByte(),
            Result = (EndScreenResult)reader.ReadByte(),
            TeamScore = reader.ReadInt32(),
            OpponentScore = reader.ReadInt32(),
            LocalPlayerReward = reader.ReadUInt32()
        };

        int scoreCount = reader.ReadInt32();
        for (int i = 0; i < scoreCount; i++)
            snapshot.AllScores.Add(new TeamScoreEntry((Team)reader.ReadByte(), reader.ReadInt32()));

        int playerCount = reader.ReadInt32();
        for (int i = 0; i < playerCount; i++)
            snapshot.Players.Add(EndScreenPlayerStats.Deserialize(reader));

        return snapshot;
    }

    public void Serialize(BinaryWriter writer)
    {
        writer.Write((byte)Team);
        writer.Write((byte)Result);
        writer.Write(TeamScore);
        writer.Write(OpponentScore);
        writer.Write(LocalPlayerReward);

        writer.Write(AllScores.Count);
        foreach (TeamScoreEntry entry in AllScores)
        {
            writer.Write((byte)entry.Team);
            writer.Write(entry.Score);
        }

        writer.Write(Players.Count);
        foreach (EndScreenPlayerStats player in Players)
            player.Serialize(writer);
    }
}

/// <summary>Serializable end screen data for one player.</summary>
public record EndScreenPlayerStats(
    byte PlayerIndex,
    Team Team,
    string Name,
    int Kills,
    int Deaths,
    uint DamageDealt,
    uint DamageTaken,
    uint TilesMined,
    uint TilesPlaced,
    uint ConsumablesUsed,
    uint LavaDeaths,
    uint FoodEaten,
    uint BossDamageDealt,
    uint PortalKills,
    uint DifferentWeaponsUsed,
    uint LostHoney,
    string RoleTitle = "",
    string RoleValue = "")
{
    public static EndScreenPlayerStats Deserialize(BinaryReader reader)
    {
        return new EndScreenPlayerStats(
            reader.ReadByte(),
            (Team)reader.ReadByte(),
            reader.ReadString(),
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadUInt32(),
            reader.ReadString(),
            reader.ReadString());
    }

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(PlayerIndex);
        writer.Write((byte)Team);
        writer.Write(Name ?? "");
        writer.Write(Kills);
        writer.Write(Deaths);
        writer.Write(DamageDealt);
        writer.Write(DamageTaken);
        writer.Write(TilesMined);
        writer.Write(TilesPlaced);
        writer.Write(ConsumablesUsed);
        writer.Write(LavaDeaths);
        writer.Write(FoodEaten);
        writer.Write(BossDamageDealt);
        writer.Write(PortalKills);
        writer.Write(DifferentWeaponsUsed);
        writer.Write(LostHoney);
        writer.Write(RoleTitle ?? "");
        writer.Write(RoleValue ?? "");
    }
}
