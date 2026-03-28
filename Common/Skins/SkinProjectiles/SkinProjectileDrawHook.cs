using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace PvPAdventure.Common.Skins.SkinProjectiles;

[Autoload(Side = ModSide.Client)]
internal sealed class SkinProjectileDrawHook : ModSystem
{
    public override void Load()
    {
        On_Main.DrawProj += OnDrawProj;
    }

    public override void Unload()
    {
        On_Main.DrawProj -= OnDrawProj;
    }

    private static void OnDrawProj(On_Main.orig_DrawProj orig, Main self, int i)
    {
        Projectile proj = Main.projectile[i];

        if (proj.type == ProjectileID.TrueExcalibur)
        {
            DrawProj_TrueExcalibur(proj);
            return;
        }

        if (proj.type == ProjectileID.Excalibur)
        {
            DrawProj_Excalibur(proj);
            return;
        }

        orig(self, i);
    }


    private static void DrawProj_Excalibur(Projectile proj)
    {
        Vector2 vector = proj.Center - Main.screenPosition;
        Asset<Texture2D> asset = TextureAssets.Projectile[proj.type];
        Rectangle rectangle = asset.Frame(1, 4);
        Vector2 origin = rectangle.Size() / 2f;
        float num = proj.scale * 1.1f;
        SpriteEffects effects = ((!(proj.ai[0] >= 0f)) ? SpriteEffects.FlipVertically : SpriteEffects.None);
        float num2 = proj.localAI[0] / proj.ai[1];
        float num3 = Utils.Remap(num2, 0f, 0.6f, 0f, 1f) * Utils.Remap(num2, 0.6f, 1f, 1f, 0f);
        float num4 = 0.975f;
        float fromValue = Lighting.GetColor(proj.Center.ToTileCoordinates()).ToVector3().Length() / (float)Math.Sqrt(3.0);
        fromValue = Utils.Remap(fromValue, 0.2f, 1f, 0f, 1f);

        // OLD
        //Color color = new Color(180, 160, 60);
        //Color color2 = new Color(255, 240, 150);
        //Color color3 = new Color(255, 255, 80);

        // NEW
        Color color = new Color(255, 80, 190);   // pink outer base tint
        Color color2 = Color.White;              // bright white inner
        Color color3 = new Color(255, 70, 220);  // bright pink outer ring/sparkles

        Main.spriteBatch.Draw(asset.Value, vector, rectangle, color * fromValue * num3, proj.rotation + proj.ai[0] * ((float)Math.PI / 4f) * -1f * (1f - num2), origin, num, effects, 0f);
        Color color4 = Color.White * num3 * 0.5f;
        color4.A = (byte)((float)(int)color4.A * (1f - fromValue));
        Color color5 = color4 * fromValue * 0.5f;

        // OLD (warm/yellow bias)
        //color5.G = (byte)((float)(int)color5.G * fromValue);
        //color5.B = (byte)((float)(int)color5.R * (0.25f + fromValue * 0.75f));

        // NEW (neutral white bloom)
        color5.G = color5.R;
        color5.B = color5.R;

        Main.spriteBatch.Draw(asset.Value, vector, rectangle, color5 * 0.15f, proj.rotation + proj.ai[0] * 0.01f, origin, num, effects, 0f);
        Main.spriteBatch.Draw(asset.Value, vector, rectangle, color3 * fromValue * num3 * 0.3f, proj.rotation, origin, num, effects, 0f);
        Main.spriteBatch.Draw(asset.Value, vector, rectangle, color2 * fromValue * num3 * 0.5f, proj.rotation, origin, num * num4, effects, 0f);
        Main.spriteBatch.Draw(asset.Value, vector, asset.Frame(1, 4, 0, 3), Color.White * 0.6f * num3, proj.rotation + proj.ai[0] * 0.01f, origin, num, effects, 0f);
        Main.spriteBatch.Draw(asset.Value, vector, asset.Frame(1, 4, 0, 3), Color.White * 0.5f * num3, proj.rotation + proj.ai[0] * -0.05f, origin, num * 0.8f, effects, 0f);
        Main.spriteBatch.Draw(asset.Value, vector, asset.Frame(1, 4, 0, 3), Color.White * 0.4f * num3, proj.rotation + proj.ai[0] * -0.1f, origin, num * 0.6f, effects, 0f);
        for (float num5 = 0f; num5 < 8f; num5 += 1f)
        {
            float num6 = proj.rotation + proj.ai[0] * num5 * ((float)Math.PI * -2f) * 0.025f + Utils.Remap(num2, 0f, 1f, 0f, (float)Math.PI / 4f) * proj.ai[0];
            Vector2 drawpos = vector + num6.ToRotationVector2() * ((float)asset.Width() * 0.5f - 6f) * num;
            float num7 = num5 / 9f;
            Main.DrawPrettyStarSparkle(proj.Opacity, SpriteEffects.None, drawpos, new Color(255, 255, 255, 0) * num3 * num7, color3, num2, 0f, 0.5f, 0.5f, 1f, num6, new Vector2(0f, Utils.Remap(num2, 0f, 1f, 3f, 0f)) * num, Vector2.One * num);
        }
        Vector2 drawpos2 = vector + (proj.rotation + Utils.Remap(num2, 0f, 1f, 0f, (float)Math.PI / 4f) * proj.ai[0]).ToRotationVector2() * ((float)asset.Width() * 0.5f - 4f) * num;
        Main.DrawPrettyStarSparkle(proj.Opacity, SpriteEffects.None, drawpos2, new Color(255, 255, 255, 0) * num3 * 0.5f, color3, num2, 0f, 0.5f, 0.5f, 1f, 0f, new Vector2(2f, Utils.Remap(num2, 0f, 1f, 4f, 1f)) * num, Vector2.One * num);
    }

