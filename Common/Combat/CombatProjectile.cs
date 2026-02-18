using Microsoft.Xna.Framework;
using MonoMod.Cil;
using PvPAdventure.Common.Spawnbox;
using PvPAdventure.Core.Config;
using PvPAdventure.Core.Utilities;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Combat;

public class CombatProjectile : GlobalProjectile
{
    public override void Load()
    {
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

        var adventureConfig = ModContent.GetInstance<ServerConfig>();

        var healMultiplier = adventureConfig.Other.SpectreHealing.PvPHealMultiplier;
        healMultiplier -= self.numHits * 0.05f;
        if (healMultiplier <= 0f)
            return;

        var heal = dmg * healMultiplier;
        if ((int)heal <= 0)
            return;

        if (!self.CountsAsClass(DamageClass.Magic))
            return;

        var maxDistance = adventureConfig.Other.SpectreHealing.PvPHealRange;
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
                personalHeal *= adventureConfig.Other.SpectreHealing.PvPSelfHealMultiplier;

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
                return self.type switch
                {
                    ProjectileID.PiercingStarlight => 4,
                    ProjectileID.NettleBurstLeft => 15,
                    ProjectileID.NettleBurstRight => 15,
                    ProjectileID.NettleBurstEnd => 15,
                    ProjectileID.CrystalVileShardHead => 15,
                    ProjectileID.CrystalVileShardShaft => 15,
                    ProjectileID.VilethornTip => 15,
                    ProjectileID.VilethornBase => 15,
                    ProjectileID.InfernoFriendlyBlast => 10,
                    ProjectileID.RainbowRodBullet => 12,
                    ProjectileID.Electrosphere => 8,
                    ProjectileID.WoodYoyo => 10,
                    ProjectileID.CorruptYoyo => 10,
                    ProjectileID.CrimsonYoyo => 10,
                    ProjectileID.JungleYoyo => 10,
                    ProjectileID.RedsYoyo => 10,
                    ProjectileID.ValkyrieYoyo => 10,
                    ProjectileID.HiveFive => 10,
                    ProjectileID.Cascade => 10,
                    ProjectileID.Yelets => 10,
                    ProjectileID.Code1 => 10,
                    ProjectileID.Code2 => 10,
                    ProjectileID.Rally => 10,
                    ProjectileID.Valor => 10,
                    ProjectileID.Chik => 10,
                    ProjectileID.FormatC => 10,
                    ProjectileID.HelFire => 10,
                    ProjectileID.Amarok => 10,
                    ProjectileID.Gradient => 10,
                    ProjectileID.Kraken => 10,
                    ProjectileID.TheEyeOfCthulhu => 10,
                    ProjectileID.DeathSickle => 10,
                    ProjectileID.Trident => 15,
                    ProjectileID.AdamantiteGlaive => 15,
                    ProjectileID.CobaltNaginata => 15,
                    ProjectileID.DarkLance => 15,
                    ProjectileID.MonkStaffT2 => 15,
                    ProjectileID.Gungnir => 15,
                    ProjectileID.MushroomSpear => 15,
                    ProjectileID.MythrilHalberd => 15,
                    ProjectileID.OrichalcumHalberd => 15,
                    ProjectileID.NorthPoleSpear => 15,
                    ProjectileID.PalladiumPike => 15,
                    ProjectileID.ObsidianSwordfish => 15,
                    ProjectileID.Spear => 15,
                    ProjectileID.ThunderSpear => 15,
                    ProjectileID.Swordfish => 15,
                    ProjectileID.TheRottedFork => 15,
                    ProjectileID.TitaniumTrident => 15,
                    ProjectileID.EnchantedBoomerang => 10,
                    ProjectileID.Flamarang => 10,
                    ProjectileID.WoodenBoomerang => 10,
                    ProjectileID.Trimarang => 10,
                    ProjectileID.ThornChakram => 10,
                    ProjectileID.BloodyMachete => 10,
                    ProjectileID.Shroomerang => 10,
                    ProjectileID.IceBoomerang => 10,
                    ProjectileID.CombatWrench => 10,
                    ProjectileID.FlyingKnife => 10,
                    ProjectileID.BouncingShield => 10,
                    ProjectileID.LightDisc => 10,
                    ProjectileID.Bananarang => 10,
                    ProjectileID.PaladinsHammerFriendly => 10,
                    ProjectileID.PossessedHatchet => 10,
                    ProjectileID.Mace => 10,
                    ProjectileID.FlamingMace => 10,
                    ProjectileID.BallOHurt => 10,
                    ProjectileID.TheMeatball => 10,
                    ProjectileID.BlueMoon => 10,
                    ProjectileID.Sunfury => 10,
                    ProjectileID.DripplerFlail => 10,
                    ProjectileID.TheDaoofPow => 10,
                    ProjectileID.FlowerPow => 10,
                    ProjectileID.Flairon => 10,
                    ProjectileID.ShadowJoustingLance => 10,
                    ProjectileID.JoustingLance => 10,
                    ProjectileID.HallowJoustingLance => 10,
                    ProjectileID.MolotovFire => 10,
                    ProjectileID.MolotovFire2 => 10,
                    ProjectileID.MolotovFire3 => 10,
                    ProjectileID.WeatherPainShot => 30,
                    ProjectileID.RainbowBack => 10,
                    ProjectileID.RainbowFront => 10,
                    ProjectileID.DemonScythe => 10,
                    ProjectileID.BookOfSkullsSkull => 10,
                    ProjectileID.WaterBolt => 10,
                    ProjectileID.CursedFlameFriendly => 15,
                    ProjectileID.ChargedBlasterLaser => 10,
                    ProjectileID.ClingerStaff => 10,
                    ProjectileID.EighthNote => 10,
                    ProjectileID.TiedEighthNote => 10,
                    ProjectileID.QuarterNote => 10,
                    ProjectileID.Flamelash => 15,
                    ProjectileID.FairyQueenMagicItemShot => 20,
                    ProjectileID.ToxicCloud => 10,
                    ProjectileID.ToxicCloud2 => 10,
                    ProjectileID.ToxicCloud3 => 10,
                    ProjectileID.SporeCloud => 10,
                    ProjectileID.SporeGas => 10,
                    ProjectileID.SporeGas2 => 10,
                    ProjectileID.SporeGas3 => 10,
                    ProjectileID.SporeTrap => 10,
                    ProjectileID.SporeTrap2 => 10,

                    _ => 40  // Default for everything else
                };
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
            var adventureConfig = ModContent.GetInstance<ServerConfig>();
            return adventureConfig.Other.SpectreHealing.PvEHealRange;
        });
    }

    public override void ModifyHitPlayer(Projectile projectile, Player target, ref Player.HurtModifiers modifiers)
    {
        //Log.Chat("Projectile: " + projectile.Name + ", hit: " + target.name + ", SourceDamage: " + modifiers.SourceDamage);

        // Replicate what vanilla does against NPCs for the Staff of Earth
        if (projectile.type == ProjectileID.BoulderStaffOfEarth && projectile.velocity.Length() < 3.5f)
        {
            modifiers.SourceDamage /= 2;
            modifiers.Knockback /= 2;
        }

        var adventureConfig = ModContent.GetInstance<ServerConfig>();

        var bounced =
            projectile.type == ProjectileID.ShadowBeamFriendly && projectile.localAI[1] > 0
            || projectile.type == ProjectileID.LightDisc && projectile.localAI[0] > 0;

        if (bounced)
            modifiers.SourceDamage *= adventureConfig.WeaponBalance.ProjectileBounceDamageReduction;

        if (adventureConfig.WeaponBalance.ProjectileLineOfSightDamageReduction.TryGetValue(new(projectile.type),
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
}
