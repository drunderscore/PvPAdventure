using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Statistics;

internal class ProjectileKillFeedSource : GlobalProjectile
{
    private IEntitySource entitySource;

    public override bool InstancePerEntity => true;

    public override void Load()
    {
        On_PlayerDeathReason.ByProjectile += OnPlayerDeathReasonByProjectile;
    }

    public override void Unload()
    {
        On_PlayerDeathReason.ByProjectile -= OnPlayerDeathReasonByProjectile;
    }

    public override void OnSpawn(Projectile projectile, IEntitySource source)
    {
        entitySource = source;
    }

    private static EntitySource_ItemUse GetItemUseSource(Projectile projectile, Projectile lastProjectile)
    {
        var global = projectile.GetGlobalProjectile<ProjectileKillFeedSource>();

        if (global.entitySource is EntitySource_ItemUse itemUse)
            return itemUse;

        if (global.entitySource is EntitySource_Parent parent &&
            parent.Entity is Projectile parentProjectile &&
            parentProjectile != lastProjectile)
        {
            return GetItemUseSource(parentProjectile, projectile);
        }

        return null;
    }

    private static PlayerDeathReason OnPlayerDeathReasonByProjectile(
        On_PlayerDeathReason.orig_ByProjectile orig,
        int playerIndex,
        int projectileIndex)
    {
        var reason = orig(playerIndex, projectileIndex);

        if (projectileIndex < 0 || projectileIndex >= Main.maxProjectiles)
            return reason;

        var projectile = Main.projectile[projectileIndex];
        if (projectile == null || !projectile.active)
            return reason;

        var itemUse = GetItemUseSource(projectile, null);
        if (itemUse != null)
            reason.SourceItem = itemUse.Item;

        return reason;
    }
}
