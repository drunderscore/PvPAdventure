using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace PvPAdventure.Common.Spectator.Drawers;

internal static class NPCDrawer
{
    public static void DrawFullNPC(SpriteBatch sb, NPC npc, Rectangle box)
    {
        const float nameScale = 1f;
        const float nameTop = 2f;

        sb.Draw(Main.Assets.Request<Texture2D>("Images/UI/PlayerBackground").Value, box, Color.White);

        string name = StatDrawer.Truncate(FontAssets.MouseText.Value, npc.FullName, box.Width - 8, nameScale);
        Vector2 nameSize = FontAssets.MouseText.Value.MeasureString(name) * nameScale;
        Vector2 namePos = new((int)MathF.Round(box.Center.X - nameSize.X * 0.5f), box.Y + nameTop);

        Texture2D texture = TextureAssets.Npc[npc.type].Value;
        Rectangle source = npc.frame;
        if (source.Width <= 0 || source.Height <= 0)
            source = texture.Frame();

        Rectangle drawArea = new(box.X + 3, box.Y + 3, box.Width - 6, box.Height - 6);
        float fitScale = Math.Min((drawArea.Width - 2f) / source.Width, (drawArea.Height - 8f) / source.Height);
        fitScale = Math.Max(0.1f, fitScale);

        Vector2 origin = source.Size() * 0.5f;
        Vector2 drawCenter = drawArea.Center.ToVector2() + new Vector2(0f, 8f);
        SpriteEffects effects = npc.spriteDirection >= 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

        sb.Draw(texture, drawCenter, source, Lighting.GetColor(npc.Center.ToTileCoordinates()), 0f, origin, fitScale, effects, 0f);

        sb.End();
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);

        Utils.DrawBorderString(sb, name, namePos, Color.White, nameScale);

        sb.End();
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);
    }

    public static void DrawNPCHead(SpriteBatch sb, NPC npc, Rectangle area)
    {
        int bossHeadId = npc.type >= 0 && npc.type < NPCID.Sets.BossHeadTextures.Length ? NPCID.Sets.BossHeadTextures[npc.type] : -1;
        if (bossHeadId >= 0)
        {
            Main.BossNPCHeadRenderer.DrawWithOutlines(null, bossHeadId, area.Center.ToVector2(), Color.White, 0f, 0.52f, SpriteEffects.None);
            return;
        }

        Texture2D texture = TextureAssets.Npc[npc.type].Value;
        Rectangle source = npc.frame;
        if (source.Width <= 0 || source.Height <= 0)
            source = texture.Frame();

        float scale = Math.Min(area.Width / (float)source.Width, area.Height / (float)source.Height);
        Vector2 origin = source.Size() * 0.5f;
        sb.Draw(texture, area.Center.ToVector2(), source, Color.White, 0f, origin, scale, npc.spriteDirection >= 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0f);
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
}