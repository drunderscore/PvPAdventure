using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Statistics;

internal class StatisticsProjectile : GlobalProjectile
{
    private Item sourceItem;

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
        // keep track of the item that came with our entity source.
        // if that source is our parent projectile, take its item.
        var item = source switch
        {
            EntitySource_ItemUse sourceItemUse => sourceItemUse.Item,
            EntitySource_Parent sourceParent when sourceParent.Entity is Projectile parentProjectile &&
                                                  parentProjectile.whoAmI != projectile.whoAmI => parentProjectile
                .GetGlobalProjectile<StatisticsProjectile>()
                .sourceItem,
            _ => null
        };

        projectile.GetGlobalProjectile<StatisticsProjectile>().sourceItem = item;
    }

    private PlayerDeathReason OnPlayerDeathReasonByProjectile(On_PlayerDeathReason.orig_ByProjectile orig,
        int playerindex, int projectileindex)
    {
        var self = orig(playerindex, projectileindex);

        self.SourceItem = Main.projectile[projectileindex].GetGlobalProjectile<StatisticsProjectile>().sourceItem;

        return self;
    }
}