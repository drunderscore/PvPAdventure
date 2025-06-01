using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using PvPAdventure.System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Mono.Cecil.Cil;

namespace PvPAdventure;

public class AdventureProjectile : GlobalProjectile
{
    private IEntitySource _entitySource;
    public override bool InstancePerEntity => true;

    public override void Load()
    {
        On_PlayerDeathReason.ByProjectile += OnPlayerDeathReasonByProjectile;
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

    public class SpectreHoodPvPFix : GlobalProjectile
    {
        private static ILHook _ghostHealHook;

        public override void Load()
        {
            base.Load();

            // Hook into the ghostHeal method
            MethodInfo ghostHealMethod = typeof(Projectile)
                .GetMethod("ghostHeal", BindingFlags.Instance | BindingFlags.Public);

            if (ghostHealMethod != null)
            {
                _ghostHealHook = new ILHook(ghostHealMethod, ModifyGhostHeal);
                Mod.Logger.Info("Successfully hooked into ghostHeal method!");
            }
            else
            {
                Mod.Logger.Error("Failed to find ghostHeal method!");
            }
        }

        public override void Unload()
        {
            base.Unload();
            _ghostHealHook?.Dispose();
            Mod.Logger.Info("Unloaded Spectre Hood PvP fix.");
        }

        private void ModifyGhostHeal(ILContext il)
        {
            var c = new ILCursor(il);

            try
            {
                // Find the condition that checks for hostile players and team matching
                // We're looking for the pattern that checks: 
                // (!Main.player[this.owner].hostile && !Main.player[i].hostile) || Main.player[this.owner].team == Main.player[i].team

                // Look for the first ldsfld Main.player instruction in the loop
                if (c.TryGotoNext(
                    x => x.MatchLdsfld(typeof(Main), "player"),
                    x => x.MatchLdarg(0), // this
                    x => x.MatchLdfld<Projectile>("owner")))
                {
                    // Move forward to find the hostile check
                    if (c.TryGotoNext(
                        x => x.MatchLdfld<Player>("hostile")))
                    {
                        // We found the hostile field access
                        // Now we need to find the branch instruction that uses this condition

                        // Continue searching for the branch instruction
                        if (c.TryGotoNext(x => x.MatchBrfalse(out _)))
                        {
                            // Replace the branch condition with a simple check
                            // We want to always enter the healing logic regardless of hostility

                            // Clear the stack by popping the boolean result
                            c.Emit(OpCodes.Pop);
                            // Push true (1) to always pass the condition
                            c.Emit(OpCodes.Ldc_I4_1);

                            Mod.Logger.Info("Successfully modified ghostHeal PvP condition!");
                        }
                        else
                        {
                            Mod.Logger.Error("Could not find branch instruction in ghostHeal!");
                        }
                    }
                    else
                    {
                        Mod.Logger.Error("Could not find hostile field access in ghostHeal!");
                    }
                }
                else
                {
                    Mod.Logger.Error("Could not find player array access in ghostHeal!");
                }
            }
            catch (Exception ex)
            {
                Mod.Logger.Error($"Error modifying ghostHeal: {ex.Message}");

                // Fallback: Replace the entire method with a custom implementation
                c.Index = 0;
                c.RemoveRange(c.Instrs.Count);

                // Custom ghostHeal implementation that works with PvP
                c.EmitDelegate<Action<Projectile, int, Vector2, Entity>>((proj, dmg, position, victim) =>
                {
                    CustomGhostHeal(proj, dmg, position, victim);
                });
                c.Emit(OpCodes.Ret);

                Mod.Logger.Info("Used fallback custom ghostHeal implementation!");
            }
        }

        // Custom implementation of ghostHeal that works with PvP
        private static void CustomGhostHeal(Projectile proj, int dmg, Vector2 position, Entity victim)
        {
            float healMultiplier = 0.2f; // Increased from 0.2f to test IL edit is working
            healMultiplier -= proj.numHits * 0.05f;

            if (healMultiplier <= 0f)
                return;

            // Enhanced healing for PvP damage
            bool isPvPDamage = victim is Player;
            if (isPvPDamage)
            {
                healMultiplier *= 4f; // 300% more healing for PvP damage
            }

            float healAmount = dmg * healMultiplier;
            if ((int)healAmount <= 0)
                return;

            if (Main.player[Main.myPlayer].lifeSteal <= 0f)
                return;

            Main.player[Main.myPlayer].lifeSteal -= healAmount;

            if (!proj.DamageType.CountsAsClass(DamageClass.Magic))
                return;

            float maxMissingHealth = 0f;
            int targetPlayer = proj.owner;

            // Modified loop - works for both PvE and PvP
            for (int i = 0; i < 255; i++)
            {
                if (Main.player[i].active && !Main.player[i].dead)
                {
                    // Check if player is within range
                    if (proj.Distance(Main.player[i].Center) <= 3000f)
                    {
                        bool shouldHeal = false;

                        // Original condition: teammates or both non-hostile
                        if ((!Main.player[proj.owner].hostile && !Main.player[i].hostile) ||
                            Main.player[proj.owner].team == Main.player[i].team)
                        {
                            shouldHeal = true;
                        }
                        // NEW: Also heal if the victim is a player (PvP scenario)
                        else if (victim is Player)
                        {
                            shouldHeal = true;
                        }

                        if (shouldHeal)
                        {
                            int missingHealth = Main.player[i].statLifeMax2 - Main.player[i].statLife;
                            if (missingHealth > maxMissingHealth)
                            {
                                maxMissingHealth = missingHealth;
                                targetPlayer = i;
                            }
                        }
                    }
                }
            }

            // Spawn the healing projectile
            Projectile.NewProjectile(
                proj.GetSource_OnHit(victim),
                position.X, position.Y,
                0f, 0f,
                298, // Healing projectile ID
                0, 0f,
                proj.owner,
                targetPlayer,
                healAmount,
                0f
            );
        }
    }
}