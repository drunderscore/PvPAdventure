using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Spectator.Visualization;
using PvPAdventure.Common.Visualization;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics;
using Terraria.ID;

namespace PvPAdventure.Common.Spectator.Drawers;

public static class EntityDrawer
{
    private static readonly RasterizerState ClippedCullNone = new()
    {
        CullMode = CullMode.None,
        ScissorTestEnable = true
    };

    private static readonly RasterizerState ClippedCullCounterClockwise = new()
    {
        CullMode = CullMode.CullCounterClockwiseFace,
        ScissorTestEnable = true
    };

    #region Entity Background Texture
    public static Texture2D EntityBackground => Main.Assets.Request<Texture2D>("Images/UI/PlayerBackground").Value;

    public static void DrawEntityBackground(SpriteBatch sb, Rectangle area)
    {
        DrawPlayerBackgroundSlice(sb, EntityBackground, area, Color.White);
    }

    private static void DrawPlayerBackgroundSlice(SpriteBatch sb, Texture2D texture, Rectangle area, Color color)
    {
        const int left = 6;
        const int right = 6;
        const int top = 6;
        const int bottom = 6;

        if (area.Width <= 0 || area.Height <= 0)
            return;

        int middleSourceWidth = texture.Width - left - right;
        int middleSourceHeight = texture.Height - top - bottom;
        int middleDestinationWidth = Math.Max(0, area.Width - left - right);
        int middleDestinationHeight = Math.Max(0, area.Height - top - bottom);

        DrawSlice(sb, texture, new Rectangle(0, 0, left, top), new Rectangle(area.X, area.Y, left, top), color);
        DrawSlice(sb, texture, new Rectangle(left, 0, middleSourceWidth, top), new Rectangle(area.X + left, area.Y, middleDestinationWidth, top), color);
        DrawSlice(sb, texture, new Rectangle(texture.Width - right, 0, right, top), new Rectangle(area.Right - right, area.Y, right, top), color);

        DrawSlice(sb, texture, new Rectangle(0, top, left, middleSourceHeight), new Rectangle(area.X, area.Y + top, left, middleDestinationHeight), color);
        DrawSlice(sb, texture, new Rectangle(left, top, middleSourceWidth, middleSourceHeight), new Rectangle(area.X + left, area.Y + top, middleDestinationWidth, middleDestinationHeight), color);
        DrawSlice(sb, texture, new Rectangle(texture.Width - right, top, right, middleSourceHeight), new Rectangle(area.Right - right, area.Y + top, right, middleDestinationHeight), color);

        DrawSlice(sb, texture, new Rectangle(0, texture.Height - bottom, left, bottom), new Rectangle(area.X, area.Bottom - bottom, left, bottom), color);
        DrawSlice(sb, texture, new Rectangle(left, texture.Height - bottom, middleSourceWidth, bottom), new Rectangle(area.X + left, area.Bottom - bottom, middleDestinationWidth, bottom), color);
        DrawSlice(sb, texture, new Rectangle(texture.Width - right, texture.Height - bottom, right, bottom), new Rectangle(area.Right - right, area.Bottom - bottom, right, bottom), color);
    }

    private static void DrawSlice(SpriteBatch sb, Texture2D texture, Rectangle source, Rectangle destination, Color color)
    {
        if (source.Width <= 0 || source.Height <= 0 || destination.Width <= 0 || destination.Height <= 0)
            return;

        sb.Draw(texture, destination, source, color);
    }
    #endregion

    #region Player
    public static void DrawPlayerPreview(SpriteBatch sb, Player player, Rectangle area)
    {
        const float bottomPadding = 5f;

        if (area.Width <= 0 || area.Height <= 0)
            return;

        Player drawPlayer = CreateFullDrawPlayer(player);

        float scale = Math.Min(area.Width / (drawPlayer.width + 4f), (area.Height - bottomPadding) / drawPlayer.height);
        scale = BoostSmallPreviewScale(scale, 1.3f);

        Vector2 feet = new(area.Center.X, area.Bottom - bottomPadding);
        Vector2 drawPos = new(
            (int)MathF.Round(feet.X - drawPlayer.width * scale * 0.5f),
            (int)MathF.Round(feet.Y - drawPlayer.height * scale + drawPlayer.gfxOffY * scale));

        DrawFullPlayer(sb, player, drawPos, scale);
    }

