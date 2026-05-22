using PvPAdventure.Content.Buffs;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace PvPAdventure.Content.Buffs
{
    public class PlayerInSpawn : ModBuff
    {
        public override string Texture => $"PvPAdventure/Assets/Buff/PlayerInSpawn";

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = false;
            Main.buffNoTimeDisplay[Type] = true;
            Main.persistentBuff[Type] = true;
        }
        public override void Update(Player player, ref int buffIndex)
        {
            player.GetDamage(DamageClass.Generic) *= -999f;
        }
    }
}

namespace PvPAdventure.Common.Players
{
    public class PlayerInSpawnPlayer : ModPlayer
    {
        public override bool ImmuneTo(PlayerDeathReason damageSource, int cooldownCounter, bool pvp)
        {
            if (Player.HasBuff(ModContent.BuffType<PlayerInSpawn>()))
                return true;

            return false;
        }
    }
}