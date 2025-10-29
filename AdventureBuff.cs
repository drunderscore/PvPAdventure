using System;
using Terraria;
using Terraria.DataStructures;
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
                damage += 0.08f;
                attackSpeed += 0.08f;
            }

            if (player.beetleOrbs >= 2)
            {
                damage += 0.08f;
                attackSpeed += 0.08f;
            }

            if (player.beetleOrbs >= 3)
            {
                damage += 0.08f;
                attackSpeed += 0.08f;
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

    public class RemoveFlaskBuffsOnDeath : ModPlayer
    {
        // Array of buff IDs to remove on death
        private readonly int[] buffsToRemove = { 71, 73, 74, 75, 76, 77, 78, 79 }; //Every single Flask buff that doesn't go away on death for some reason

        public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
        {

            for (int i = 0; i < Player.MaxBuffs; i++)
            {
                int buffType = Player.buffType[i];


                foreach (int buffId in buffsToRemove)
                {
                    if (buffType == buffId)
                    {
                        Player.DelBuff(i); // Remove the buff
                        i--; // Adjust index since we removed a buff
                        break;
                    }
                }
            }
        }
    }
    public class ArcheryNerf : GlobalItem
    {
        public override void ModifyWeaponDamage(Item item, Player player, ref StatModifier damage)
        {
            if (player.HasBuff(BuffID.Archery) && item.useAmmo == AmmoID.Arrow)
            {
                damage /= 1.1f;
            }
        }
    }
    public class BuffTextChange : GlobalBuff
    {
   public override void ModifyBuffText(int type, ref string buffName, ref string tip, ref int rare)
        {
            if (type == BuffID.Archery)
            {
                tip = "20% increased arrow speed\nNo longer grants bow damage";
            }
        }
    }
}