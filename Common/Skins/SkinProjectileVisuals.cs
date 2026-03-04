using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Skins;

[Autoload(Side = ModSide.Client)]
internal sealed class SkinProjectileVisuals : GlobalProjectile
{
    public override bool PreDraw(Projectile projectile, ref Color lightColor)
    {
        SkinProjectileData data = projectile.GetGlobalProjectile<SkinProjectileData>();
        if (string.IsNullOrEmpty(data.SkinId))
            return true;

        if (!SkinProjectileCatalog.TryGet(data.SkinId, projectile.type, out var asset))
            return true;

        if (!asset.IsLoaded)
        {
            Main.Assets.Request<Texture2D>(asset.Name, AssetRequestMode.AsyncLoad);
            return true;
        }

        Texture2D tex = asset.Value;

        int frames = Main.projFrames[projectile.type];
        Rectangle src;

        if (frames <= 1)
            src = tex.Frame();
        else
            src = tex.Frame(1, frames, 0, projectile.frame);

        Vector2 origin = src.Size() * 0.5f;

        Vector2 pos = projectile.Center - Main.screenPosition;
        pos.Y += projectile.gfxOffY;

        SpriteEffects fx = SpriteEffects.None;
        if (projectile.spriteDirection == -1)
            fx = SpriteEffects.FlipHorizontally;

        Color color = projectile.GetAlpha(lightColor);

        Main.EntitySpriteDraw(tex, pos, src, color, projectile.rotation, origin, projectile.scale, fx, 0);
        return false;
    }
}