//using Microsoft.Xna.Framework;
//using Microsoft.Xna.Framework.Graphics;
//using Terraria;
//using Terraria.GameContent;
//using Terraria.ModLoader;

//namespace PvPAdventure.Core.HealthBars;
//internal class HealthBarHooks : ModSystem
//{
//    public override void Load()
//    {
//        On_Main.DrawHealthBar += ModifyHealthBar;
//    }
//    public override void Unload()
//    {
//        On_Main.DrawHealthBar -= ModifyHealthBar;
//    }

//    private void ModifyHealthBar(On_Main.orig_DrawHealthBar orig, Main self, float X, float Y, int Health, int MaxHealth, float alpha, float scale, bool noFlip)
//    {
//        //orig(self, X, Y, Health, MaxHealth, alpha, scale, noFlip);
//        //var cfg = ModContent.GetInstance<AdventureClientConfig>();

//        if (Health <= 0)
//            return;

//        // Get health ratio
//        float healthRatio = (float)Health / MaxHealth;
//        if (healthRatio > 1f)
//            healthRatio = 1f;
//        int barPixels = (int)(36f * healthRatio);

//        // World position
//        float barWorldX = X - 18f * scale;
//        float barWorldY = Y;
//        if (Main.LocalPlayer.gravDir == -1f && !noFlip)
//        {
//            barWorldY -= Main.screenPosition.Y;
//            barWorldY = Main.screenPosition.Y + Main.screenHeight - barWorldY;
//        }

//        // Bar color
//        float alphaByte = 255f;
//        healthRatio -= 0.1f;
//        float green = healthRatio > 0.5f ? 255f : 255f * healthRatio * 2f;
//        float red = healthRatio > 0.5f ? 255f * (1f - healthRatio) * 2f : 255f;
//        float colorScale = alpha * 0.95f;
//        red *= colorScale;
//        green *= colorScale;
//        alphaByte *= colorScale;
//        red = MathHelper.Clamp(red, 0f, 255f);
//        green = MathHelper.Clamp(green, 0f, 255f);
//        alphaByte = MathHelper.Clamp(alphaByte, 0f, 255f);
//        Color barColor = new((byte)red, (byte)green, (byte)0, (byte)alphaByte);

//        // Clamp minimum bar width
//        if (barPixels < 3)
//            barPixels = 3;

//        // Position and textures
//        Vector2 screenOrigin = new Vector2(barWorldX - Main.screenPosition.X, barWorldY - Main.screenPosition.Y);
//        Texture2D backTex = TextureAssets.Hb2.Value;
//        Texture2D fillTex = TextureAssets.Hb1.Value;

//        // Draw bar
//        if (barPixels < 34)
//        {
//            if (barPixels < 36)
//                Main.spriteBatch.Draw(backTex, screenOrigin + new Vector2(barPixels * scale, 0f), new Rectangle(2, 0, 2, backTex.Height), barColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

//            if (barPixels < 34)
//                Main.spriteBatch.Draw(backTex, screenOrigin + new Vector2((barPixels + 2) * scale, 0f), new Rectangle(barPixels + 2, 0, 36 - barPixels - 2, backTex.Height), barColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

//            if (barPixels > 2)
//                Main.spriteBatch.Draw(fillTex, screenOrigin, new Rectangle(0, 0, barPixels - 2, fillTex.Height), barColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

//            Main.spriteBatch.Draw(fillTex, screenOrigin + new Vector2((barPixels - 2) * scale, 0f), new Rectangle(32, 0, 2, fillTex.Height), barColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
//        }
//        else
//        {
//            if (barPixels < 36)
//                Main.spriteBatch.Draw(backTex, screenOrigin + new Vector2(barPixels * scale, 0f), new Rectangle(barPixels, 0, 36 - barPixels, backTex.Height), barColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

//            Main.spriteBatch.Draw(fillTex, screenOrigin, new Rectangle(0, 0, barPixels, fillTex.Height), barColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
//        }
//    }
//}
