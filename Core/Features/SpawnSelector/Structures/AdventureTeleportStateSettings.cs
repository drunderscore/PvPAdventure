namespace PvPAdventure.Core.Features.SpawnSelector.Structures;

public static class AdventureTeleportStateSettings
{
    private static bool IsEnabled = false;

    public static bool GetIsEnabled() => IsEnabled;
    public static void SetIsEnabled(bool value) => IsEnabled = value;
}
