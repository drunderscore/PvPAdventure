using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Utilities;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;

namespace PvPAdventure.Common.SpawnSelector;

public static class PortalDrawer
{
    public static Color GetPortalColor(Player player)
    {
        if (player == null || player.team <= 0 || player.team >= Main.teamColor.Length)
            return Color.White;

        return Main.teamColor[player.team];
    }

    public static Asset<Texture2D> GetPortalAsset(Player player)
    {
        if (player == null)
            return Ass.Portal;

        return player.team switch
        {
            (int)Terraria.Enums.Team.Red => Ass.Portal_Red,
            (int)Terraria.Enums.Team.Green => Ass.Portal_Green,
            (int)Terraria.Enums.Team.Blue => Ass.Portal_Blue,
            (int)Terraria.Enums.Team.Yellow => Ass.Portal_Yellow,
            (int)Terraria.Enums.Team.Pink => Ass.Portal_Pink,
            _ => Ass.Portal,
        };
    }

    public static Asset<Texture2D> GetPortalMinimapAsset(Player player)
    {
        if (player == null)
            return Ass.Portal_Minimap;

        return player.team switch
        {
            (int)Terraria.Enums.Team.Red => Ass.Portal_Red_Minimap,
            (int)Terraria.Enums.Team.Green => Ass.Portal_Green_Minimap,
            (int)Terraria.Enums.Team.Blue => Ass.Portal_Blue_Minimap,
            (int)Terraria.Enums.Team.Yellow => Ass.Portal_Yellow_Minimap,
            (int)Terraria.Enums.Team.Pink => Ass.Portal_Pink_Minimap,
            _ => Ass.Portal_Minimap,
        };
    }

    public static Rectangle GetPortalFrameRectangle(Texture2D texture, int frameCount = 8)
    {
        int frame = (int)(Main.GameUpdateCount / 5 % frameCount);
        int frameHeight = texture.Height / frameCount;
        return new Rectangle(0, frame * frameHeight, texture.Width, frameHeight);
    }

