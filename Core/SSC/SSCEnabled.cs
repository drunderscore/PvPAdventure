using Terraria.ModLoader;

namespace PvPAdventure.Core.SSC;

/// <summary>
/// Determines if server-side characters (SSC) are enabled in this build.
/// </summary>
public static class SSCEnabled
{
    public static bool IsEnabled
    {
        get
        {
            

#if DEBUG
            return true;
#else
            var config = ModContent.GetInstance<AdventureServerConfig>();
            if (config != null)
            {
                return config.IsSSCEnabled;
            }
#endif
        }
    }
}
