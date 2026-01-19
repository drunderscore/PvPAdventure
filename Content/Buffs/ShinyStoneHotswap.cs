using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Content.Buffs;

public class ShinyStoneHotswap : ModBuff
{
    public override string Texture => $"PvPAdventure/Assets/Buff/ShinyStoneHotswap";

    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.buffNoSave[Type] = true;
        Main.buffNoTimeDisplay[Type] = false;
    }
}
