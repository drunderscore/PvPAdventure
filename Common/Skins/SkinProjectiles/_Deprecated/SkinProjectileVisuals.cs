//using Microsoft.Xna.Framework;
//using Microsoft.Xna.Framework.Graphics;
//using ReLogic.Content;
//using Terraria;
//using Terraria.ModLoader;

//namespace PvPAdventure.Common.Skins.SkinProjectiles;

//internal sealed class SkinProjectileVisuals : GlobalProjectile
//{
//    public override bool PreDraw(Projectile projectile, ref Color lightColor)
//    {
//        // 1. Safely get our custom data and check if it has a valid skin
//        if (!projectile.TryGetGlobalProjectile(out SkinProjectileData data) || !data.Identity.IsValid)
//            return true;

//        // 2. Check if a skinned texture exists for this specific projectile type
//        if (!SkinProjectileCatalog.TryGet(data.Identity, projectile.type, out Asset<Texture2D> asset) || !asset.IsLoaded)
//            return true;

//        Texture2D texture = asset.Value;

//        // 3. Calculate the animation frame based on vanilla frame counts
//        int frameHeight = texture.Height / Main.projFrames[projectile.type];
//        Rectangle sourceRect = new Rectangle(0, frameHeight * projectile.frame, texture.Width, frameHeight);
//        Vector2 origin = sourceRect.Size() / 2f;

//        // 4. Calculate the standard draw position
//        Vector2 drawPos = projectile.Center - Main.screenPosition + new Vector2(0f, projectile.gfxOffY);
//        SpriteEffects effects = projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

//        // 5. Draw the custom skinned texture
//        Main.EntitySpriteDraw(
//            texture,
//            drawPos,
//            sourceRect,
//            projectile.GetAlpha(lightColor),
//            projectile.rotation,
//            origin,
//            projectile.scale,
//            effects,
//            0
//        );

//        // 6. Return false to prevent Terraria from drawing the vanilla texture
//        return false;
//    }
//}