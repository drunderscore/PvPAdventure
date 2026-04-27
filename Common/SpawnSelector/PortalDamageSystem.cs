using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.SpawnSelector;

internal sealed class PortalProjectileDamage : GlobalProjectile
{
    private readonly HashSet<int> hitPortalOwners = [];

    public override bool InstancePerEntity => true;

    public override void PostAI(Projectile projectile)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        if (!projectile.active || !projectile.friendly || projectile.hostile || projectile.damage <= 0)
            return;

        if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
            return;

        Player attacker = Main.player[projectile.owner];
        if (attacker == null || !attacker.active)
            return;

        PortalDamageHelper.TryDamageHitPortals(attacker, projectile.Hitbox, hitPortalOwners, projectile.damage, $"proj:{projectile.type}");
    }
}

internal sealed class PortalMeleeDamagePlayer : ModPlayer
{
    private readonly HashSet<int> hitPortalOwners = [];

    public override void PostUpdate()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        if (Player.itemAnimation <= 0)
        {
            hitPortalOwners.Clear();
            return;
        }

        Item item = Player.HeldItem;
        if (item == null || item.IsAir || item.damage <= 0 || item.noMelee)
            return;

        PortalDamageHelper.TryDamageHitPortals(Player, GetSwingHitbox(item), hitPortalOwners, item.damage, $"melee:{item.type}");
    }

    private Rectangle GetSwingHitbox(Item item)
    {
        int width = System.Math.Max(item.width, 44);
        int height = System.Math.Max(item.height, 44);
        Vector2 center = Player.Center + new Vector2(Player.direction * (Player.width * 0.5f + width * 0.35f), 0f);

        return new Rectangle((int)(center.X - width * 0.5f), (int)(center.Y - height * 0.5f), width, height);
    }
}

internal static class PortalDamageHelper
{
    public static void TryDamageHitPortals(Player attacker, Rectangle hitbox, HashSet<int> hitPortalOwners, int damage, string source)
    {
        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player owner = Main.player[i];

            if (owner == null || !owner.active)
                continue;

            // Skip self
#if !DEBUG
            if (i == attacker.whoAmI)
                continue;
#endif

            // Skip teammates
            if (attacker.team != 0 && attacker.team == owner.team)
                continue;

            if (SpawnPlayer.TryGetPortal(owner, out Vector2 pos, out _) &&
                hitbox.Intersects(PortalSystem.GetPortalHitbox(pos)) &&
                hitPortalOwners.Add(i))
            {
                PortalSystem.TryDamagePortal(attacker, i, damage, source);
            }
        }
    }
}
