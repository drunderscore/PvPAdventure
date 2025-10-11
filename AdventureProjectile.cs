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
    public override bool InstancePerEntity => true;
    private IEntitySource _entitySource;

    public override void Load()
    {
        On_PlayerDeathReason.ByProjectile += OnPlayerDeathReasonByProjectile;

        // Adapt Spectre Hood set bonus "Ghost Heal" to be better suited for PvP.
        On_Projectile.ghostHeal += OnProjectileghostHeal;

        // Make Starlight only give 4-iframes (Projectile.playerImmune).
        IL_Projectile.Damage += EditProjectileDamage;

        // Add configurable distance for Ghost Heal when damaging NPCs.
        IL_Projectile.ghostHeal += EditProjectileghostHeal;

        // Track if the Friendly Shadowbeam Staff has bounced.
        IL_Projectile.HandleMovement += EditProjectileHandleMovement;

        // Track if the Light Disc has bounced.
        On_Projectile.LightDisc_Bounce += OnProjectileLightDisc_Bounce;
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
                ProjectileID.SpiritHeal,
                0,
                0f,
                self.owner,
                i,
                personalHeal
            );
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
                   entity.type == ProjectileID.RainCloudRaining ||
                   entity.type == ProjectileID.BloodCloudRaining ||
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
    public class AdventureNightglow : GlobalProjectile
    {
        public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) =>
            entity.type == ProjectileID.FairyQueenMagicItemShot;

        public override void SetDefaults(Projectile entity)
        {
            entity.localAI[0] = 0;
        }

        public override void AI(Projectile projectile)
        {
            if (projectile.localAI[0] <= 60)
            {
                projectile.localAI[0]++;
                return;
            }

            if (!projectile.TryGetOwner(out var owner))
                return;

            if (owner.whoAmI != Main.myPlayer)
                return;

            if (owner.itemAnimation > 0 && owner.HeldItem.type == ItemID.FairyQueenMagicItem)
            {
                var cursorPosition = Main.MouseWorld;
                var toCursor = cursorPosition - projectile.Center;

                var baseSpeed = 20.0f;
                var accelerationFactor = 1.5f;
                var turnStrength = 0.05f;

                var direction = toCursor.SafeNormalize(Vector2.Zero);
                var targetVelocity = direction * baseSpeed * accelerationFactor;

                projectile.velocity = Vector2.Lerp(projectile.velocity, targetVelocity, turnStrength);
                projectile.rotation = projectile.velocity.ToRotation() * MathHelper.PiOver2;
                projectile.netUpdate = true;
            }
        }
    }

    public class WhipRangeChanges : GlobalProjectile
    {
        public override bool AppliesToEntity(Projectile projectile, bool lateInstantiation)
        {
            return projectile.type == ProjectileID.MaceWhip ||
                   projectile.type == ProjectileID.RainbowWhip ||
                   projectile.type == ProjectileID.CoolWhip ||
                   projectile.type == ProjectileID.FireWhip ||
                   projectile.type == ProjectileID.SwordWhip ||
                   projectile.type == ProjectileID.ScytheWhip ||
                   projectile.type == ProjectileID.ThornWhip ||
                   projectile.type == ProjectileID.BoneWhip ||
                   projectile.type == ProjectileID.BlandWhip;
        }
        public override void SetDefaults(Projectile projectile)
        {
            if (projectile.type == ProjectileID.MaceWhip)
            {
                projectile.WhipSettings.RangeMultiplier = 1.03f; // this is like 20% range
            }
            if (projectile.type == ProjectileID.RainbowWhip)
            {
                projectile.WhipSettings.RangeMultiplier = 1.43f; // this is like 20% range
            }
            if (projectile.type == ProjectileID.CoolWhip)
            {
                projectile.WhipSettings.RangeMultiplier = 1.18f; // this is like 20% range
            }
            if (projectile.type == ProjectileID.FireWhip)
            {
                projectile.WhipSettings.RangeMultiplier = 1.18f; // this is like 20% range
            }
            if (projectile.type == ProjectileID.SwordWhip)
            {
                projectile.WhipSettings.RangeMultiplier = 1.4f; // this is like 20% range
            }
            if (projectile.type == ProjectileID.ScytheWhip)
            {
                projectile.WhipSettings.RangeMultiplier = 2f; // this is like 20% range
            }
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

    private void EditProjectileghostHeal(ILContext il)
    {
        var cursor = new ILCursor(il);

        // Find a call to Entity.Distance and a float constant load...
        cursor.GotoNext(i => i.MatchCall<Entity>("Distance") && i.Next.MatchLdcR4(out _));
        // ...to go back to the float constant load...
        cursor.Index += 1;

        // ...to remove it...
        cursor.Remove();

        // ...and emit our own delegate to return the value.
        cursor.EmitDelegate(() =>
        {
            var adventureConfig = ModContent.GetInstance<AdventureConfig>();
            return adventureConfig.Combat.GhostHealMaxDistanceNpc;
        });
    }

    public override void ModifyHitPlayer(Projectile projectile, Player target, ref Player.HurtModifiers modifiers)
    {
        // Replicate what vanilla does against NPCs for the Staff of Earth
        if (projectile.type == ProjectileID.BoulderStaffOfEarth && projectile.velocity.Length() < 3.5f)
        {
            modifiers.SourceDamage /= 2;
            modifiers.Knockback /= 2;
        }

        var adventureConfig = ModContent.GetInstance<AdventureConfig>();

        var bounced =
            projectile.type == ProjectileID.ShadowBeamFriendly && projectile.localAI[1] > 0
            || projectile.type == ProjectileID.LightDisc && projectile.localAI[0] > 0;

        if (bounced)
            modifiers.SourceDamage *= adventureConfig.Combat.ProjectileCollisionDamageReduction;

        if (adventureConfig.Combat.NoLineOfSightDamageReduction.TryGetValue(new(projectile.type),
                out var damageReduction) && projectile.TryGetOwner(out var owner) && !Collision.CanHit(owner, target))
            modifiers.SourceDamage *= damageReduction;
    }

    private void EditProjectileHandleMovement(ILContext il)
    {
        var cursor = new ILCursor(il);

        // Make sure when we emit we are put inside the respective label.
        cursor.MoveAfterLabels();

        // First, find a load to Projectile.type and a constant load to the Friendly Shadowbeam Staff projectile ID...
        cursor.GotoNext(i => i.MatchLdfld<Projectile>("type") && i.Next.MatchLdcI4(ProjectileID.ShadowBeamFriendly));

        // ...then find a load to Vector.X, an add instruction, and a store instruction...
        cursor.GotoNext(i => i.MatchLdfld<Vector2>("X") && i.Next.MatchAdd() && i.Next.Next.MatchStindR4());

        // ...and go forward to the store instruction...
        cursor.Index += 3;
        // ...to prepare a delegate call...
        cursor.EmitLdarg0();
        cursor.EmitDelegate(DidBounce);

        // ...then find a load to Vector.Y, an add instruction, and a store instruction...
        cursor.GotoNext(i => i.MatchLdfld<Vector2>("Y") && i.Next.MatchAdd() && i.Next.Next.MatchStindR4());

        // ...and go forward to the store instruction...
        cursor.Index += 3;
        // ...to prepare a delegate call...
        cursor.EmitLdarg0();
        cursor.EmitDelegate(DidBounce);

        return;

        void DidBounce(Projectile self)
        {
            self.localAI[1] = 1;
        }
    }

    private void OnProjectileLightDisc_Bounce(On_Projectile.orig_LightDisc_Bounce orig, Projectile self,
        Vector2 hitPoint, Vector2 normal)
    {
        self.localAI[0] = 1;
        orig(self, hitPoint, normal);
    }

    public class WhipBuffs : GlobalProjectile
    {
        public override void ModifyHitPlayer(Projectile projectile, Player target, ref Player.HurtModifiers modifiers)
        {
            if (projectile.type == ProjectileID.SwordWhip && target.hostile)
            {
                if (projectile.owner >= 0 && projectile.owner < Main.maxPlayers)
                {
                    Player attacker = Main.player[projectile.owner];
                    if (attacker != null && attacker.active && attacker != target && attacker.hostile)
                    {
                        int buffDuration = 420;
                        attacker.AddBuff(BuffID.SwordWhipPlayerBuff, buffDuration);
                    }
                }
            }

            if (projectile.type == ProjectileID.ThornWhip && target.hostile)
            {
                if (projectile.owner >= 0 && projectile.owner < Main.maxPlayers)
                {
                    Player attacker = Main.player[projectile.owner];

                    if (attacker != null && attacker.active && attacker != target && attacker.hostile)
                    {
                        int buffDuration = 420;
                        attacker.AddBuff(BuffID.ThornWhipPlayerBuff, buffDuration);
                    }
                }
            }

            if (projectile.type == ProjectileID.ScytheWhip && target.hostile)
            {
                if (projectile.owner >= 0 && projectile.owner < Main.maxPlayers)
                {
                    Player attacker = Main.player[projectile.owner];

                    if (attacker != null && attacker.active && attacker != target && attacker.hostile)
                    {
                        int buffDuration = 420;
                        attacker.AddBuff(BuffID.ScytheWhipPlayerBuff, buffDuration);
                    }
                }
            }
        }
    }
}

