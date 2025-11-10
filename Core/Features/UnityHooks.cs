using Microsoft.Xna.Framework;
using PvPAdventure.Core.Helpers;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Features;

public class UnityHooks : ModSystem
{
    public override void Load()
    {
        Main.QueueMainThreadAction(() =>
        {
            On_Player.HasUnityPotion += OverrideUnityPotionCheck;
            On_Player.UnityTeleport += DisableRTPMenuAfterTeleport;
        });
    }

    public override void Unload()
    {
        Main.QueueMainThreadAction(() =>
        {
            On_Player.HasUnityPotion -= OverrideUnityPotionCheck;
            On_Player.UnityTeleport -= DisableRTPMenuAfterTeleport;
        });
    }

    private void DisableRTPMenuAfterTeleport(On_Player.orig_UnityTeleport orig, Player self, Vector2 telePos)
    {
        // this method does what it says
        RTPSpawnSelectorSettings.SetIsEnabled(false);
        Log.Debug("Unity teleport detected, disabling RTP menu.");
        orig(self, telePos);
    }

    private static bool OverrideUnityPotionCheck(Terraria.On_Player.orig_HasUnityPotion orig, Player self)
    {
        // debug
        Log.Debug("has unity: " + Main.LocalPlayer.HasUnityPotion());
        Log.Debug("is rtp menu enabled: " + RTPSpawnSelectorSettings.GetIsEnabled());

        if (RTPSpawnSelectorSettings.GetIsEnabled())
            return true;

        return orig(self);
    }
}
