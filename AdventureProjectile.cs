using Microsoft.Xna.Framework;
using PvPAdventure.System;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure;

public class AdventureProjectile : GlobalProjectile
{
    private IEntitySource _entitySource;
    public override bool InstancePerEntity => true;

    public override void Load()
    {
        On_PlayerDeathReason.ByProjectile += OnPlayerDeathReasonByProjectile;

        // Adapt Spectre Hood set bonus "Ghost Heal" to be better suited for PvP.
        On_Projectile.ghostHeal += OnProjectileghostHeal;
    }

    private static EntitySource_ItemUse GetItemUseSource(Projectile projectile, Projectile lastProjectile)
    {
        var adventureProjectile = projectile.GetGlobalProjectile<AdventureProjectile>();

        if (adventureProjectile._entitySource is EntitySource_ItemUse entitySourceItemUse)
            return entitySourceItemUse;

        if (adventureProjectile._entitySource is EntitySource_Parent entitySourceParent &&
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
        _entitySource = source;
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

    public class ClingerStaffGlobalProjectile : GlobalProjectile
    {
        public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
        {
            return entity.type == ProjectileID.ClingerStaff;
        }

        public override void PostAI(Projectile projectile)
        {
            Player owner = Main.player[projectile.owner];

            // Check inventory (including equipped items) AND mouse slot
            bool hasStaff = owner.HasItem(ItemID.ClingerStaff);
            bool mouseHasStaff = owner.inventory[58].type == ItemID.ClingerStaff && owner.inventory[58].stack > 0;

            if (!hasStaff && !mouseHasStaff)
            {
                projectile.Kill();
            }
        }
    }

    public class SpiderStaffGlobalProjectile : GlobalProjectile
    {
        public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
        {
            return entity.type == ProjectileID.VenomSpider ||
                   entity.type == ProjectileID.JumperSpider || // Note: Fix typo here
                   entity.type == ProjectileID.DangerousSpider;
        }
        public override void PostAI(Projectile projectile)
        {
            Player owner = Main.player[projectile.owner];

            // Check inventory (including equipped items) AND mouse slot
            bool hasStaff = owner.HasItem(ItemID.SpiderStaff);
            bool mouseHasStaff = owner.inventory[58].type == ItemID.SpiderStaff && owner.inventory[58].stack > 0;


            if (!hasStaff && !mouseHasStaff)
            {
                projectile.Kill();
            }
        }



        public class QueenSpiderStaffGlobalProjectile : GlobalProjectile
        {
            public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
            {
                return entity.type == ProjectileID.SpiderHiver;
            }

            public override void PostAI(Projectile projectile)
            {
                Player owner = Main.player[projectile.owner];

                // Check inventory (including equipped items) AND mouse slot
                bool hasStaff = owner.HasItem(ItemID.QueenSpiderStaff);
                bool mouseHasStaff = owner.inventory[58].type == ItemID.QueenSpiderStaff && owner.inventory[58].stack > 0;

                if (!hasStaff && !mouseHasStaff)
                {
                    projectile.Kill();
                }
            }
        }

        public class NimbusRodGlobalProjectile : GlobalProjectile
        {
            public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
            {
                return entity.type == ProjectileID.RainNimbus;
            }

            public override void PostAI(Projectile projectile)
            {
                Player owner = Main.player[projectile.owner];

                // Check inventory (including equipped items) AND mouse slot
                bool hasStaff = owner.HasItem(ItemID.NimbusRod);
                bool mouseHasStaff = owner.inventory[58].type == ItemID.NimbusRod && owner.inventory[58].stack > 0;

                if (!hasStaff && !mouseHasStaff)
                {
                    projectile.Kill();
                }
            }
        }

        public class XenoStaffGlobalProjectile : GlobalProjectile
        {
            public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
            {
                return entity.type == ProjectileID.UFOMinion;
            }

            public override void PostAI(Projectile projectile)
            {
                Player owner = Main.player[projectile.owner];

                // Check inventory (including equipped items) AND mouse slot
                bool hasStaff = owner.HasItem(ItemID.XenoStaff);
                bool mouseHasStaff = owner.inventory[58].type == ItemID.XenoStaff && owner.inventory[58].stack > 0;

                if (!hasStaff && !mouseHasStaff)
                {
                    projectile.Kill();
                }
            }
        }

        public class BladeStaffGlobalProjectile : GlobalProjectile
        {
            public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
            {
                return entity.type == ProjectileID.Smolstar;
            }

            public override void PostAI(Projectile projectile)
            {
                Player owner = Main.player[projectile.owner];

                // Check inventory (including equipped items) AND mouse slot
                bool hasStaff = owner.HasItem(ItemID.Smolstar);
                bool mouseHasStaff = owner.inventory[58].type == ItemID.Smolstar && owner.inventory[58].stack > 0;

                if (!hasStaff && !mouseHasStaff)
                {
                    projectile.Kill();
                }
            }
        }

        public class HornetStaffGlobalProjectile : GlobalProjectile
        {
            public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
            {
                return entity.type == ProjectileID.Hornet;
            }

            public override void PostAI(Projectile projectile)
            {
                Player owner = Main.player[projectile.owner];

                // Check inventory (including equipped items) AND mouse slot
                bool hasStaff = owner.HasItem(ItemID.HornetStaff);
                bool mouseHasStaff = owner.inventory[58].type == ItemID.HornetStaff && owner.inventory[58].stack > 0;

                if (!hasStaff && !mouseHasStaff)
                {
                    projectile.Kill();
                }
            }
        }

        public class ImpStaffGlobalProjectile : GlobalProjectile
        {
            public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
            {
                return entity.type == ProjectileID.FlyingImp;
            }

            public override void PostAI(Projectile projectile)
            {
                Player owner = Main.player[projectile.owner];

                // Check inventory (including equipped items) AND mouse slot
                bool hasStaff = owner.HasItem(ItemID.ImpStaff);
                bool mouseHasStaff = owner.inventory[58].type == ItemID.ImpStaff && owner.inventory[58].stack > 0;

                if (!hasStaff && !mouseHasStaff)
                {
                    projectile.Kill();
                }
            }
        }

        public class PygmyStaffGlobalProjectile : GlobalProjectile
        {
            public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
            {
                return entity.type == ProjectileID.Pygmy ||
                       entity.type == ProjectileID.Pygmy2 ||
                       entity.type == ProjectileID.Pygmy3 ||
                       entity.type == ProjectileID.Pygmy4;
            }

            public override void PostAI(Projectile projectile)
            {
                Player owner = Main.player[projectile.owner];

                // Check inventory (including equipped items) AND mouse slot
                bool hasStaff = owner.HasItem(ItemID.PygmyStaff);
                bool mouseHasStaff = owner.inventory[58].type == ItemID.PygmyStaff && owner.inventory[58].stack > 0;

                if (!hasStaff && !mouseHasStaff)
                {
                    projectile.Kill();
                }
            }
        }

        public class DeadlySphereGlobalProjectile : GlobalProjectile
        {
            public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
            {
                return entity.type == ProjectileID.DeadlySphere;
            }

            public override void PostAI(Projectile projectile)
            {
                Player owner = Main.player[projectile.owner];

                // Check inventory (including equipped items) AND mouse slot
                bool hasStaff = owner.HasItem(ItemID.DeadlySphereStaff);
                bool mouseHasStaff = owner.inventory[58].type == ItemID.DeadlySphereStaff && owner.inventory[58].stack > 0;

                if (!hasStaff && !mouseHasStaff)
                {
                    projectile.Kill();
                }
            }
        }

        public class PirateStaffGlobalProjectile : GlobalProjectile
        {
            public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
            {
                return entity.type == ProjectileID.OneEyedPirate ||
                       entity.type == ProjectileID.SoulscourgePirate ||
                       entity.type == ProjectileID.PirateCaptain;
            }

            public override void PostAI(Projectile projectile)
            {
                Player owner = Main.player[projectile.owner];

                // Check inventory (including equipped items) AND mouse slot
                bool hasStaff = owner.HasItem(ItemID.PirateStaff);
                bool mouseHasStaff = owner.inventory[58].type == ItemID.PirateStaff && owner.inventory[58].stack > 0;

                if (!hasStaff && !mouseHasStaff)
                {
                    projectile.Kill();
                }
            }
        }

        public class TempestStaffGlobalProjectile : GlobalProjectile
        {
            public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
            {
                return entity.type == ProjectileID.Tempest;
            }

            public override void PostAI(Projectile projectile)
            {
                Player owner = Main.player[projectile.owner];

                // Check inventory (including equipped items) AND mouse slot
                bool hasStaff = owner.HasItem(ItemID.TempestStaff);
                bool mouseHasStaff = owner.inventory[58].type == ItemID.TempestStaff && owner.inventory[58].stack > 0;

                if (!hasStaff && !mouseHasStaff)
                {
                    projectile.Kill();
                }
            }
        }

        public class TerraprismaGlobalProjectile : GlobalProjectile
        {
            public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
            {
                return entity.type == ProjectileID.EmpressBlade;
            }

            public override void PostAI(Projectile projectile)
            {
                Player owner = Main.player[projectile.owner];

                // Check inventory (including equipped items) AND mouse slot
                bool hasStaff = owner.HasItem(ItemID.EmpressBlade);
                bool mouseHasStaff = owner.inventory[58].type == ItemID.EmpressBlade && owner.inventory[58].stack > 0;

                if (!hasStaff && !mouseHasStaff)
                {
                    projectile.Kill();
                }
            }
        }

        public class DeadProjectileList : GlobalProjectile
        {
            public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
            {
                return entity.type == ProjectileID.ClingerStaff ||
                       entity.type == ProjectileID.SporeTrap ||
                       entity.type == ProjectileID.SporeTrap2 ||
                       entity.type == ProjectileID.SporeGas ||
                       entity.type == ProjectileID.SporeGas2 ||
                       entity.type == ProjectileID.SporeGas3;
            }

            public override void PostAI(Projectile projectile)
            {
                // Ensure owner index is valid
                if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
                    return;

                Player owner = Main.player[projectile.owner];

                // Kill projectile if owner is dead or inactive
                if (owner.dead || !owner.active)
                {
                    projectile.Kill();
                }
            }
        }
    }
}

