using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure
{

    public class ShinyStoneHotswap : ModBuff

    {
        public override string Texture => $"PvPAdventure/Assets/Buff/ShinyStoneHotswap}";
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true; 
            Main.buffNoTimeDisplay[Type] = false; 
        }
    }



        private bool IsShinyStoneEquipped()
        {
            for (int i = 3; i < 10; i++) // Check all accessory slots 1-5
            {
                if (Player.armor[i].type == ItemID.ShinyStone &&
                   (i < 7 || !Player.hideVisibleAccessory[i - 3]))
                {
                    return true;
                }
            }
            return false;
        }
    }
)