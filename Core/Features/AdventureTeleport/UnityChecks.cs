using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Features.AdventureTeleport;

public class UnityChecks : ModSystem
{
    public override void Load()
    {
        Main.QueueMainThreadAction(() =>
        {
            On_Player.HasUnityPotion += OverrideUnityPotionCheck;
            //On_Player.UnityTeleport += DisableRTPMenuAfterTeleport;
        });
    }

    public override void Unload()
    {
        Main.QueueMainThreadAction(() =>
        {
            On_Player.HasUnityPotion -= OverrideUnityPotionCheck;
            //On_Player.UnityTeleport -= DisableRTPMenuAfterTeleport;
        });
    }

    [Obsolete("This method is redundant since we always set this variable to false whenever map is closed.")]
    private void DisableRTPMenuAfterTeleport(On_Player.orig_UnityTeleport orig, Player self, Vector2 telePos)
    {
        AdventureTeleportStateSettings.SetIsEnabled(false);
        orig(self, telePos);
    }

    private static bool OverrideUnityPotionCheck(On_Player.orig_HasUnityPotion orig, Player self)
    {
        if (AdventureTeleportStateSettings.GetIsEnabled())
            return true;

        return orig(self);
    }
}
