using PvPAdventure.Common.Travel;
using PvPAdventure.Content.Portals;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Travel.Portals;

internal sealed class PortalPlayer : ModPlayer
{
    public override bool CanHitNPC(NPC target)
    {
        if (IsFriendlyPortalTarget(Player, target))
            return false;

        return true;
    }

    public override bool? CanHitNPCWithItem(Item item, NPC target)
    {
        if (IsFriendlyPortalTarget(Player, target))
            return false;

        return null;
    }

    public override bool? CanHitNPCWithProj(Projectile proj, NPC target)
    {
        if (IsFriendlyPortalTarget(Player, target))
            return false;

        return null;
    }

    public override void OnHurt(Player.HurtInfo info)
    {
        if (!info.PvP || !PortalSystem.IsCreatingPortal(Player))
            return;

        if (Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI != Main.myPlayer)
            return;

        PortalCreatorItem.ResetUseState(Player);

        if (Player.whoAmI == Main.myPlayer)
        {
            PortalCreatorItem.Warning(Player, "Mods.PvPAdventure.PortalCreator.Cancelled");
            TravelTeleportSystem.ClearSelection();
        }

        if (Main.netMode == NetmodeID.MultiplayerClient)
            PortalNetHandler.SendPortalCreationCancel();
        else
            PortalSystem.ClearCreationProjectiles(Player.whoAmI);
    }

    public override void Load()
    {
        On_Player.ApplyDamageToNPC += OnPlayerApplyDamageToNPC;
        On_Player.StrikeNPCDirect += OnPlayerStrikeNPCDirect;
    }

    public override void Unload()
    {
        On_Player.ApplyDamageToNPC -= OnPlayerApplyDamageToNPC;
        On_Player.StrikeNPCDirect -= OnPlayerStrikeNPCDirect;
    }

    private static void OnPlayerApplyDamageToNPC(
        On_Player.orig_ApplyDamageToNPC orig,
        Player self,
        NPC npc,
        int damage,
        float knockback,
        int direction,
        bool crit,
        DamageClass damageType,
        bool damageVariation)
    {
        if (IsFriendlyPortalTarget(self, npc))
            return;

        orig(self, npc, damage, knockback, direction, crit, damageType, damageVariation);
    }

    private static void OnPlayerStrikeNPCDirect(
        On_Player.orig_StrikeNPCDirect orig,
        Player self,
        NPC npc,
        NPC.HitInfo hit)
    {
        if (IsFriendlyPortalTarget(self, npc))
            return;

        orig(self, npc, hit);
    }

    private static bool IsFriendlyPortalTarget(Player player, NPC target)
    {
        return target?.ModNPC is PortalNPC portal && PortalSystem.IsFriendlyPortal(player, portal);
    }
}
