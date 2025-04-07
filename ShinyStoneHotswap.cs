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



        public override void PostUpdateEquips()
        {
            // Check if Shiny Stone is equipped
            bool hasShinyStone = IsShinyStoneEquipped();

            // Apply debuff when first equipped or when you relog and rejoin (unintentional but works so who gafs)
            if (hasShinyStone && !hadShinyStoneLastFrame)
            {
                Player.AddBuff(ModContent.BuffType<ShinyStoneHotswap>(), 3600); // 60 seconds of charging upon being swapped on
            }

            // Disable Shiny Stone effects while debuffed (it just works)
            if (Player.HasBuff(ModContent.BuffType<ShinyStoneHotswap>()))
            {
                Player.shinyStone = false;
            }

            hadShinyStoneLastFrame = hasShinyStone;
        }

        public override void OnRespawn()
        {
            // We re-apply debuff if equipped and you respawn
            //TODO: we could maybe make it so that if you equip the shiny stone witin the bounds of the spawnbox you don't get the Charging debuff
            if (IsShinyStoneEquipped())
            {
                Player.AddBuff(ModContent.BuffType<ShinyStoneHotswap>(), 900); // 15 seconds of Charging after respawnin
            }
        }
        //>>>>>>> c7bba43 (Added the shiny stone code I wrote from addon to here. Truthfully I'm not sure if I need to do something special for the asset to line up, and because It's my first time doing this I probably fucked up. Also the code might be shit but we will see. Anyways I'm supposed to describe my change so: Added new debuff that prevents shiny stone from working, preventing players from switching shiny stone on/off whenever they need to heal. Also this time im pushing this to remote.)

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
}