namespace PvPAdventure.Core.SSC;

public static class SSCBuild
{
    public static bool Enabled
    {
        get
        {
#if DEBUG
            return false;
#else
            return false; // Change this to false to keep SSC disabled in release builds
#endif
        }
    }
}
