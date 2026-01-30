using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Statistics;

internal class StatisticsProjectile : GlobalProjectile
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
        return GetItemUseSource(projectile, lastProjectile, 0, null);
    }

    private static EntitySource_ItemUse GetItemUseSource(
        Projectile projectile,
        Projectile lastProjectile,
        int depth,
        HashSet<int> seen)
    {
        if (projectile == null)
            return null;

        if (depth >= 64)
        {
            //Log.Chat($"[Killfeed] abort: depth cap hit at proj={projectile.whoAmI} type={projectile.type}");
            return null;
        }

        seen ??= new HashSet<int>();
        if (!seen.Add(projectile.whoAmI))
        {
            //Log.Chat($"[Killfeed] abort: cycle detected at proj={projectile.whoAmI} type={projectile.type}");
            return null;
        }

        var global = projectile.GetGlobalProjectile<StatisticsProjectile>();

        IEntitySource src = global.entitySource;
        string srcName = src == null ? "null" : src.GetType().Name;
        //Log.Chat($"[Killfeed] step depth={depth} proj={projectile.whoAmI} type={projectile.type} src={srcName}");

        if (src is EntitySource_ItemUse itemUse)
        {
            int itemType = itemUse.Item.type;
            string itemName = itemUse.Item.Name;
            //Log.Chat($"[Killfeed] resolved: item={itemType} ({itemName})");
            return itemUse;
        }

        if (src is EntitySource_Parent parent &&
            parent.Entity is Projectile parentProjectile &&
            parentProjectile != lastProjectile)
        {
            return GetItemUseSource(parentProjectile, projectile, depth + 1, seen);
        }

        //Log.Chat($"[Killfeed] stop: no parent / no itemuse at proj={projectile.whoAmI} type={projectile.type}");
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

        //Log.Chat($"[Killfeed] death: victim={playerIndex} proj={projectile.whoAmI} type={projectile.type} owner={projectile.owner}");

        var itemUse = GetItemUseSource(projectile, null);
        if (itemUse != null)
        {
            reason.SourceItem = itemUse.Item;
            //Log.Chat($"[Killfeed] applied SourceItem={itemUse.Item.type} ({itemUse.Item.Name})");
        }
        else
        {
            //Log.Chat("[Killfeed] no SourceItem resolved");
        }

        return reason;
    }

}
