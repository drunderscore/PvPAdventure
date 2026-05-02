using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PvPAdventure.Common.Spectator;
using PvPAdventure.Common.Spectator.SpectatorMode;
using PvPAdventure.Common.TeammateSpectator.TeammateOverlay;
using PvPAdventure.Core.Utilities;
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
        // Spectator special case
        if (SpectatorModeSystem.IsInSpectateMode(Main.LocalPlayer) || self.ghost)
        {
            CloseOwnInventory();
            if (!Main.ingameOptionsWindow)
            {
                return;
            }
        }

        // Press escape special case
        if (KeyboardHelper.Pressed(Keys.Escape))
        {
            Player target = SpectatorTargetSystem.GetPlayerTarget();

            if (target?.active == true)
            {
                DebugLog.Chat("Toggle spectated player's inventory for " + target.name);
                PlayerHudOverlay.Toggle(target.whoAmI);
            }
            else
            {
                // TODO
                // This never reaches because it's handled in PlayerHudOverlay.Update.
                // ...So this is redundant, but keep it just in-case.
                //Main.NewText("Inventory is disabled as a spectator unless you are spectating another player.", Color.Yellow);
            }

            return;
        }

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
        if (SpectatorModeSystem.IsInSpectateMode(Main.LocalPlayer) || Main.LocalPlayer.ghost)
        {
            CloseOwnInventory();
            return;
        }

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
