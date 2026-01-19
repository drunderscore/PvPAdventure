using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Content.Buffs;

public class ShatteredArmor : ModBuff
{
    public override string Texture => $"PvPAdventure/Assets/Buff/ShatteredArmor";

    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.buffNoSave[Type] = false;
        Main.buffNoTimeDisplay[Type] = false;
        Main.persistentBuff[Type] = false;
        BuffID.Sets.IsATagBuff[Type] = false;
    }
    public override void Update(Player player, ref int buffIndex)
    { }
}
