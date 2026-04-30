using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace PvPAdventure.Common.Spectator.Drawers.Crafting;

public static class CraftingMenuDrawer
{
    public static void DrawCrafting(CraftingMenuSnapshot snapshot)
    {
        if (snapshot.NumAvailableRecipes <= 0 || snapshot.AvailableRecipes == null || snapshot.AvailableRecipeY == null)
            return;

        float oldScale = Main.inventoryScale;
        Color oldBack = Main.inventoryBack;

        try
        {
            Color textColor = new(
                (byte)(Main.mouseTextColor * Main.craftingAlpha),
                (byte)(Main.mouseTextColor * Main.craftingAlpha),
                (byte)(Main.mouseTextColor * Main.craftingAlpha),
                (byte)(Main.mouseTextColor * Main.craftingAlpha)
            );

            int visibleRange = (int)(Main.screenHeight / 600f * 250f);
            int screenOffsetY = (Main.screenHeight - 600) / 2;

            Main.spriteBatch.DrawString(FontAssets.MouseText.Value, Lang.inter[25].Value, new Vector2(76f, 414 + screenOffsetY), textColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

            for (int i = 0; i < snapshot.NumAvailableRecipes; i++)
            {
                if (i >= snapshot.AvailableRecipes.Length || i >= snapshot.AvailableRecipeY.Length)
                    break;

                int recipeIndex = snapshot.AvailableRecipes[i];

                if (recipeIndex < 0 || recipeIndex >= Recipe.maxRecipes)
                    continue;

                float recipeY = snapshot.AvailableRecipeY[i];

                Main.inventoryScale = 100f / (Math.Abs(recipeY) + 100f);

                if (Main.inventoryScale < 0.75f)
                    Main.inventoryScale = 0.75f;

                if (Math.Abs(recipeY) > visibleRange)
                    continue;

                int x = (int)(46f - 26f * Main.inventoryScale);
                int y = (int)(410f + recipeY * Main.inventoryScale - 30f * Main.inventoryScale + screenOffsetY);

                double backAlpha = Main.inventoryBack.A + 50;
                double lightAlpha = 255.0;

                if (Math.Abs(recipeY) > visibleRange - 100f)
                {
                    float fade = 100f - (Math.Abs(recipeY) - (visibleRange - 100f));
                    backAlpha = 150f * fade * 0.01f;
                    lightAlpha = 255f * fade * 0.01f;
                }

                backAlpha = Math.Clamp(backAlpha - 50.0, 0.0, 255.0);
                lightAlpha = Math.Clamp(lightAlpha, 0.0, 255.0);

                Color oldInventoryBack = Main.inventoryBack;
                Main.inventoryBack = new Color((byte)backAlpha, (byte)backAlpha, (byte)backAlpha, (byte)backAlpha);

                Item createItem = Main.recipe[recipeIndex].createItem;
                Color lightColor = new((byte)lightAlpha, (byte)lightAlpha, (byte)lightAlpha, (byte)lightAlpha);

                ItemSlot.Draw(Main.spriteBatch, ref createItem, 22, new Vector2(x, y), lightColor);

                Main.inventoryBack = oldInventoryBack;
            }

            int focus = snapshot.FocusRecipe;

            if (focus < 0 || focus >= snapshot.NumAvailableRecipes || focus >= snapshot.AvailableRecipes.Length)
                return;

            int focusedRecipeIndex = snapshot.AvailableRecipes[focus];

            if (focusedRecipeIndex < 0 || focusedRecipeIndex >= Recipe.maxRecipes)
                return;

            Recipe recipe = Main.recipe[focusedRecipeIndex];
            float focusedY = focus < snapshot.AvailableRecipeY.Length ? snapshot.AvailableRecipeY[focus] : 0f;

            for (int i = 0; i < recipe.requiredItem.Count; i++)
            {
                Item item = recipe.requiredItem[i];

                if (item == null || item.IsAir)
                    break;

                int x = 80 + i * 40;
                int y = 380 + screenOffsetY;

                float backAlpha = Main.inventoryBack.A + 50 - Math.Abs(focusedY) * 2f;
                float lightAlpha = 255f - Math.Abs(focusedY) * 2f;

                backAlpha = MathHelper.Clamp(backAlpha, 0f, 255f);
                lightAlpha = MathHelper.Clamp(lightAlpha, 0f, 255f);

                if (backAlpha <= 0f)
                    break;

                Main.inventoryScale = 0.6f;

                Color oldInventoryBack = Main.inventoryBack;
                Main.inventoryBack = new Color((byte)Math.Max(0f, backAlpha - 50f), (byte)Math.Max(0f, backAlpha - 50f), (byte)Math.Max(0f, backAlpha - 50f), (byte)Math.Max(0f, backAlpha - 50f));

                Item material = item;
                ItemSlot.Draw(Main.spriteBatch, ref material, 22, new Vector2(x, y), new Color((byte)lightAlpha, (byte)lightAlpha, (byte)lightAlpha, (byte)lightAlpha));

                Main.inventoryBack = oldInventoryBack;
            }
        }
        finally
        {
            Main.inventoryScale = oldScale;
            Main.inventoryBack = oldBack;
        }
    }
}

public readonly struct CraftingMenuSnapshot
{
    public readonly int[] AvailableRecipes;
    public readonly float[] AvailableRecipeY;
    public readonly int FocusRecipe;
    public readonly int NumAvailableRecipes;

    public CraftingMenuSnapshot(int[] availableRecipes, float[] availableRecipeY, int focusRecipe, int numAvailableRecipes)
    {
        AvailableRecipes = availableRecipes;
        AvailableRecipeY = availableRecipeY;
        FocusRecipe = focusRecipe;
        NumAvailableRecipes = numAvailableRecipes;
    }
}
