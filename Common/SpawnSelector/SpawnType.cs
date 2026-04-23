namespace PvPAdventure.Common.SpawnSelector;

/// <summary>
/// Specifies the spawn location types for a player.
/// These are available from:
/// - the fullscreen map, 
/// - from spawn regions (world spawn, my bed and team beds), 
/// - when using the Adventure Mirror item, 
/// - and when dead
/// </summary>
public enum SpawnType : byte
{
    None,
    World,
    MyBed,
    MyPortal,
    Random,
    TeammatePortal,
    TeammateBed
}
