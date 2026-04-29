using Microsoft.Xna.Framework;

namespace PvPAdventure.Common.Travel;

public readonly struct TravelTarget
{
    public readonly TravelType Type;
    public readonly int PlayerIndex;
    public readonly Vector2 WorldPosition;
    public readonly string Name;
    public readonly string BiomeName;
    public readonly bool Available;
    public readonly string DisabledReason;

    public TravelTarget(TravelType type, int playerIndex, Vector2 worldPosition, string name, string biomeName, bool available, string disabledReason = "")
    {
        Type = type;
        PlayerIndex = playerIndex;
        WorldPosition = worldPosition;
        Name = name;
        BiomeName = biomeName;
        Available = available;
        DisabledReason = disabledReason;
    }
}