    private static void DrawProj_TrueExcalibur(Projectile proj)
    {
        Vector2 vector = proj.Center - Main.screenPosition;
        Asset<Texture2D> asset = TextureAssets.Projectile[proj.type];
        Rectangle rectangle = asset.Frame(1, 4);
        Vector2 origin = rectangle.Size() / 2f;
        float num = proj.scale * 1.1f;
        SpriteEffects effects = ((!(proj.ai[0] >= 0f)) ? SpriteEffects.FlipVertically : SpriteEffects.None);
        float num2 = proj.localAI[0] / proj.ai[1];
        float num3 = Utils.Remap(num2, 0f, 0.6f, 0f, 1f) * Utils.Remap(num2, 0.6f, 1f, 1f, 0f);
        float num4 = 0.975f;
        float amount = num3;
        float fromValue = Lighting.GetColor(proj.Center.ToTileCoordinates()).ToVector3().Length() / (float)Math.Sqrt(3.0);
        fromValue = Utils.Remap(fromValue, 0.2f, 1f, 0f, 1f);

        // OLD
        //Color color = Color.Lerp(new Color(180, 50, 90), new Color(180, 30, 60), amount);
        //Color color2 = Color.Lerp(new Color(255, 240, 150), new Color(255, 60, 170), amount);
        //Color color3 = Color.Lerp(new Color(255, 255, 80), new Color(255, 60, 190), amount);

        // NEW
        Color color = Color.Lerp(new Color(255, 85, 195), new Color(210, 45, 165), amount); // pink outer base
        Color color2 = Color.White;                                                           // bright white inner
        Color color3 = Color.Lerp(new Color(255, 120, 235), new Color(255, 55, 215), amount); // bright pink outer

        Main.spriteBatch.Draw(asset.Value, vector, rectangle, color * fromValue * num3, proj.rotation + proj.ai[0] * ((float)Math.PI / 4f) * -1f * (1f - num2), origin, num, effects, 0f);
        Color color4 = Color.White * num3 * 0.5f;
        color4.A = (byte)((float)(int)color4.A * (1f - fromValue));
        Color color5 = color4 * fromValue * 0.5f;

        // OLD (warm/yellow bias)
        //color5.G = (byte)((float)(int)color5.G * fromValue);
        //color5.B = (byte)((float)(int)color5.R * (0.25f + fromValue * 0.75f));

        // NEW (neutral white bloom)
        color5.G = color5.R;
        color5.B = color5.R;

        Main.spriteBatch.Draw(asset.Value, vector, rectangle, color5 * 0.15f, proj.rotation + proj.ai[0] * 0.01f, origin, num, effects, 0f);

        // OLD
        //Main.spriteBatch.Draw(asset.Value, vector, rectangle, color3 * fromValue * num3 * 0.3f, proj.rotation, origin, num, effects, 0f);
        //Main.spriteBatch.Draw(asset.Value, vector, rectangle, color2 * fromValue * num3 * 0.5f, proj.rotation, origin, num * num4, effects, 0f);

        // NEW (brighter outer + brighter inner)
        Main.spriteBatch.Draw(asset.Value, vector, rectangle, color3 * fromValue * num3 * 0.55f, proj.rotation, origin, num, effects, 0f);
        Main.spriteBatch.Draw(asset.Value, vector, rectangle, color2 * fromValue * num3 * 0.9f, proj.rotation, origin, num * num4, effects, 0f);

        Main.spriteBatch.Draw(asset.Value, vector, asset.Frame(1, 4, 0, 3), Color.White * 0.6f * num3, proj.rotation + proj.ai[0] * 0.01f, origin, num, effects, 0f);
        Main.spriteBatch.Draw(asset.Value, vector, asset.Frame(1, 4, 0, 3), Color.White * 0.5f * num3, proj.rotation + proj.ai[0] * -0.05f, origin, num * 0.8f, effects, 0f);
        Main.spriteBatch.Draw(asset.Value, vector, asset.Frame(1, 4, 0, 3), Color.White * 0.4f * num3, proj.rotation + proj.ai[0] * -0.1f, origin, num * 0.6f, effects, 0f);
        float num5 = num * 0.75f;
        for (float num6 = 0f; num6 < 12f; num6 += 1f)
        {
            float num7 = proj.rotation + proj.ai[0] * num6 * ((float)Math.PI * -2f) * 0.025f + Utils.Remap(num2, 0f, 0.6f, 0f, 0.95504415f) * proj.ai[0];
            Vector2 drawpos = vector + num7.ToRotationVector2() * ((float)asset.Width() * 0.5f - 6f) * num;
            float num8 = num6 / 12f;
            Main.DrawPrettyStarSparkle(proj.Opacity, SpriteEffects.None, drawpos, new Color(255, 255, 255, 0) * num3 * num8, color3, num2, 0f, 0.5f, 0.5f, 1f, num7, new Vector2(0f, Utils.Remap(num2, 0f, 1f, 3f, 0f)) * num5, Vector2.One * num5);
        }
        Vector2 drawpos2 = vector + (proj.rotation + Utils.Remap(num2, 0f, 0.6f, 0f, 0.95504415f) * proj.ai[0]).ToRotationVector2() * ((float)asset.Width() * 0.5f - 4f) * num;
        Main.DrawPrettyStarSparkle(proj.Opacity, SpriteEffects.None, drawpos2, new Color(255, 255, 255, 0) * num3 * 0.5f, color3, num2, 0f, 0.5f, 0.5f, 1f, 0f, new Vector2(2f, Utils.Remap(num2, 0f, 1f, 4f, 1f)) * num5, Vector2.One * num5);
    }

}
