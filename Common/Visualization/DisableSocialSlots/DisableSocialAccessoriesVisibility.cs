using MonoMod.RuntimeDetour;
using PvPAdventure.Core.Config;
using System;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Visualization.DisableSocialSlots;

internal class DisableSocialAccessoriesVisibility : ModSystem
{
    private static Hook _updateVisibleAccessoriesHook;

    public override void Load()
    {
        if (Main.dedServ)
            return;

        MethodInfo method = typeof(Player).GetMethod(
            "UpdateVisibleAccessories",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);

        if (method == null)
        {
            Log.Warn("Player.UpdateVisibleAccessories not found. Vanity suppression not installed.");
            return;
        }

        _updateVisibleAccessoriesHook = new Hook(method, Hook_UpdateVisibleAccessories);
        //Log.Info("Hooked Player.UpdateVisibleAccessories: vanity accessory visuals disabled, functional forced visible.");
    }

    public override void Unload()
    {
        _updateVisibleAccessoriesHook?.Dispose();
        _updateVisibleAccessoriesHook = null;
    }

    private delegate void Orig_UpdateVisibleAccessories(Player self);

    private static void Hook_UpdateVisibleAccessories(Orig_UpdateVisibleAccessories orig, Player self)
    {
        if (Main.dedServ)
        {
            orig(self);
            return;
        }

        // Client config
        var cfg = ModContent.GetInstance<ClientConfig>();

        // Always suppress vanity for OTHER players.
        // Only let the local player opt out via config.
        bool suppressForThisPlayer = self.whoAmI != Main.myPlayer
            || cfg.HideVanityVisuals;

        if (!suppressForThisPlayer)
        {
            orig(self);
            return;
        }

        // 1) Force functional accessories to always be visible (disable the "eye" hiding).
        // hideVisibleAccessory is indexed by accessory slot id (same indices used in AccessorySlotLoader DrawVisibility).
        if (self.hideVisibleAccessory != null)
        {
            for (int i = 0; i < self.hideVisibleAccessory.Length; i++)
                self.hideVisibleAccessory[i] = false;
        }

        // 2) Temporarily blank vanity accessory slots so they cannot override visuals.
        // Vanilla accessory slots are 3..(dye.Length-1). Vanity counterparts are slot + dye.Length.
        Item[] armor = self.armor;
        Item[] saved = null;

        int dyeLen = self.dye?.Length ?? 0;
        if (armor != null && dyeLen > 0)
        {
            // We'll save only the vanity accessory range we touch.
            saved = new Item[armor.Length];

            for (int slot = 3; slot < dyeLen; slot++)
            {
                int vanityIndex = slot + dyeLen;
                if (vanityIndex < 0 || vanityIndex >= armor.Length)
                    continue;

                // Save and clear (only if not air to reduce allocations).
                if (!armor[vanityIndex].IsAir)
                {
                    saved[vanityIndex] = armor[vanityIndex];
                    armor[vanityIndex] = new Item(); // Air
                }
            }
        }

        // Run vanilla logic with vanity accessories effectively removed
        orig(self);

        // Restore vanity items
        if (saved != null && armor != null)
        {
            for (int i = 0; i < saved.Length; i++)
            {
                if (saved[i] != null)
                    armor[i] = saved[i];
            }
        }
    }
}
