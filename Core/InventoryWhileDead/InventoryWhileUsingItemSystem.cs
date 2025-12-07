using MonoMod.RuntimeDetour;
using PvPAdventure.Core.Helpers;
using System;
using System.Reflection;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace PvPAdventure.Core.InventoryWhileDead;

internal class InventoryWhileUsingItemSystem : ModSystem
{
    private Hook ignoreMouseHook;
    private Hook leftClickHook;

    public override void Load()
    {
        On_Main.DrawInterface_0_InterfaceLogic1 += ModifyInterface0;

        // Mouse interface
        MethodInfo getter = typeof(PlayerInput).GetMethod(
            "get_IgnoreMouseInterface",
            BindingFlags.Public | BindingFlags.Static
        );

        if (getter == null)
        {
            Log.Error("Getter not found!");
            return;
        }

        ignoreMouseHook = new Hook(getter, (Func<Func<bool>, bool>)OverrideIgnoreMouseInterface);
    }

    //private bool OverrideIgnoreMouseInterface(Func<bool> orig)
    //{

    private void ModifyInterface0(On_Main.orig_DrawInterface_0_InterfaceLogic1 orig)
    {
        // do nothing
    }

    public override void Unload()
    {
        On_Main.DrawInterface_0_InterfaceLogic1 -= ModifyInterface0;
        ignoreMouseHook?.Dispose();
    }

    private bool OverrideIgnoreMouseInterface(Func<bool> orig)
    {
        Player player = Main.LocalPlayer;

        // Allow mouse to interact while using items
        if (player.itemAnimation > 0)
            return false;

        // fallback to vanilla
        return orig();
    }
}
