//using Terraria;
//using Terraria.DataStructures;
//using Terraria.ModLoader;

//namespace PvPAdventure.Common.Spectator.Trackers;

//internal sealed class SummonTrackerPlayer : ModPlayer
//{
//    public int LatestMinionIdentity { get; private set; } = -1;
//    public int LatestMinionType { get; private set; } = -1;
//    public int LatestSummonItemType { get; private set; } = -1;

//    public void TrackMinion(Projectile projectile, Item item)
//    {
//        if (projectile.owner != Player.whoAmI || item == null || item.IsAir)
//            return;

//        LatestMinionIdentity = projectile.identity;
//        LatestMinionType = projectile.type;
//        LatestSummonItemType = item.type;
//    }

//    public bool TryGetLatestSummonItem(out int itemType)
//    {
//        itemType = LatestSummonItemType;

//        if (itemType <= 0)
//            return false;

//        for (int i = 0; i < Main.maxProjectiles; i++)
//        {
//            Projectile projectile = Main.projectile[i];

//            if (!projectile.active ||
//                projectile.owner != Player.whoAmI ||
//                projectile.identity != LatestMinionIdentity ||
//                projectile.type != LatestMinionType ||
//                !projectile.minion)
//            {
//                continue;
//            }

//            return true;
//        }

//        itemType = -1;
//        return false;
//    }
//}

//internal sealed class SummonTrackerProjectile : GlobalProjectile
//{
//    public override void OnSpawn(Projectile projectile, IEntitySource source)
//    {
//        if (!projectile.minion || projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
//            return;

//        if (source is not EntitySource_ItemUse itemSource)
//            return;

//        Player owner = Main.player[projectile.owner];

//        if (owner?.active != true)
//            return;

//        owner.GetModPlayer<SummonTrackerPlayer>().TrackMinion(projectile, itemSource.Item);
//    }
//}