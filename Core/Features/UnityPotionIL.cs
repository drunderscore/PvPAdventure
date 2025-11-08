using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using static PvPAdventure.Core.Features.TeleportMapSystem;

namespace PvPAdventure.Core.Features;

public class UnityPotionIL : ModSystem
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
        RTPSpawnSelectorSettings.IsEnabled = false;
        orig(self, telePos);
    }

    private static bool OverrideUnityPotionCheck(Terraria.On_Player.orig_HasUnityPotion orig, Player self)
    {
        // debug
        var logger = ModContent.GetInstance<PvPAdventure>().Logger;
        //logger.Debug("has unity: " + Main.LocalPlayer.HasUnityPotion());
        //logger.Debug("is rtp menu enabled: " + "has unity: " + RTPSpawnSelectorSettings.IsEnabled());

        if (RTPSpawnSelectorSettings.IsEnabled)
            return true;

        return orig(self);
    }
}

    //private void OverrideUnityPotionCheck(ILContext il)
    //{
    //    try
    //    {
    //        var c = new ILCursor(il);
            
    //        if (c.TryGotoNext(i => i.MatchRet()))
    //        {
    //            c.Emit(OpCodes.Pop);

    //            c.EmitDelegate<Func<bool>>(() =>
    //            {
    //                return RTPSpawnSelectorSettings.IsEnabled;
    //            });
    //        }
    //    }
    //    catch (Exception e)
    //    {
    //        throw new ILPatchFailureException(Mod, il, e);
    //    }
    //}
