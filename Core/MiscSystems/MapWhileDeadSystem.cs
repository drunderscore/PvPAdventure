using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace PvPAdventure.Core.MiscSystems;

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

        // Allows you open the inventory
        // This code is copied from somewhere else in the Terraria src
        if (!Main.drawingPlayerChat && !Main.editSign && !Main.editChest && !Main.blockInput)
        {
            Main.player[Main.myPlayer].controlInv = PlayerInput.Triggers.Current.Inventory;
            if (Main.player[Main.myPlayer].controlInv)
            {
                if (Main.player[Main.myPlayer].releaseInventory)
                {
                    Main.player[Main.myPlayer].ToggleInv();
                }
                Main.player[Main.myPlayer].releaseInventory = false;
            }
            else
            {
                Main.player[Main.myPlayer].releaseInventory = true;
            }
        }

        // Allows you open the map
        // This code is copied from somewhere else in the Terraria src
        foreach (var key in Main.keyState.GetPressedKeys())
        {
            if (key.ToString() == Main.cMapFull)
            {
                if (self.releaseMapFullscreen)
                {
                    if (!Main.mapFullscreen)
                    {
                        Main.playerInventory = false;
                        Main.player[Main.myPlayer].talkNPC = -1;
                        Main.npcChatCornerItem = 0;
                        //Main.PlaySound(10, -1, -1, 1, 1f, 0f);
                        Main.mapFullscreenScale = 2.5f;
                        Main.mapFullscreen = true;
                        Main.resetMapFull = true;
                    }
                    else
                    {
                        //Main.PlaySound(10, -1, -1, 1, 1f, 0f);
                        Main.mapFullscreen = false;
                    }
                }
                self.releaseMapFullscreen = false;
            }
            else
            {
                self.releaseMapFullscreen = true;
            }
        }
        if (Main.keyState.GetPressedKeys().Length == 0)
        {
            self.releaseMapFullscreen = true;
        }

        // Fix zooming not working (also copied code)
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
