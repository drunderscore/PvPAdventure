namespace PvPAdventure.Core.Features;

public static class RTPSpawnSelectorSettings
{
    private static bool IsEnabled = false;

    public static bool GetIsEnabled() => IsEnabled;
    public static void SetIsEnabled(bool value) => IsEnabled = value;
}
