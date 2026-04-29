using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Misc.DeadSystems;

/// <summary>
/// Allows fullscreen map access while dead.
/// </summary>
[Autoload(Side = ModSide.Client)]
internal class MapWhileDeadSystem : ModSystem
{
    public override void Load()
    {
        On_Player.UpdateDead += Player_UpdateDead;
    }
    public override void Unload()
    {
        On_Player.UpdateDead -= Player_UpdateDead;
    }

    private static void Player_UpdateDead(On_Player.orig_UpdateDead orig, Player self)
    {
        orig(self);

        // Only the local client should interpret local input and toggle global UI state
        if (Main.dedServ || self.whoAmI != Main.myPlayer)
            return;

        // Dont allow toggling while typing / editing UI text boxes
        if (Main.drawingPlayerChat || Main.editSign || Main.editChest || Main.blockInput)
            return;

        bool mapDown = PlayerInput.Triggers.Current.MapFull; 
        if (mapDown)
        {
            if (self.releaseMapFullscreen)
            {
                if (!Main.mapFullscreen)
                {
                    Main.playerInventory = false;
                    self.talkNPC = -1;
                    Main.npcChatCornerItem = 0;

                    //Main.mapFullscreenScale = 2.5f;
                    Main.mapFullscreen = true;
                    Main.resetMapFull = true;
                }
                else
                {
                    Main.mapFullscreen = false;
                }
            }

            self.releaseMapFullscreen = false;
        }
        else
        {
            self.releaseMapFullscreen = true;
        }

        // Fix zooming not working
        if (Main.mapFullscreen)
        {
            float num7 = PlayerInput.ScrollWheelDelta / 120;
            if (PlayerInput.UsingGamepad)
            {
                num7 += (PlayerInput.Triggers.Current.HotbarPlus.ToInt() - PlayerInput.Triggers.Current.HotbarMinus.ToInt()) * 0.1f;
            }
            Main.mapFullscreenScale *= 1f + num7 * 0.3f;
        }
    }
}
