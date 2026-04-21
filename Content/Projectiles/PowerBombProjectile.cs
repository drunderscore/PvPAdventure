using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Content.Projectiles;
/// <summary>
/// The PowerBomb projectile from vanilla 1.4.5, recreated.
/// </summary>
internal class PowerBombProjectile : ModProjectile
{
    public override string Texture => "PvPAdventure/Assets/Projectiles/PowerBombProjectile";

    private const int FuseTime = 60 * 5;

    private const int ExplosionRadius = 9;

    public override void SetStaticDefaults()
    {
        Main.projFrames[Projectile.type] = 1;
    }

    public override void SetDefaults()
    {
        Projectile.width = 22;
        Projectile.height = 22;

        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.DamageType = DamageClass.Summon;
        Projectile.penetrate = -1;
        Projectile.timeLeft = FuseTime;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = false;
        Projectile.aiStyle = 0;
    }
    
    public override bool? CanDamage() => false;

    #region AI

    public override void AI()
    {
        // Gravity
        Projectile.velocity.Y += 0.2f;
        Projectile.velocity.X *= 0.99f;
        Projectile.rotation += Projectile.velocity.X * 0.05f;

        if (Main.rand.NextBool(3))
            Dust.NewDust(Projectile.position, Projectile.width, Projectile.height,
                DustID.Smoke, 0f, -1f, 100, default, 0.8f);

        if (Main.rand.NextBool(4))
            Dust.NewDust(Projectile.position, Projectile.width, Projectile.height,
                DustID.Torch, 0f, -1f, 0, default, 1.2f);
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (Projectile.velocity.X != oldVelocity.X)
            Projectile.velocity.X = -oldVelocity.X * 0.4f;

        if (Projectile.velocity.Y != oldVelocity.Y && oldVelocity.Y > 1f)
            Projectile.velocity.Y = -oldVelocity.Y * 0.4f;

        return false;
    }

    #endregion

    #region Explosion

    public override void OnKill(int timeLeft)
    {
        Explode();
    }

    private void Explode()
    {
        SoundEngine.PlaySound(SoundID.Item14, Projectile.position);

        SpawnExplosionDust();
        DestroyTiles();

        DamagePlayers();
        SpawnExplosionHitbox();
    }
    private void SpawnExplosionHitbox()
    {
        int hitboxSize = ExplosionRadius * 16 * 2;

        Projectile.NewProjectile(
            Projectile.GetSource_FromThis(),
            Projectile.Center - new Vector2(hitboxSize / 2f),
            Vector2.Zero,
            ModContent.ProjectileType<PowerBombExplosion>(),
            200,  // damage
            9f,   // knockback
            Projectile.owner
        );
    }
    private void DamagePlayers()
    {
        float explosionPixelRadius = ExplosionRadius * 16f;
        Player owner = Main.player[Projectile.owner];

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player target = Main.player[i];

            if (!target.active || target.dead)
                continue;

            // Vanilla team explosion behavior
            if (target.whoAmI != Projectile.owner
                && owner.team != 0
                && target.team == owner.team)
                continue;

            if (target.Center.DistanceSQ(Projectile.Center) > explosionPixelRadius * explosionPixelRadius)
                continue;

            Vector2 direction = target.Center - Projectile.Center;
            if (direction == Vector2.Zero)
                direction = Vector2.UnitY;

            int hitDirection = direction.X > 0 ? 1 : -1;

            target.Hurt(
                PlayerDeathReason.ByProjectile(Projectile.owner, Projectile.whoAmI),
                200,
                hitDirection,
                pvp: true,
                knockback: 9f
            );
        }
    }

    private void SpawnExplosionDust()
    {
        for (int i = 0; i < 60; i++)
        {
            Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height,
                DustID.Smoke, 0f, 0f, 100, default, Main.rand.NextFloat(2f, 5f));
            d.velocity *= 6f;
            d.noGravity = true;
        }

        for (int i = 0; i < 40; i++)
        {
            Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height,
                DustID.Torch, 0f, 0f, 0, default, Main.rand.NextFloat(3f, 6f));
            d.velocity *= 8f;
            d.noGravity = true;
        }
    }

    private void DestroyTiles()
    {
        if (Projectile.npcProj || Projectile.trap)
            return;

        int centerX = (int)(Projectile.Center.X / 16f);
        int centerY = (int)(Projectile.Center.Y / 16f);

        bool allMechBossesDefeated = NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3;

        for (int x = centerX - ExplosionRadius; x <= centerX + ExplosionRadius; x++)
        {
            for (int y = centerY - ExplosionRadius; y <= centerY + ExplosionRadius; y++)
            {
                float dx = x - centerX;
                float dy = y - centerY;
                if (dx * dx + dy * dy > ExplosionRadius * ExplosionRadius)
                    continue;

                if (!WorldGen.InWorld(x, y, 1))
                    continue;

                Tile tile = Main.tile[x, y];
                if (tile == null)
                    continue;

                if (tile.HasTile && !IsImmuneToExplosion(tile, allMechBossesDefeated))
                    WorldGen.KillTile(x, y, false, false, false);

                if (tile.WallType > 0 && !IsWallImmuneToExplosion(tile))
                    WorldGen.KillWall(x, y, false);
            }
        }

        if (Main.netMode == NetmodeID.Server)
        {
            NetMessage.SendTileSquare(-1,
                centerX - ExplosionRadius,
                centerY - ExplosionRadius,
                ExplosionRadius * 2 + 1,
                ExplosionRadius * 2 + 1);
        }
    }

    private static bool IsImmuneToExplosion(Tile tile, bool allMechBossesDefeated)
    {
        if (tile.TileType == TileID.LihzahrdBrick
            || tile.TileType == TileID.LihzahrdAltar
            || tile.TileType == TileID.DemonAltar
            || tile.TileType == TileID.PinkDungeonBrick
            || tile.TileType == TileID.BlueDungeonBrick
            || tile.TileType == TileID.GreenDungeonBrick)
            return true;

        if (tile.TileType == TileID.Chlorophyte && !allMechBossesDefeated) // This is not in vanilla ; this is our own change
            return true;

        return false;
    }

    private static bool IsWallImmuneToExplosion(Tile tile)
    {
        if (tile.WallType == WallID.LihzahrdBrickUnsafe
            || tile.WallType == WallID.PinkDungeonUnsafe
            || tile.WallType == WallID.BlueDungeonUnsafe
            || tile.WallType == WallID.GreenDungeonUnsafe)
            return true;

        return false;
    }

    #endregion
}