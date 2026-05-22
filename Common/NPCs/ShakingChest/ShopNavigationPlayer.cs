using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.NPCs;

public class ShopNavigationPlayer : ModPlayer
{
    public override void PostUpdate()
    {
        int nextType = ModContent.ItemType<NextPageItem>();
        int prevType = ModContent.ItemType<PrevPageItem>();

        if (Main.mouseItem.type == nextType)
        {
            Main.mouseItem = new Item();
            ShopPager.GoToNextPage();
            SoundEngine.PlaySound(SoundID.MenuTick);
            return;
        }

        if (Main.mouseItem.type == prevType)
        {
            Main.mouseItem = new Item();
            ShopPager.GoToPrevPage();
            SoundEngine.PlaySound(SoundID.MenuTick);
            return;
        }
    }
}