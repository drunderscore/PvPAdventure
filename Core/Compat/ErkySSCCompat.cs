using PvPAdventure.Core.Utilities;
using System;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Compat;

public static class ErkySSCCompat
{
    public static bool TryApplyStartingItems(Player player)
    {
        if (!TryGetMod(out Mod erkySsc) || erkySsc.Code == null)
            return false;

        try
        {
            Type type = erkySsc.Code.GetType("ErkySSC.Common.SSC.StartingItems");
            MethodInfo method = type?.GetMethod(
                "ApplyStartItems",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: [typeof(Player)],
                modifiers: null);

            if (method == null)
                return false;

            method.Invoke(null, [player]);
            return true;
        }
        catch (Exception exception)
        {
            Log.Warn($"[PvPAdventure] Could not apply ErkySSC starting items: {exception.Message}");
            return false;
        }
    }

    public static bool IsAdmin(int whoAmI)
    {
        if (Main.netMode == NetmodeID.SinglePlayer)
            return true;

        if (!TryGetMod(out Mod erkySsc))
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

        if (!TryGetMod(out Mod erkySsc))
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

    private static bool TryGetMod(out Mod mod) =>
        ModLoader.TryGetMod("ErkySSC", out mod) ||
        ModLoader.TryGetMod("ErkySsc", out mod);
}