    public static void DrawAllPortals(SpriteBatch spriteBatch)
    {
        if (Main.dedServ)
            return;

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player player = Main.player[i];
            if (player == null || !player.active)
                continue;

            if (!SpawnPlayer.TryGetPortal(player, out Vector2 worldPos, out int health))
                continue;

            Texture2D texture = GetPortalAsset(player).Value;
            Rectangle source = GetPortalFrameRectangle(texture);
            Vector2 origin = new(source.Width * 0.5f, source.Height);
            Vector2 drawPos = worldPos - Main.screenPosition;
            Color borderColor = GetPortalColor(player);

            DrawPortal(spriteBatch, texture, drawPos, source, origin, 1f, Color.White, borderColor, outline: true);
            DrawPortalHealthBar(spriteBatch, worldPos + new Vector2(0f, 8f), health, PortalSystem.PortalMaxHealth, 1f, 1f);
            DrawHoverText(player, worldPos, health);

#if DEBUG
            string text = $"{(Terraria.Enums.Team)player.team} Team portal by {player.name}";
            // Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value, text, drawPos.X, drawPos.Y, borderColor, Color.Black, Vector2.Zero);
#endif
        }
    }

    public static void DrawPortalPreview(SpriteBatch sb, Player player, Vector2 position, float scale, bool outline = true, Color drawColor = default, float blackOutlineDistance = 4f, float colorOutlineDistance = 3f)
    {
        Texture2D texture = GetPortalAsset(player).Value;
        Rectangle source = GetPortalFrameRectangle(texture);
        Vector2 origin = source.Size() * 0.5f;
        Color borderColor = GetPortalColor(player);

        if (drawColor == default)
            drawColor = Color.White;

        DrawPortal(sb, texture, position, source, origin, scale, drawColor, borderColor, outline, blackOutlineDistance, colorOutlineDistance);
    }

    public static void DrawPortal(SpriteBatch sb, Texture2D texture, Vector2 position, Rectangle source, Vector2 origin, float scale, Color drawColor, Color borderColor, bool outline, float blackOutlineDistance = 4f, float colorOutlineDistance = 3f)
    {
        if (outline)
        {
            float alphaScale = 0.9f * drawColor.A / 255f;
            DrawTextureOutline(sb, texture, position, source, origin, Color.Black * alphaScale, scale, blackOutlineDistance);
            DrawTextureOutline(sb, texture, position, source, origin, borderColor * alphaScale, scale, colorOutlineDistance);
        }

        sb.Draw(texture, position, source, drawColor, 0f, origin, scale, SpriteEffects.None, 0f);
    }

    private static void DrawPortalHealthBar(SpriteBatch sb, Vector2 worldPos, int health, int maxHealth, float scale, float alpha)
    {
        float healthRatio = (float)health / maxHealth;
        if (healthRatio > 1f)
            healthRatio = 1f;

        int barPixels = (int)(36f * healthRatio);
        if (barPixels < 3)
            barPixels = 3;

        healthRatio -= 0.1f;
        float green = healthRatio > 0.5f ? 255f : 255f * healthRatio * 2f;
        float red = healthRatio > 0.5f ? 255f * (1f - healthRatio) * 2f : 255f;
        float colorScale = alpha * 0.95f;

        red = MathHelper.Clamp(red * colorScale, 0f, 255f);
        green = MathHelper.Clamp(green * colorScale, 0f, 255f);
        float alphaByte = MathHelper.Clamp(255f * colorScale, 0f, 255f);
        Color barColor = new((byte)red, (byte)green, 0, (byte)alphaByte);

        Vector2 screenOrigin = new(worldPos.X - 18f * scale - Main.screenPosition.X, worldPos.Y - Main.screenPosition.Y);
        Texture2D backTex = TextureAssets.Hb2.Value;
        Texture2D fillTex = TextureAssets.Hb1.Value;

        if (barPixels < 34)
        {
            if (barPixels < 36)
                sb.Draw(backTex, screenOrigin + new Vector2(barPixels * scale, 0f), new Rectangle(2, 0, 2, backTex.Height), barColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

            if (barPixels < 34)
                sb.Draw(backTex, screenOrigin + new Vector2((barPixels + 2) * scale, 0f), new Rectangle(barPixels + 2, 0, 36 - barPixels - 2, backTex.Height), barColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

            if (barPixels > 2)
                sb.Draw(fillTex, screenOrigin, new Rectangle(0, 0, barPixels - 2, fillTex.Height), barColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

            sb.Draw(fillTex, screenOrigin + new Vector2((barPixels - 2) * scale, 0f), new Rectangle(32, 0, 2, fillTex.Height), barColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            return;
        }

        if (barPixels < 36)
            sb.Draw(backTex, screenOrigin + new Vector2(barPixels * scale, 0f), new Rectangle(barPixels, 0, 36 - barPixels, backTex.Height), barColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

        sb.Draw(fillTex, screenOrigin, new Rectangle(0, 0, barPixels, fillTex.Height), barColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    private static void DrawHoverText(Player player, Vector2 worldPos, int health)
    {
        if (!PortalSystem.GetPortalHitbox(worldPos).Contains(Main.MouseWorld.ToPoint()))
            return;

        Main.instance.MouseText($"{(Terraria.Enums.Team)player.team} Team Portal: {health}/{PortalSystem.PortalMaxHealth}");
    }

    private static void DrawTextureOutline(SpriteBatch sb, Texture2D texture, Vector2 position, Rectangle source, Vector2 origin, Color color, float scale, float distance)
    {
        Vector2[] offsets =
        [
            new Vector2(-distance, 0f),
            new Vector2(distance, 0f),
            new Vector2(0f, -distance),
            new Vector2(0f, distance),
            new Vector2(-distance, -distance),
            new Vector2(-distance, distance),
            new Vector2(distance, -distance),
            new Vector2(distance, distance)
        ];

        for (int i = 0; i < offsets.Length; i++)
            sb.Draw(texture, position + offsets[i], source, color, 0f, origin, scale, SpriteEffects.None, 0f);
    }
}