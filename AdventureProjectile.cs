using Microsoft.Xna.Framework;
using MonoMod.Cil;
using PvPAdventure.System;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure;

public class AdventureProjectile : GlobalProjectile
{
    public IEntitySource EntitySource { get; private set; }
    public override bool InstancePerEntity => true;

    public override void Load()
    {
        On_PlayerDeathReason.ByProjectile += OnPlayerDeathReasonByProjectile;

        // Adapt Spectre Hood set bonus "Ghost Heal" to be better suited for PvP.
        On_Projectile.ghostHeal += OnProjectileghostHeal;

        // Make Starlight only give 4-iframes (Projectile.playerImmune).
        IL_Projectile.Damage += EditProjectileDamage;
    }

    private static EntitySource_ItemUse GetItemUseSource(Projectile projectile, Projectile lastProjectile)
    {
        var adventureProjectile = projectile.GetGlobalProjectile<AdventureProjectile>();

        if (adventureProjectile.EntitySource is EntitySource_ItemUse entitySourceItemUse)
            return entitySourceItemUse;

        if (adventureProjectile.EntitySource is EntitySource_Parent entitySourceParent &&
            entitySourceParent.Entity is Projectile projectileParent && projectileParent != lastProjectile)
            return GetItemUseSource(projectileParent, projectile);

        return null;
    }

    private PlayerDeathReason OnPlayerDeathReasonByProjectile(On_PlayerDeathReason.orig_ByProjectile orig,
        int playerindex, int projectileindex)
    {
        var self = orig(playerindex, projectileindex);

        var projectile = Main.projectile[projectileindex];
        var entitySourceItemUse = GetItemUseSource(projectile, null);

        if (entitySourceItemUse != null)
            self.SourceItem = entitySourceItemUse.Item;

        return self;
    }

    public override void OnSpawn(Projectile projectile, IEntitySource source)
    {
        EntitySource = source;
    }

    public override bool? CanCutTiles(Projectile projectile)
    {
        if (projectile.owner == Main.myPlayer)
        {
            var region = ModContent.GetInstance<RegionManager>()
                .GetRegionIntersecting(projectile.Hitbox.ToTileRectangle());

            if (region != null && !region.CanModifyTiles)
                return false;
        }

        return null;
    }

    public override bool OnTileCollide(Projectile projectile, Vector2 oldVelocity)
    {
        if (projectile.type == ProjectileID.RainbowRodBullet)
            projectile.Kill();

        return true;
    }

    public override void SetDefaults(Projectile entity)
    {
        // All projectiles are important.
        entity.netImportant = true;
    }

    public override void PostAI(Projectile projectile)
    {
        // Ignore net spam restraints.
        projectile.netSpam = 0;
    }

    private void OnProjectileghostHeal(On_Projectile.orig_ghostHeal orig, Projectile self, int dmg, Vector2 position,
        Entity victim)
    {
        // Don't touch anything about the Ghost Heal outside PvP.
        if (victim is not Player)
        {
            orig(self, dmg, position, victim);
            return;
        }

        // This implementation differs from vanilla:
        //   - The None team isn't counted when looking for teammates.
        //     - Two players on the None team fighting would end up healing the person you attacked.
        //   - Player life steal is entirely disregarded.
        //   - All nearby teammates are healed, instead of only the one with the largest health deficit.

        var adventureConfig = ModContent.GetInstance<AdventureConfig>();

        var healMultiplier = adventureConfig.Combat.GhostHealMultiplier;
        healMultiplier -= self.numHits * 0.05f;
        if (healMultiplier <= 0f)
            return;

        var heal = dmg * healMultiplier;
        if ((int)heal <= 0)
            return;

        if (!self.CountsAsClass(DamageClass.Magic))
            return;

        var maxDistance = adventureConfig.Combat.GhostHealMaxDistance;
        for (var i = 0; i < Main.maxPlayers; i++)
        {
            var player = Main.player[i];

            if (!player.active || player.dead || !player.hostile)
                continue;

            if (player.team == (int)Team.None || player.team != Main.player[self.owner].team)
                continue;

            if (self.Distance(player.Center) > maxDistance)
                continue;

            var personalHeal = heal;
            if (player.ghostHeal)
                personalHeal *= adventureConfig.Combat.GhostHealMultiplierWearers;

            // FIXME: Can't set the context properly because of poor TML visibility to ProjectileSourceID.
            Projectile.NewProjectile(
                self.GetSource_OnHit(victim),
                position.X,
                position.Y,
                0f,
                0f,
                298,
                0,
                0f,
                self.owner,
                i,
                personalHeal
            );
        }
    }

    private void EditProjectileDamage(ILContext il)
    {
        var cursor = new ILCursor(il);

        // First, match Projectile.playerImmune that is sometime followed by 40...
        cursor.GotoNext(i => i.MatchLdfld<Projectile>("playerImmune") && i.Next.Next.MatchLdcI4(40));

        // ...and go to the load of a value...
        cursor.Index += 2;
        // ...to remove it...
        cursor.Remove()
            // ...and prepare a delegate call.
            .EmitLdarg0()
            .EmitDelegate((Projectile self) =>
            {
                if (self.type == ProjectileID.PiercingStarlight)
                    return 4;

                return 40;
            });
    }
}