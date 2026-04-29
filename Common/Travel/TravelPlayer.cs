using Microsoft.Xna.Framework;
using PvPAdventure.Common.GameTimer;
using PvPAdventure.Common.Travel.Portals;
using PvPAdventure.Content.Portals;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Travel;

internal class TravelPlayer : ModPlayer
{
    public override void OnHurt(Player.HurtInfo info)
    {
        if (Player.whoAmI != Main.myPlayer)
            return;

        if (Player.itemTime <= 0 || Player.HeldItem?.ModItem is not PortalCreatorItem)
            return;

        PortalCreatorItem.ResetUseState(Player);
        PopupText.NewText(new AdvancedPopupRequest
        {
            Color = Color.Crimson,
            Text = Language.GetTextValue("Mods.PvPAdventure.PortalCreator.Cancelled"),
            Velocity = new Vector2(0f, -4f),
            DurationInFrames = 120
        }, Player.Top + new Vector2(0f, -4f));
    }

    public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
    {
        if (Player.whoAmI == Main.myPlayer)
            TravelTeleportSystem.ClearSelection();

        if (Main.netMode != NetmodeID.MultiplayerClient)
            PortalSystem.ClearPortal(Player.whoAmI);
    }

    public override void UpdateDead()
    {
        if (Player.whoAmI != Main.myPlayer)
        {
            base.UpdateDead();
            return;
        }

        // 
        if (!TravelTeleportSystem.ShouldUseDeathTravelSelection(Player))
        {
            base.UpdateDead();
            return;
        }

        if (Player.respawnTimer > 2)
        {
            base.UpdateDead();
            return;
        }

        // Keep stuck until selection
        if (!TravelTeleportSystem.HasSelection)
        {
            Player.respawnTimer = 2;
            return;
        }

        base.UpdateDead();
    }

    // -- Beds -> TeamBedPlayer later

}