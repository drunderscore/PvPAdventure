using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Misc.DeadSystems;

/// <summary>
/// Allows inventory access while dead.
/// </summary>
[Autoload(Side = ModSide.Client)]
internal class InventoryWhileDeadSystem : ModSystem
{
    public override void Load()
    {
        On_Main.DrawInterface_26_InterfaceLogic3 += ModifyInterfaceLogic;
        On_Player.TryOpeningInGameOptionsBasedOnInput += ModifyIngameOptionsInput;
    }
    public override void Unload()
    {
        On_Main.DrawInterface_26_InterfaceLogic3 -= ModifyInterfaceLogic;
        On_Player.TryOpeningInGameOptionsBasedOnInput -= ModifyIngameOptionsInput;
    }

    private void ModifyIngameOptionsInput(On_Player.orig_TryOpeningInGameOptionsBasedOnInput orig, Player self)
    {
        // Reese / Ghost Spectating compatibility: prevent opening inventory when spectating
        if (self.ghost)
        {
            Log.Chat("close");
            CloseOwnInventory();
            self.releaseInventory = !self.controlInv;
            return;
        }

        // Override to allow inventory access while dead
        if (self.dead)
        {
            if (self.controlInv)
            {
                if (self.releaseInventory)
                {
                    self.releaseInventory = false;
                    self.ToggleInv(); // actually toggles the inventory
                }
            }
            else
            {
                self.releaseInventory = true;
            }

            return;
        }

        // Vanilla logic
        if (self.controlInv)
        {
            if (self.releaseInventory)
            {
                self.releaseInventory = false;
                if (Main.ingameOptionsWindow)
                {
                    IngameOptions.Close();
                }
                else
                {
                    IngameOptions.Open();
                }
            }
        }
        else
        {
            self.releaseInventory = true;
        }
    }

    private void ModifyInterfaceLogic(On_Main.orig_DrawInterface_26_InterfaceLogic3 orig)
    {
        bool flag = Main.playerInventory;
        if (Main.player[Main.myPlayer].dead)
        {
            //Main.playerInventory = false; // skip this
        }
        if (!Main.playerInventory)
        {
            Main.player[Main.myPlayer].chest = -1;
            Main.InGuideCraftMenu = false;
            Main.InReforgeMenu = false;
            if (flag)
            {
                Recipe.FindRecipes();
            }
        }
        Main.hoverItemName = "";
    }

    private static void CloseOwnInventory()
    {
        //Log.Chat("Closing ghost/spectator inventory");
        Main.playerInventory = false;
        Main.LocalPlayer.chest = -1;
        Main.InGuideCraftMenu = false;
        Main.InReforgeMenu = false;
    }
}
