using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Features.SpawnSelector.Systems;

/// <summary>
/// This allows the player to click on teammates to teleport to them.
/// </summary>
public class UnityHooks : ModSystem
{
    public override void Load()
    {
        On_Player.HasUnityPotion += OnHasUnityPotion;
    }

    public override void Unload()
    {
        On_Player.HasUnityPotion -= OnHasUnityPotion;
    }

    private static bool OnHasUnityPotion(On_Player.orig_HasUnityPotion orig, Player self)
    {
        if (SpawnSelectorSystem.GetEnabled())
            return true;

        return orig(self);
    }
}
