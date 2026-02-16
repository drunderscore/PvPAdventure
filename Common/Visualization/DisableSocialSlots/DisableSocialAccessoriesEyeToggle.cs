using Microsoft.Xna.Framework.Graphics;
using MonoMod.RuntimeDetour;
using PvPAdventure.Core.Config;
using System.Reflection;
using Terraria.ModLoader;

internal sealed class DisableSocialAccessoriesEyeToggle : ModSystem
{
    private static Hook _drawVisibilityHook;

    public override void Load()
    {
        MethodInfo method = typeof(AccessorySlotLoader).GetMethod("DrawVisibility",BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        if (method == null)
        {
            Log.Warn("AccessorySlotLoader.DrawVisibility not found. Eye toggle hook not installed.");
            return;
        }

        _drawVisibilityHook = new Hook(method, Hook_DrawVisibility);
        //Log.Info("Hooked AccessorySlotLoader.DrawVisibility (eye toggle disabled).");
    }

    public override void Unload()
    {
        _drawVisibilityHook?.Dispose();
        _drawVisibilityHook = null;
    }

    private delegate bool Orig_DrawVisibility(
        AccessorySlotLoader self,
        ref bool hideFlag,
        int context,
        int xLoc,
        int yLoc,
        out int xOut,
        out int yOut,
        out Texture2D texOut);

    private static bool Hook_DrawVisibility(
        Orig_DrawVisibility orig,
        AccessorySlotLoader self,
        ref bool hideFlag,
        int context,
        int xLoc,
        int yLoc,
        out int xOut,
        out int yOut,
        out Texture2D texOut)
    {
        // Client config
        var cfg = ModContent.GetInstance<ClientConfig>();
        if (cfg.ShowVanityVisuals)
        {
            return orig(self, ref hideFlag, context, xLoc, yLoc, out xOut, out yOut, out texOut);
        }

        // Do NOT toggle anything. Just hide the icon by drawing it off-screen.
        xOut = -100000;
        yOut = -100000;
        texOut = Terraria.GameContent.TextureAssets.MagicPixel.Value;

        // Return false so the slot itself behaves normally (mouse works on the slot).
        // (Returning true would tell the slot draw to "skip mouse" logic.)
        return false;
    }
}
