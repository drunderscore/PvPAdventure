using MonoMod.Cil;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Visualization;

internal class AllowMouseInputWhenHoveringBuffIcons : ModSystem
{
    public override void Load()
    {
        // Don't set Player.mouseInterface when mousing over buffs.
        IL_Main.DrawBuffIcon += EditMainDrawBuffIcon;
    }
    public override void Unload()
    {
        IL_Main.DrawBuffIcon -= EditMainDrawBuffIcon;
    }

    private void EditMainDrawBuffIcon(ILContext il)
    {
        var cursor = new ILCursor(il);

        // First, find a store to Player.mouseInterface...
        // NOTE: The reference we find actually relates to gamepad, which we don't touch.
        cursor.GotoNext(i => i.MatchStfld<Player>("mouseInterface"));
        // ...and go past the gamepad interactions...
        cursor.Index += 2;
        // ...to remove the loads and stores to Player.mouseInterface for non-gamepad.
        cursor.RemoveRange(5);
    }
}
