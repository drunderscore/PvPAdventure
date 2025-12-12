using MonoMod.RuntimeDetour;
using PvPAdventure.Content.Items;
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
        orig();
    }

    public override void Unload()
    {
        On_Main.DrawInterface_0_InterfaceLogic1 -= ModifyInterface0;
        ignoreMouseHook?.Dispose();
    }

    private bool OverrideIgnoreMouseInterface(Func<bool> orig)
    {
        Player p = Main.LocalPlayer;

        if (p != null &&
            Main.playerInventory &&                 
            p.itemAnimation > 0 &&
            p.HeldItem.type == ModContent.ItemType<AdventureMirror>())
        {
            return false; // allow UI interaction while AM is animating
        }

        return orig();
    }
}
