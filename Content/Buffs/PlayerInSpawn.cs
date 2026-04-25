using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Content.Buffs;

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
        player.statDefense += 99999999; // scuffed way to prevent players from taking damage in spawn
    }
}
