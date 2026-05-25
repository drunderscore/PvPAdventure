using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.NPCs;

public class NextPageItem : ModItem
{
    public override string Texture => $"PvPAdventure/Assets/Custom/NextPageItem";

    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 0;
    }

    public override void SetDefaults()
    {
        Item.width = 20;
        Item.height = 20;
        Item.rare = ItemRarityID.White;
        Item.value = 0;
    }
}