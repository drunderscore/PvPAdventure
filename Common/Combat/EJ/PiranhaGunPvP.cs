using Microsoft.Xna.Framework;
using PvPAdventure.Content.Buffs;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace PvPAdventure.Common.Combat.EJ
{
    public class PiranhaGunProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        private const float StateFlying = 0f;
        private const float StateAttached = 1f;
        private const float StateReturning = 2f;

        private Vector2 attachOffset = Vector2.Zero;

        public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
            => entity.type == ProjectileID.MechanicalPiranha;

        public override void SetDefaults(Projectile projectile)
        {
            projectile.friendly = true;
        }

        public bool HasPlayerTarget(Projectile proj) => proj.ai[1] < 0;
        public int GetPlayerTarget(Projectile proj) => -(int)proj.ai[1] - 1;
        public void SetPlayerTarget(Projectile proj, int index, Vector2 offset)
        {
            proj.ai[0] = StateAttached;
            proj.ai[1] = -(index + 1);
            attachOffset = offset;
            proj.netUpdate = true;
        }
        public void ClearPlayerTarget(Projectile proj)
        {
            proj.ai[0] = StateReturning;
            proj.ai[1] = 0f;
            attachOffset = Vector2.Zero;
            proj.netUpdate = true;
        }

        public override bool PreAI(Projectile projectile)
        {
            if (HasPlayerTarget(projectile))
            {
                int targetIndex = GetPlayerTarget(projectile);
                Player owner = Main.player[projectile.owner];
                Player target = Main.player[targetIndex];

                if (projectile.owner == Main.myPlayer)
                {
                    bool shouldDetach =
                        !target.active || target.dead || !target.hostile || target.team == owner.team ||
                        target.HasBuff(ModContent.BuffType<PlayerInSpawn>()) ||
                        !owner.channel || owner.HeldItem.type != ItemID.PiranhaGun ||
                        Vector2.Distance(projectile.Center, owner.Center) > 2000f;

                    if (shouldDetach)
                    {
                        ClearPlayerTarget(projectile);
                        return true;
                    }

                    projectile.Center = target.Center + attachOffset;
                    projectile.velocity = Vector2.Zero;
                    projectile.rotation = (target.Center - owner.Center).ToRotation() + MathHelper.PiOver2;
                    projectile.timeLeft = 300;
                    projectile.netUpdate = true;

                    if (projectile.localAI[0] <= 0f)
                    {
                        projectile.localAI[0] = 14f;
                        int hitDirection = (target.Center.X > owner.Center.X) ? 1 : -1;
                        target.Hurt(PlayerDeathReason.ByProjectile(owner.whoAmI, projectile.whoAmI),
                            projectile.damage, hitDirection, pvp: true);
                    }
                    else
                    {
                        projectile.localAI[0]--;
                    }
                }
                else
                {
                    projectile.velocity = Vector2.Zero;
                }

                return false;
            }

            if (!HasPlayerTarget(projectile) && projectile.owner == Main.myPlayer)
            {
                Player owner = Main.player[projectile.owner];
                if (owner.hostile)
                {
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        if (i == projectile.owner)
                            continue;

                        Player target = Main.player[i];

                        if (!target.active || target.dead || !target.hostile || target.team == owner.team ||
                            target.HasBuff(ModContent.BuffType<PlayerInSpawn>()))
                            continue;

                        Rectangle projHitbox = new Rectangle((int)projectile.position.X, (int)projectile.position.Y,
                            projectile.width, projectile.height);
                        Rectangle playerHitbox = new Rectangle((int)target.position.X, (int)target.position.Y,
                            target.width, target.height);

                        if (projHitbox.Intersects(playerHitbox))
                        {
                            Vector2 offset = projectile.Center - target.Center;
                            SetPlayerTarget(projectile, target.whoAmI, offset);
                            int hitDirection = (target.Center.X > owner.Center.X) ? 1 : -1;
                            target.Hurt(PlayerDeathReason.ByProjectile(owner.whoAmI, projectile.whoAmI),
                                projectile.damage, hitDirection, pvp: true);
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        public override void OnHitPlayer(Projectile projectile, Player target, Player.HurtInfo info)
        {
            if (projectile.owner != Main.myPlayer)
                return;

            Player owner = Main.player[projectile.owner];

            if (!owner.hostile || !target.hostile || owner.team == target.team ||
                target.HasBuff(ModContent.BuffType<PlayerInSpawn>()))
                return;

            if (!HasPlayerTarget(projectile))
            {
                Vector2 offset = projectile.Center - target.Center;
                SetPlayerTarget(projectile, target.whoAmI, offset);
            }
        }

        public override bool CanHitPlayer(Projectile projectile, Player target)
        {
            if (projectile.owner == target.whoAmI)
                return false;

            if (HasPlayerTarget(projectile) && GetPlayerTarget(projectile) == target.whoAmI)
                return false;

            Player owner = Main.player[projectile.owner];
            if (!owner.hostile || !target.hostile || owner.team == target.team)
                return false;

            return base.CanHitPlayer(projectile, target);
        }

        public override void ModifyHitPlayer(Projectile projectile, Player target, ref Player.HurtModifiers modifiers)
        {
            modifiers.Knockback *= 0f;
        }

        public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            binaryWriter.Write(attachOffset.X);
            binaryWriter.Write(attachOffset.Y);
        }

        public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
        {
            attachOffset.X = binaryReader.ReadSingle();
            attachOffset.Y = binaryReader.ReadSingle();
        }
    }
    public class PiranhaGunItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
            => entity.type == ItemID.PiranhaGun;

        public override void HoldItem(Item item, Player player)
        {
            bool hasActivePiranha = false;
            bool hasAttachedPiranha = false;
            Projectile attachedProj = null;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (!proj.active || proj.owner != player.whoAmI || proj.type != ProjectileID.MechanicalPiranha)
                    continue;

                hasActivePiranha = true;

                var modProj = proj.GetGlobalProjectile<PiranhaGunProjectile>();
                if (modProj.HasPlayerTarget(proj))
                {
                    hasAttachedPiranha = true;
                    attachedProj = proj;
                    break;
                }
            }

            if (hasActivePiranha)
            {
                player.itemAnimation = player.itemAnimationMax;
                player.itemTime = player.itemTimeMax;
            }

            if (hasAttachedPiranha && attachedProj != null)
            {
                int targetIdx = attachedProj.GetGlobalProjectile<PiranhaGunProjectile>().GetPlayerTarget(attachedProj);
                Player target = Main.player[targetIdx];
                if (target.active)
                {
                    player.direction = (target.Center.X > player.Center.X) ? 1 : -1;
                }
            }
        }

        public override bool CanUseItem(Item item, Player player)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.owner == player.whoAmI && proj.type == ProjectileID.MechanicalPiranha)
                    return false;
            }
            return base.CanUseItem(item, player);
        }
    }
}