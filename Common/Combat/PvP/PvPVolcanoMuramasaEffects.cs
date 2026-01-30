using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Combat.PvP;

internal class PvPVolcanoMuramasaEffects : ModPlayer
{
    public override void OnHurt(Player.HurtInfo info)
    {
        if (!info.PvP)
            return;

        // Spawn Volcano projectile when hit by Volcano sword
        if (info.DamageSource.SourceItem != null &&
            info.DamageSource.SourceItem.type == ItemID.FieryGreatsword &&
            Main.netMode != NetmodeID.MultiplayerClient)
        {
            int owner = info.DamageSource.SourcePlayerIndex >= 0 && info.DamageSource.SourcePlayerIndex < Main.maxPlayers
                ? info.DamageSource.SourcePlayerIndex
                : -1;

            int proj = Projectile.NewProjectile(
                Player.GetSource_OnHurt(info.DamageSource),
                Player.Center,
                Vector2.Zero,
                ProjectileID.Volcano,
                    (int)(info.SourceDamage * 0.75f), // 75% damage like vanilla
                0f,
                owner,
                0f,
                0f
            );

            if (proj >= 0 && proj < Main.maxProjectiles && Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, proj);
            }
        }

        // Spawn Muramasa projectile when hit by Muramasa
        if (info.DamageSource.SourceItem != null &&
    info.DamageSource.SourceItem.type == ItemID.Muramasa &&
    Main.netMode != NetmodeID.MultiplayerClient)
        {
            int owner = info.DamageSource.SourcePlayerIndex;
            Player attacker = Main.player[owner];
            int direction = Math.Sign(attacker.Center.X - Player.Center.X);
            if (direction == 0) direction = 1;

            int num5 = 1;

            for (int j = 0; j < num5; j++)
            {
                Rectangle hitbox = Player.Hitbox;
                hitbox.Inflate(30, 16);
                hitbox.Y -= 8;

                Vector2 randomPos = Main.rand.NextVector2FromRectangle(hitbox);
                Vector2 center = hitbox.Center.ToVector2();

                Vector2 velocity = (center - randomPos).SafeNormalize(new Vector2(direction, Player.gravDir)) * 8f;

                float rotationFactor = (float)(Main.rand.Next(2) * 2 - 1) * (0.62831855f + 2.5132742f * Main.rand.NextFloat());
                rotationFactor *= 0.5f;

                velocity = velocity.RotatedBy(0.7853981852531433);

                int steps = 3;
                int rotationSteps = 10 * steps;
                int velocitySteps = 5;
                int totalSteps = velocitySteps * steps;

                Vector2 spawnPos = center;
                for (int k = 0; k < totalSteps; k++)
                {
                    spawnPos -= velocity;
                    velocity = velocity.RotatedBy(-rotationFactor / rotationSteps);
                }

                spawnPos += Player.velocity * velocitySteps;

                // Create the projectile
                int proj = Projectile.NewProjectile(
                    Player.GetSource_OnHurt(info.DamageSource),
                    spawnPos,
                    velocity,
                    ProjectileID.Muramasa,
                    (int)(info.SourceDamage * 0.5f), // 50% damage like vanilla
                    0f,
                    owner,
                    rotationFactor,
                    0f
                );

                if (proj >= 0 && proj < Main.maxProjectiles && Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, proj);
                }
            }
        }
    }
}
