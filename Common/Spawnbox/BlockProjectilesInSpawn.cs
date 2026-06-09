using Microsoft.Xna.Framework;
using PvPAdventure.Core.Utilities;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Spawnbox;

internal static class SpawnboxBarrier
{
    internal static bool TouchesSpawnbox(Rectangle hitbox)
    {
        Rectangle tiles = hitbox.ToTileRectangle();
        return ModContent.GetInstance<RegionManager>().Regions
            .Any(region => !region.CanModifyTiles && region.Area.Intersects(tiles));
    }
}

internal class BlockProjectilesInSpawn : GlobalProjectile
{
    public override bool PreAI(Projectile projectile)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || !SpawnboxBarrier.TouchesSpawnbox(projectile.Hitbox))
            return true;

        int identity = projectile.identity;
        int owner = projectile.owner;
        projectile.Kill();

        if (Main.netMode == NetmodeID.Server)
            NetMessage.SendData(MessageID.KillProjectile, -1, -1, null, identity, owner);

        return false;
    }

    public override bool? CanCutTiles(Projectile projectile) =>
        SpawnboxBarrier.TouchesSpawnbox(projectile.Hitbox) ? false : null;
}

internal class BlockItemsInSpawn : GlobalItem
{
    public override bool CanUseItem(Item item, Player player) =>
        item.shoot <= ProjectileID.None || !SpawnboxBarrier.TouchesSpawnbox(player.Hitbox);

    public override void Update(Item item, ref float gravity, ref float maxFallSpeed)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || !SpawnboxBarrier.TouchesSpawnbox(item.Hitbox))
            return;

        item.TurnToAir();
        item.active = false;

        if (Main.netMode == NetmodeID.Server)
            NetMessage.SendData(MessageID.SyncItem, -1, -1, null, item.whoAmI);
    }
}

internal class BlockLiquidsInSpawn : ModSystem
{
    public override void PostUpdateWorld()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        foreach (var region in ModContent.GetInstance<RegionManager>().Regions.Where(r => !r.CanModifyTiles))
            ClearLiquids(region.Area);
    }

    private static void ClearLiquids(Rectangle area)
    {
        for (int x = area.Left; x < area.Right; x++)
            for (int y = area.Top; y < area.Bottom; y++)
                if (WorldGen.InWorld(x, y) && Main.tile[x, y].LiquidAmount > 0)
                {
                    Main.tile[x, y].LiquidAmount = 0;

                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.sendWater(x, y);
                }
    }
}
