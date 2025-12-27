using MonoMod.Cil;
using System;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Core.MiscSystems;

/// <summary>
/// Inspired by BetterDeath mod to allow inventory, map, and settings access while dead.
/// Modified and ported by Erky to 1.4.4+
/// https://github.com/NotLe0n/BetterDeath/blob/master/Edits.cs#L23
/// </summary>
public class BetterDeathEdits : ModSystem
{
    public override void Load()
    {
        On_Player.UpdateDead += Player_UpdateDead;
        IL_Main.DrawInterface_26_InterfaceLogic3 += Main_DrawInterface_26_InterfaceLogic3;
        IL_Player.KillMe += Player_KillMe;
        IL_IngameOptions.Draw += IngameOptions_Draw;
        IL_IngameFancyUI.Draw += IngameFancyUI_Draw;
    }

    public override void Unload()
    {
        On_Player.UpdateDead -= Player_UpdateDead;
        IL_Main.DrawInterface_26_InterfaceLogic3 -= Main_DrawInterface_26_InterfaceLogic3;
        IL_Player.KillMe -= Player_KillMe;
        IL_IngameOptions.Draw -= IngameOptions_Draw;
        IL_IngameFancyUI.Draw -= IngameFancyUI_Draw;
    }

    /// <summary>
    /// fixes Map zoom, lets you open the inventory
    /// </summary>
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


    /// <summary>
    /// leaves your inventory open while you're dead
    /// </summary>
    private static void Main_DrawInterface_26_InterfaceLogic3(ILContext il)
    {
        var c = new ILCursor(il);

        //  GOAL: remove "if (Main.player[Main.myPlayer].dead) Main.playerInventory = false;"
        //  Position: IL_0006

        //          <--- here
        //  IL_0006: ldsfld    class Terraria.Player[] Terraria.Main::player
        //  IL_000B: ldsfld int32 Terraria.Main::myPlayer
        //  IL_0010: ldelem.ref
        //  IL_0011: ldfld     bool Terraria.Player::dead
        //  IL_0016: brfalse.s IL_001E
        //  IL_0018: ldc.i4.0
        //  IL_0019: stsfld    bool Terraria.Main::playerInventory

        if (!c.TryGotoNext(MoveType.Before,
            i => i.MatchLdsfld<Main>("player"),
            i => i.MatchLdsfld<Main>("myPlayer"),
            i => i.MatchLdelemRef(),
            i => i.MatchLdfld<Player>("dead"),
            i => i.MatchBrfalse(out _),
            i => i.MatchLdcI4(0),
            i => i.MatchStsfld<Main>("playerInventory")))
            return;

        c.RemoveRange(7);
    }

    /// <summary>
    /// leaves the map open when you die
    /// </summary>
    private static void Player_KillMe(ILContext il)
    {
        var c = new ILCursor(il);

        // GOAL: change "Main.mapFullscreen = false;" to "Main.mapFullscreen = Main.mapFullscreen;"
        // Position: Player.Killme, IL_0092

        //IL_0092: ldsfld int32 Terraria.Main::myPlayer
        //IL_0097: ldarg.0
        //IL_0098: ldfld int32 Terraria.Entity::whoAmI
        //IL_009D: bne.un.s IL_00A5
        //IL_009F: ldc.i4.0
        //      <--- here
        //IL_00A0: stsfld    bool Terraria.Main::mapFullscreen


        if (!c.TryGotoNext(MoveType.After,
            i => i.MatchLdsfld<Main>("myPlayer"),
            i => i.MatchLdarg(0),
            i => i.MatchLdfld<Entity>("whoAmI"),
            i => i.MatchBneUn(out _),
            i => i.MatchLdcI4(0)))
            return;

        c.EmitDelegate<Func<int, int>>((returnvalue) => Main.mapFullscreen ? 1 : 0);
    }

    /// <summary>
    /// allows you open the settings menu
    /// </summary>
    private static void IngameOptions_Draw(ILContext il)
    {
        var c = new ILCursor(il);

        // Position: IngameOptions.Draw, IL_0000
        // GOAL: remove these Lines of code:

        //  if (Main.player[Main.myPlayer].dead && !Main.player[Main.myPlayer].ghost)
        //  {
        //      Main.setKey = -1;
        //      IngameOptions.Close();
        //      Main.playerInventory = false;
        //      return;
        //  }


        if (!c.TryGotoNext(MoveType.Before,
            i => i.MatchLdsfld<Main>("player"),
            i => i.MatchLdsfld<Main>("myPlayer"),
            i => i.MatchLdelemRef(),
            i => i.MatchLdfld<Player>("dead"),
            i => i.MatchBrfalse(out _),
            i => i.MatchLdsfld<Main>("player"),
            i => i.MatchLdsfld<Main>("myPlayer"),
            i => i.MatchLdelemRef(),
            i => i.MatchLdfld<Player>("ghost"),
            i => i.MatchBrtrue(out _),
            i => i.MatchLdcI4(-1)))
            return;

        c.RemoveRange(16);
    }

    /// <summary>
    /// Prevents the Settings menu from closing when entering a submenu
    /// </summary>
    private static void IngameFancyUI_Draw(ILContext il)
    {
        var c = new ILCursor(il);

        // Goal: Prevent the Settings menu from closing when entering a submenu
        // Position: IngameFancyUI.Draw, IL_0000

        //          <--- here
        //  IL_0000: ldsfld    bool Terraria.Main::gameMenu
        //  IL_0005: brtrue.s IL_0038
        //  IL_0007: ldsfld    class Terraria.Player[] Terraria.Main::player
        //  IL_000C: ldsfld int32 Terraria.Main::myPlayer
        //  IL_0011: ldelem.ref
        //  IL_0012: ldfld     bool Terraria.Player::dead
        //  IL_0017: brfalse.s IL_0038
        //  IL_0019: ldsfld    class Terraria.Player[] Terraria.Main::player
        //  IL_001E: ldsfld int32 Terraria.Main::myPlayer
        //  IL_0023: ldelem.ref
        //  IL_0024: ldfld     bool Terraria.Player::ghost
        //  IL_0029: brtrue.s IL_0038

        if (!c.TryGotoNext(MoveType.Before,
            i => i.MatchLdsfld<Main>("gameMenu"),
            i => i.MatchBrtrue(out _),
            i => i.MatchLdsfld<Main>("player"),
            i => i.MatchLdsfld<Main>("myPlayer"),
            i => i.MatchLdelemRef(),
            i => i.MatchLdfld<Player>("dead"),
            i => i.MatchBrfalse(out _),
            i => i.MatchLdsfld<Main>("player"),
            i => i.MatchLdsfld<Main>("myPlayer"),
            i => i.MatchLdelemRef(),
            i => i.MatchLdfld<Player>("ghost"),
            i => i.MatchBrtrue(out _)))
            return;

        c.RemoveRange(17);
    }
}
