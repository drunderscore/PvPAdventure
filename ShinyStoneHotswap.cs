using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Tpvpaquickaddon.Content.Buffs
{
    public class ShinyStoneHotswap : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = false; // Show timer
        }
    }
}