using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure;

public class AdventureBuff : GlobalBuff
{
    public override void Update(int type, Player player, ref int buffIndex)
    {
        // This has the contract that players should only have ONE Beetle Might buff at a time.
        if (type >= BuffID.BeetleMight1 && type <= BuffID.BeetleMight3)
        {
            // Calculate how many beetles we have based on the buff we have
            player.beetleOrbs = type - BuffID.BeetleMight1 + 1;

            var damage = 0.0f;
            var attackSpeed = 0.0f;

            if (player.beetleOrbs >= 1)
            {
                damage += 0.15f;
                attackSpeed += 0.15f;
            }

            if (player.beetleOrbs >= 2)
            {
                damage += 0.10f;
                attackSpeed += 0.10f;
            }

            if (player.beetleOrbs >= 3)
            {
                damage += 0.05f;
                attackSpeed += 0.30f;
            }

            player.GetDamage<MeleeDamageClass>() += damage;
            player.GetAttackSpeed<MeleeDamageClass>() += attackSpeed;
        }
    }

    public override bool RightClick(int type, int buffIndex)
    {
        // Prevent dismissing buffs that are automated.
        if (type is BuffID.BeetleMight1 or BuffID.BeetleMight2 or BuffID.BeetleMight3)
            return false;

        return true;
    }
}