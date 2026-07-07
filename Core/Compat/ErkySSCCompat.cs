using PvPAdventure.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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

        Type saveSystemType = erkySsc.Code.GetType("ErkySSC.Common.SSC.SSCSaveSystem");

        if (saveSystemType == null)
        {
            Log.Warn("[PvPAdventure] Could not find ErkySSC.Common.SSC.SSCSaveSystem.");
            return;
        }

        MethodInfo getInstanceMethod = typeof(ModContent).GetMethod("GetInstance", BindingFlags.Public | BindingFlags.Static)?.MakeGenericMethod(saveSystemType);
        object saveSystem = getInstanceMethod?.Invoke(null, []);

        if (saveSystem == null)
        {
            Log.Warn("[PvPAdventure] Could not get SSCSaveSystem instance.");
            return;
        }

        MethodInfo sendMethod = saveSystemType.GetMethod("SendPacketToSavePlayerFile", BindingFlags.Instance | BindingFlags.Public);

        if (sendMethod == null)
        {
            Log.Warn("[PvPAdventure] Could not find SSCSaveSystem.SendPacketToSavePlayerFile().");
            return;
        }

        sendMethod.Invoke(saveSystem, []);
    }
}
