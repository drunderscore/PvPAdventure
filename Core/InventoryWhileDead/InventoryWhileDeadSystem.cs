using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Core.InventoryWhileDead;

/// <summary>
/// Allows inventory access while dead.
/// </summary>
[Autoload(Side=ModSide.Client)]
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
        //orig(self);

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
}
