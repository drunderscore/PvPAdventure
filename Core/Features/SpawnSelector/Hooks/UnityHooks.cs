using Microsoft.Xna.Framework;
using PvPAdventure.Core.Features.SpawnSelector.Systems;
using System;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Features.SpawnSelector.Hooks;

/// <summary>
/// This allows the player to click on teammates to teleport to them.
/// </summary>
public class UnityHooks : ModSystem
{
    public override void Load()
    {
        Main.QueueMainThreadAction(() =>
        {
            On_Player.HasUnityPotion += OnHasUnityPotion;
            On_Player.UnityTeleport += OnUnityTeleport;
        });
    }

    public override void Unload()
    {
        Main.QueueMainThreadAction(() =>
        {
            On_Player.HasUnityPotion -= OnHasUnityPotion;
            On_Player.UnityTeleport -= OnUnityTeleport;
        });
    }

    private void OnUnityTeleport(On_Player.orig_UnityTeleport orig, Player self, Vector2 telePos)
    {
        SpawnSelectorSystem.SetEnabled(false);
        orig(self, telePos);
    }

    private static bool OnHasUnityPotion(On_Player.orig_HasUnityPotion orig, Player self)
    {
        if (SpawnSelectorSystem.GetEnabled())
            return true;

        return orig(self);
    }
}
