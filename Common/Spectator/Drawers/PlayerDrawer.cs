using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.UI;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics;

namespace PvPAdventure.Common.Spectator.Drawers;

public static class PlayerDrawer
{
    #region Full player

    public static void DrawFullPlayerPreview(SpriteBatch sb, Player player, Rectangle box)
    {
        const int pad = 8;
        const float scale = 1f;
        const float footXOffset = 32f; // fixed horizontal anchor from left
        const float footOffsetY = 4f;  // moves player up/down together
        const float nameScale = 1f;
        const float nameGap = -8f;

        Rectangle previewBox = new(box.X + pad, box.Y + pad, box.Height - pad * 2, box.Height - pad * 2);

        string name = StatDrawer.Truncate(FontAssets.MouseText.Value, player.name, previewBox.Width - 8, nameScale);
        Vector2 nameSize = FontAssets.MouseText.Value.MeasureString(name) * nameScale;

        Player drawPlayer = CreateFullDrawPlayer(player);

        float feetX = previewBox.X + footXOffset;
        float feetY = previewBox.Bottom + footOffsetY;

        Vector2 drawPos = new(
            (int)MathF.Round(feetX - drawPlayer.width * scale * 0.5f),
            (int)MathF.Round(feetY - drawPlayer.height * scale + drawPlayer.gfxOffY * scale));

        float playerTopY = feetY - drawPlayer.height * scale + drawPlayer.gfxOffY * scale;

        Vector2 namePos = new(
            (int)MathF.Round(feetX - nameSize.X * 0.5f),
            (int)MathF.Round(playerTopY - nameSize.Y - nameGap));

        DrawFullPlayer(sb, player, drawPos, scale);
        Utils.DrawBorderString(sb, name, namePos, Color.White, nameScale);
    }

    public static void DrawPlayerBackground(SpriteBatch sb, Rectangle rect, Color color = default, int borderX = 4, int borderY = 4)
    {
        if (color == default)
            color = Color.White;

        Texture2D texture = Main.Assets.Request<Texture2D>("Images/UI/PlayerBackground").Value;
        //DrawNineSlice(sb, texture, rect, borderX, borderY, borderX, borderY, color);
    }

    public static void DrawFullPlayer(SpriteBatch sb, Player player, Vector2 position, float scale = 1f)
    {
        Player drawPlayer = CreateFullDrawPlayer(player);

        sb.End();
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);

        FullBrightDrawer.ForceFullBrightOnce = true;
        try
        {
            if (drawPlayer.ghost)
                DrawGhost(Main.Camera, drawPlayer, position + Main.screenPosition, scale);
            else
                Main.PlayerRenderer.DrawPlayer(Main.Camera, drawPlayer, position + Main.screenPosition, 0f, Vector2.Zero, 0f, scale);
        }
        finally
        {
            FullBrightDrawer.ForceFullBrightOnce = false;
        }

        sb.End();
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);
    }

    private static Player CreateFullDrawPlayer(Player player)
    {
        Player drawPlayer = player.SerializedClone();
        drawPlayer.position = player.position;
        drawPlayer.velocity = player.velocity;
        drawPlayer.direction = player.direction;
        drawPlayer.gravDir = player.gravDir;
        drawPlayer.fullRotation = player.fullRotation;
        drawPlayer.fullRotationOrigin = player.fullRotationOrigin;
        drawPlayer.selectedItem = player.selectedItem;
        drawPlayer.itemAnimation = player.itemAnimation;
        drawPlayer.itemAnimationMax = player.itemAnimationMax;
        drawPlayer.itemRotation = player.itemRotation;
        drawPlayer.heldProj = player.heldProj;
        drawPlayer.bodyFrame = player.bodyFrame;
        drawPlayer.legFrame = player.legFrame;
        drawPlayer.headFrame = player.headFrame;
        drawPlayer.wingFrame = player.wingFrame;
        drawPlayer.wings = player.wings;
        drawPlayer.gfxOffY = player.gfxOffY;

        drawPlayer.dead = false;
        drawPlayer.ghost = player.ghost || player.dead;
        if (drawPlayer.ghost)
        {
            drawPlayer.ghostFade = 1f;
            drawPlayer.ghostDir = 1;
        }

        drawPlayer.socialIgnoreLight = true;
        drawPlayer.isDisplayDollOrInanimate = false;

        return drawPlayer;
    }

    private static void DrawGhost(Camera camera, Player drawPlayer, Vector2 position, float scale)
    {
        byte mouseTextColor = Main.mouseTextColor;
        SpriteEffects effects = drawPlayer.direction == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        Color baseColor = new(mouseTextColor / 2 + 100, mouseTextColor / 2 + 100, mouseTextColor / 2 + 100, mouseTextColor / 2 + 100);
        Color color = drawPlayer.GetImmuneAlpha(baseColor, 0f);

        Rectangle frame = new(0, TextureAssets.Ghost.Height() / 4 * drawPlayer.ghostFrame, TextureAssets.Ghost.Width(), TextureAssets.Ghost.Height() / 4);
        Vector2 origin = new(frame.Width * 0.5f, frame.Height * 0.5f);
        Vector2 center = new(position.X - camera.UnscaledPosition.X + drawPlayer.width * scale * 0.5f, position.Y - camera.UnscaledPosition.Y + drawPlayer.height * scale * 0.5f);

        camera.SpriteBatch.Draw(TextureAssets.Ghost.Value, center, frame, color, 0f, origin, scale, effects, 0f);
    }

    public static Vector2 Dep_GetPlayerDrawPosition(Rectangle box, Player drawPlayer, float scale)
    {
        Vector2 centerOffset = new(0, 0);
        const float stableScale = 1.0f;
        const float driftXLinear = 10f;
        const float driftXQuad = 8f;
        const float driftYLinear = 34f;
        const float driftYQuad = 40f;

        float highScale = Math.Max(0f, scale - stableScale);
        Vector2 driftCorrection = new(
            driftXLinear * highScale + driftXQuad * highScale * highScale,
            driftYLinear * highScale + driftYQuad * highScale * highScale);

        Vector2 drawCenter = box.Center.ToVector2() + centerOffset * scale + driftCorrection;
        return new Vector2(
            (int)MathF.Round(drawCenter.X - drawPlayer.width * scale * 0.5f),
            (int)MathF.Round(drawCenter.Y - drawPlayer.height * scale * 0.5f));
    }

    #endregion

    #region Player head

    public static void DrawPlayerHead(SpriteBatch sb, Player player, Vector2 position, float scale = 1f)
    {
        Player drawPlayer = CreateHeadDrawPlayer(player);

        FullBrightDrawer.ForceFullBrightOnce = true;
        try
        {
            Main.PlayerRenderer.DrawPlayerHead(Main.Camera, drawPlayer, position, 1f, scale, Color.Transparent);
        }
        finally
        {
            FullBrightDrawer.ForceFullBrightOnce = false;
        }
    }


    private static Player CreateHeadDrawPlayer(Player player)
    {
        Player headPlayer = player.SerializedClone();
        headPlayer.dead = false;
        headPlayer.ghost = player.ghost || player.dead;

        if (headPlayer.ghost)
        {
            headPlayer.ghostFade = 1f;
            headPlayer.ghostDir = 1;
        }

        headPlayer.socialIgnoreLight = true;
        headPlayer.isDisplayDollOrInanimate = true;

        return headPlayer;
    }
    #endregion

}