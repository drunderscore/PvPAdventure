using Microsoft.Xna.Framework;
using PvPAdventure.Content.Items;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Misc.DeadSystems;

[Autoload(Side = ModSide.Client)]
internal sealed class MapZoomWhileUsingItemSystem : ModSystem
{
    public override void PostUpdateInput()
    {
        if (Main.dedServ)
            return;

        if (!Main.mapFullscreen)
            return;

        var player = Main.LocalPlayer;
        if (player == null || !player.active)
            return;

        // Don't interfere with text input / UI editing.
        if (Main.drawingPlayerChat || Main.editSign || Main.editChest || Main.blockInput)
            return;

        // Only override when the player is actively in an item-use state.
        // Mirror holds itemAnimation/itemTime; other items may set channel/controlUseItem.
        bool usingItem =
            player.itemAnimation > 0 ||
            player.itemTime > 0 ||
            player.channel ||
            player.controlUseItem;

        if (!usingItem)
            return;

        float delta = PlayerInput.ScrollWheelDelta / 120f;

        if (PlayerInput.UsingGamepad)
        {
            delta +=
                (PlayerInput.Triggers.Current.HotbarPlus.ToInt() -
                 PlayerInput.Triggers.Current.HotbarMinus.ToInt()) * 0.1f;
        }

        if (delta == 0f)
            return;

        float factor = 1f + delta * 0.3f;

        // Clamp to sane vanilla-like bounds. (APPROXIMATE.)
        // (I just ctrl+f'd mapfullscreenscale = )...
        Main.mapFullscreenScale = MathHelper.Clamp(Main.mapFullscreenScale * factor, 0.1f, 31.0f);
    }
}
