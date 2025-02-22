using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.System;

public class ShimmerBalancing : ModSystem
{
    public static List<Item> alchemyRecipeItems = [];

    public override void Load()
    {
        On_Item.GetShimmered += On_ItemGetShimmered;
    }

    private void On_ItemGetShimmered(On_Item.orig_GetShimmered orig, Item self)
    {
        for (int i = 0; i < alchemyRecipeItems.Count; i++)
        {
            if (self.type == alchemyRecipeItems[i].type)
            {
                GetShimmeredPotions(self);
                return;
            }
        }
        orig(self);
    }

    private void GetShimmeredPotions(Item self)
    {
        int shimmerEquivalentType = ItemID.Sets.ShimmerCountsAsItem[self.type] != -1 ? ItemID.Sets.ShimmerCountsAsItem[self.type] : self.type;
        int decraftingRecipeIndex = ShimmerTransforms.GetDecraftingRecipeIndex(shimmerEquivalentType);
        if (decraftingRecipeIndex >= 0)
        {
            int num = decraftingRecipeIndex < 0 ? -1 : self.stack / Main.recipe[decraftingRecipeIndex].createItem.stack;
            Recipe recipe = Main.recipe[decraftingRecipeIndex];
            int num2 = 0;
            bool flag = recipe.requiredItem.Count > 1;
            IEnumerable<Item> enumerable = recipe.requiredItem;
            if (recipe.customShimmerResults != null)
            {
                enumerable = recipe.customShimmerResults;
            }
            int num3 = 0;
            foreach (Item item2 in enumerable)
            {
                if (item2.type <= 0)
                {
                    break;
                }
                num3++;
                int num4 = num * item2.stack;
                for (int num5 = num4; num5 > 0; num5--)
                {
                    if (Main.rand.NextBool(3))
                    {
                        num4--;
                    }
                }
                while (num4 > 0)
                {
                    int num6 = num4;
                    if (num6 > 9999)
                    {
                        num6 = 9999;
                    }
                    num4 -= num6;
                    int num7 = Item.NewItem(self.GetSource_Misc("Shimmer"), (int)self.position.X, (int)self.position.Y, self.width, self.height, item2.type);
                    Item item = Main.item[num7];
                    item.stack = num6;
                    item.shimmerTime = 1f;
                    item.shimmered = true;
                    item.shimmerWet = true;
                    item.wet = true;
                    item.velocity *= 0.1f;
                    item.playerIndexTheItemIsReservedFor = Main.myPlayer;
                    if (flag)
                    {
                        item.velocity.X = 1f * (float)num3;
                        item.velocity.X *= 1f + (float)num3 * 0.05f;
                        if (num2 % 2 == 0)
                        {
                            item.velocity.X *= -1f;
                        }
                    }
                    NetMessage.SendData(145, -1, -1, null, num7, 1f);
                }
            }
            self.stack -= num * recipe.createItem.stack;
            if (self.stack <= 0)
            {
                self.stack = 0;
                self.type = 0;
            }
        }
        if (self.stack > 0)
            self.shimmerTime = 1f;
        else
            self.shimmerTime = 0f;

        self.shimmerWet = true;
        self.wet = true;
        self.velocity *= 0.1f;
        if (Main.netMode == NetmodeID.SinglePlayer)
            Item.ShimmerEffect(self.Center);
        else
        {
            NetMessage.SendData(146, -1, -1, null, 0, (int)self.Center.X, (int)self.Center.Y);
            NetMessage.SendData(145, -1, -1, null, self.whoAmI, 1f);
        }
        if (self.stack == 0)
        {
            self.makeNPC = -1;
            self.active = false;
        }
    }
    public override void PostAddRecipes()
    {
        for (int i = 0; i < Main.recipe.Length; i++)
        {
            Recipe recipe = Main.recipe[i];
            if (recipe.requiredTile.Count > 0 && recipe.requiredTile[0] == TileID.Bottles)
            {
                alchemyRecipeItems.Add(recipe.createItem);
            }
        }
    }

    public class ShimmerReforging : GlobalItem
    {
        public override void OnCreated(Item item, ItemCreationContext context)
        {
            Player player = Main.LocalPlayer;
            if (player.ZoneShimmer)
            {
                item.ResetPrefix();
            }
        }
    }
}