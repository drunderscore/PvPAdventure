namespace PvPAdventure.Core.Features.AdventureTeleport;

public static class AdventureTeleportStateSettings
{
    private static bool IsEnabled = false;

    public static bool GetIsEnabled() => IsEnabled;
    public static void SetIsEnabled(bool value) => IsEnabled = value;
}
