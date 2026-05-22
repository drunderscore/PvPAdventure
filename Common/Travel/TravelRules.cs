using PvPAdventure.Core.Config;
using PvPAdventure.Core.Utilities;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Travel;

public static class TravelRules
{
    public static bool Enabled
    {
        get
        {
            ServerConfig config = ModContent.GetInstance<ServerConfig>();
            if (config == null)
            {
                Log.Warn("ServerConfig is null, defaulting to travel system disabled.");
                return false;
            }

            return config.TravelSystem.IsTravelSystemEnabled;
        }
    }
}
