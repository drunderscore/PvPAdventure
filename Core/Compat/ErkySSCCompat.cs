using PvPAdventure.Core.Utilities;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Compat;

public static class ErkySSCCompat
{
    public static bool IsAdmin(int whoAmI)
    {
        if (Main.netMode == NetmodeID.SinglePlayer)
            return true;

        if (!ModLoader.TryGetMod("ErkySSC", out Mod erkySsc))
            return false;

        try
        {
            return erkySsc.Call("IsAdmin", whoAmI) is bool isAdmin && isAdmin;
        }
        catch (Exception e)
        {
            Log.Chat($"Failed to check ErkySSC admin permission. whoAmI={whoAmI}, error={e.Message}");
            return false;
        }
    }

    public static void TrySendErkySSCSave()
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        if (!ModLoader.TryGetMod("ErkySSC", out Mod erkySsc))
        {
            Log.Warn("[PvPAdventure] Could not request ErkySSC save because ErkySSC is not loaded.");
            return;
        }

        try
        {
            if (erkySsc.Call("RequestClientSave") is not true)
                Log.Warn("[PvPAdventure] ErkySSC rejected the client save request.");
        }
        catch (Exception exception)
        {
            Log.Warn($"[PvPAdventure] Could not request an ErkySSC save: {exception.Message}");
        }
    }
}
