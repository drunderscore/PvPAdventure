using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace PvPAdventure.Common.Combat.EJ;

public class ClockworkAssaultRifleHighVelocity : GlobalItem
{
    public override bool AppliesToEntity(Item entity, bool lateInstantiation) =>
        entity.type == ItemID.ClockworkAssaultRifle;

    public override void ModifyShootStats(Item item, Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
    {
        if (type == ProjectileID.Bullet)
            type = ProjectileID.BulletHighVelocity;
    }
}