    public static void DrawFullPlayer(SpriteBatch sb, Player player, Vector2 position, float scale = 1f)
    {
        Player drawPlayer = CreateFullDrawPlayer(player);
        Rectangle oldScissor = sb.GraphicsDevice.ScissorRectangle;
        RasterizerState oldRasterizer = sb.GraphicsDevice.RasterizerState;

        sb.End();
        sb.GraphicsDevice.ScissorRectangle = oldScissor;
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, ClippedCullNone, null, Main.UIScaleMatrix);

        FullBrightPlayerDrawer.ForceFullBrightOnce = true;
        PlayerOutlines.ForcePreviewOutline = true;

        try
        {
            if (drawPlayer.ghost)
                DrawGhost(Main.Camera, drawPlayer, position + Main.screenPosition, scale);
            else
                Main.PlayerRenderer.DrawPlayer(Main.Camera, drawPlayer, position + Main.screenPosition, 0f, Vector2.Zero, 0f, scale);
        }
        finally
        {
            FullBrightPlayerDrawer.ForceFullBrightOnce = false;
            PlayerOutlines.ForcePreviewOutline = false;
        }

        sb.End();
        sb.GraphicsDevice.ScissorRectangle = oldScissor;
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, oldRasterizer, null, Main.UIScaleMatrix);
    }

    public static void DrawPlayerHead(SpriteBatch sb, Player player, Vector2 position, float scale = 1f)
    {
        Player drawPlayer = CreateHeadDrawPlayer(player);

        FullBrightPlayerDrawer.ForceFullBrightOnce = true;

        try
        {
            Main.PlayerRenderer.DrawPlayerHead(Main.Camera, drawPlayer, position, 1f, scale, Color.Transparent);
        }
        finally
        {
            FullBrightPlayerDrawer.ForceFullBrightOnce = false;
        }
    }

    public static string DrawPlayerHeadStat(SpriteBatch sb, Rectangle area, Player player)
    {
        Rectangle textArea = new(area.X + 30, area.Y + 3, area.Width - 30, area.Height - 8);
        string text = StatDrawer.Truncate(FontAssets.MouseText.Value, player.name, textArea.Width, 0.8f);

        if (text == "..")
            text = "";

        if (text.Length > 0)
            StatDrawer.DrawBack(sb, area);

        Rectangle oldScissor = sb.GraphicsDevice.ScissorRectangle;
        RasterizerState oldRasterizer = sb.GraphicsDevice.RasterizerState;

        sb.End();
        sb.GraphicsDevice.ScissorRectangle = oldScissor;
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, ClippedCullCounterClockwise, null, Main.UIScaleMatrix);

        Rectangle headBox = new(area.X + 2, area.Y - 2, 16, 16);
        DrawPlayerHead(sb, player, new Vector2(headBox.X + headBox.Width * 0.5f, headBox.Y + headBox.Height * 0.5f), 0.85f);

        sb.End();
        sb.GraphicsDevice.ScissorRectangle = oldScissor;
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, oldRasterizer, null, Main.UIScaleMatrix);

        Utils.DrawBorderString(sb, text, new Vector2(textArea.X, textArea.Y), Color.White, 1f);
        return area.Contains(Main.MouseScreen.ToPoint()) ? $"Player: {player.name}" : null;
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
        drawPlayer.ghost = (player.ghost || player.dead) && DisableGhostDrawSystem.ShouldDrawGhost(player);

        if (drawPlayer.ghost)
        {
            drawPlayer.ghostFade = 1f;
            drawPlayer.ghostDir = 1;
        }

        drawPlayer.socialIgnoreLight = true;
        drawPlayer.isDisplayDollOrInanimate = false;

        return drawPlayer;
    }

    private static Player CreateHeadDrawPlayer(Player player)
    {
        Player headPlayer = player.SerializedClone();
        headPlayer.dead = false;
        headPlayer.ghost = (player.ghost || player.dead) && DisableGhostDrawSystem.ShouldDrawGhost(player);

        if (headPlayer.ghost)
        {
            headPlayer.ghostFade = 1f;
            headPlayer.ghostDir = 1;
        }

        headPlayer.socialIgnoreLight = true;
        headPlayer.isDisplayDollOrInanimate = true;

        return headPlayer;
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
    #endregion

    #region NPC
    public static void DrawNPCPreview(SpriteBatch sb, NPC npc, Rectangle area)
    {
        const float bottomPadding = 5f;

        if (npc == null || area.Width <= 0 || area.Height <= 0 || npc.type <= NPCID.None)
            return;

        Texture2D texture = TextureAssets.Npc[npc.type].Value;
        Rectangle source = npc.frame.Width > 0 && npc.frame.Height > 0 ? npc.frame : texture.Frame();

        float scale = Math.Min((area.Width - 4f) / source.Width, (area.Height - bottomPadding) / source.Height);
        scale = BoostSmallPreviewScale(scale, 1.45f);

        Vector2 position = new(area.Center.X, area.Bottom - bottomPadding - source.Height * scale * 0.5f);
        SpriteEffects effects = npc.spriteDirection >= 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        Color color = npc.GetAlpha(Color.White);

        sb.Draw(texture, position, source, color, 0f, source.Size() * 0.5f, scale, effects, 0f);
    }

    public static void DrawNPCHead(SpriteBatch sb, NPC npc, Rectangle area)
    {
        int bossHeadId = npc.type >= NPCID.None && npc.type < NPCID.Sets.BossHeadTextures.Length ? NPCID.Sets.BossHeadTextures[npc.type] : -1;

        if (bossHeadId >= 0)
        {
            Main.BossNPCHeadRenderer.DrawWithOutlines(null, bossHeadId, area.Center.ToVector2(), Color.White, 0f, 0.52f, SpriteEffects.None);
            return;
        }

        Texture2D texture = TextureAssets.Npc[npc.type].Value;
        Rectangle source = npc.frame.Width > 0 && npc.frame.Height > 0 ? npc.frame : texture.Frame();
        float scale = Math.Min(area.Width / (float)source.Width, area.Height / (float)source.Height);

        sb.Draw(texture, area.Center.ToVector2(), source, Color.White, 0f, source.Size() * 0.5f, scale, npc.spriteDirection >= 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0f);
    }

    public static string DrawNPCHeadStat(SpriteBatch sb, Rectangle area, NPC npc)
    {
        Rectangle textArea = new(area.X + 30, area.Y + 3, area.Width - 30, area.Height - 8);
        string text = StatDrawer.Truncate(FontAssets.MouseText.Value, npc.FullName, textArea.Width, 0.8f);

        if (text == "..")
            text = "";

        if (text.Length > 0)
            StatDrawer.DrawBack(sb, area);

        DrawNPCHead(sb, npc, new Rectangle(area.X + 3, area.Y + 3, 18, 18));
        Utils.DrawBorderString(sb, text, new Vector2(textArea.X, textArea.Y), Color.White, 1f);

        return area.Contains(Main.MouseScreen.ToPoint()) ? $"NPC: {npc.FullName}" : null;
    }
    #endregion

    #region Helpers
    private static float BoostSmallPreviewScale(float scale, float maxScale)
    {
        float boost = MathHelper.Lerp(2f, 1f, MathHelper.Clamp(scale, 0f, 1f));
        return MathHelper.Clamp(scale * boost, 0.15f, maxScale);
    }
    #endregion
}