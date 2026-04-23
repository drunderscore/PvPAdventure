using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.SpawnSelector;

/// <summary>
/// Manage all portals in the world.
/// </summary>
[Autoload(Side = ModSide.Client)]
public sealed class PortalSystem : ModSystem
{
    public const int PortalMaxHealth = 27;

    public static bool HasPortal(Player player)
    {
        return SpawnPlayer.HasPortal(player);
    }

    public static bool TryGetPortalWorldPos(Player player, out Vector2 worldPos)
    {
        return SpawnPlayer.TryGetPortalWorldPos(player, out worldPos);
    }

    public static void CreatePortalAtPosition(Player player, Vector2 position)
    {
        if (player == null || !player.active)
            return;

        player.GetModPlayer<SpawnPlayer>().SetPortal(position);
    }

    public static void ClearPortal(Player player)
    {
        if (player == null || !player.active)
            return;

        player.GetModPlayer<SpawnPlayer>().ClearPortal();
    }

    public static bool TryDamagePortal(Player attacker, int ownerIndex, int damage, string source)
    {
        if (ownerIndex < 0 || ownerIndex >= Main.maxPlayers)
            return false;

        Player owner = Main.player[ownerIndex];
        if (owner == null || !owner.active)
            return false;

        return owner.GetModPlayer<SpawnPlayer>().DamagePortal(attacker, damage, source);
    }

    public static Rectangle GetPortalHitbox(Vector2 worldPos)
    {
        return new Rectangle((int)worldPos.X - 24, (int)worldPos.Y - 72, 48, 72);
    }

    public static void PlayPortalFx(Vector2 worldPos, bool killed, int damage = 0)
    {
        if (Main.dedServ)
            return;

        if (damage > 0)
            CombatText.NewText(GetPortalHitbox(worldPos), CombatText.DamagedHostile, damage);

        if (!killed)
        {
            SoundEngine.PlaySound(SoundID.NPCHit4, worldPos);
            return;
        }

        SoundEngine.PlaySound(SoundID.NPCDeath6, worldPos);

        for (int i = 0; i < 28; i++)
        {
            Vector2 velocity = Main.rand.NextVector2Circular(3.5f, 3.5f);
            Dust.NewDustPerfect(worldPos + Main.rand.NextVector2Circular(24f, 36f), DustID.MagicMirror, velocity, 120, Color.White, Main.rand.NextFloat(1.1f, 1.8f));
        }
    }

    public static Color GetPortalColor(Player player)
    {
        if (player == null || player.team <= 0 || player.team >= Main.teamColor.Length)
            return Color.White;

        return Main.teamColor[player.team];
    }

    public static void DrawOutlinedPortalIcon(SpriteBatch sb, Texture2D texture, Vector2 position, float scale, Color borderColor)
    {
        Vector2 origin = texture.Size() * 0.5f;
        DrawTextureOutline(sb, texture, position, origin, scale, Color.Black * 0.9f, 3f);
        DrawTextureOutline(sb, texture, position, origin, scale, borderColor * 0.9f, 1.5f);
        sb.Draw(texture, position, null, Color.White, 0f, origin, scale, SpriteEffects.None, 0f);
    }

    private static void DrawTextureOutline(SpriteBatch sb, Texture2D texture, Vector2 position, Vector2 origin, float scale, Color color, float distance)
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
            sb.Draw(texture, position + offsets[i], null, color, 0f, origin, scale, SpriteEffects.None, 0f);
    }

    #region Clear hooks on load
    public override void OnWorldLoad()
    {
        ClearAllPortals();
    }

    public override void OnWorldUnload()
    {
        ClearAllPortals();
    }

    private static void ClearAllPortals()
    {
        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player player = Main.player[i];
            if (player == null || !player.active)
                continue;

            player.GetModPlayer<SpawnPlayer>().ClearPortal(sync: false);
        }
    }
    #endregion

    #region Drawing
    public override void PostDrawTiles()
    {
        if (Main.dedServ)
            return;

        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
        PortalDrawer.DrawAllPortals(Main.spriteBatch);
        Main.spriteBatch.End();
    }

    private static class PortalDrawer
    {
        public static void DrawAllPortals(SpriteBatch spriteBatch)
        {
            if (Main.dedServ)
                return;

            Texture2D texture = Ass.Portal.Value;
            const int frameCount = 8;
            int frameHeight = texture.Height / frameCount;
            int frame = (int)(Main.GameUpdateCount / 5 % frameCount);
            Rectangle source = new(0, frame * frameHeight, texture.Width, frameHeight);
            Vector2 origin = new(source.Width * 0.5f, source.Height);

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player == null || !player.active)
                    continue;

                if (!SpawnPlayer.TryGetPortal(player, out Vector2 worldPos, out int health))
                    continue;

                Vector2 drawPos = worldPos - Main.screenPosition;
                Color portalColor = GetPortalColor(player);

                //DrawPortalDustGlow(spriteBatch, drawPos - new Vector2(0f, source.Height - 6f), portalColor);
                //spriteBatch.Draw(texture, drawPos, source, Color.White, 0f, origin, 1f, SpriteEffects.None, 0f);

                DrawOutlinedPortal(spriteBatch, texture, drawPos, source, origin, portalColor);
                DrawHealthBar(spriteBatch, worldPos + new Vector2(0f, 8f), health, PortalMaxHealth, 1f, 1f);
                DrawHoverText(player, worldPos, health);
                //DrawPortalDustGlow(spriteBatch, drawPos - new Vector2(0f, source.Height - 5f), GetPortalColor(player));

#if DEBUG
                //string text = $"{player.name} [{((Terraria.Enums.Team)player.team)}]";
                string text = $"{((Terraria.Enums.Team)player.team)} Team portal by {player.name}";
                Vector2 size = FontAssets.MouseText.Value.MeasureString(text);
                //Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value, text, drawPos.X - size.X * 0.5f, drawPos.Y - source.Height - 18f, portalColor, Color.Black, Vector2.Zero);
#endif
            }
        }

        private static void DrawOutlinedPortal(SpriteBatch sb, Texture2D texture, Vector2 position, Rectangle source, Vector2 origin, Color borderColor)
        {
            DrawTextureOutline(sb, texture, position, source, origin, Color.Black * 0.9f, 3f);
            DrawTextureOutline(sb, texture, position, source, origin, borderColor * 0.9f, 1.5f);
            sb.Draw(texture, position, source, Color.White, 0f, origin, 1f, SpriteEffects.None, 0f);
        }

        private static void DrawHealthBar(SpriteBatch sb, Vector2 worldPos, int health, int maxHealth, float scale, float alpha)
        {
            if (health <= 0 || health >= maxHealth)
                return;

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
            }
            else
            {
                if (barPixels < 36)
                    sb.Draw(backTex, screenOrigin + new Vector2(barPixels * scale, 0f), new Rectangle(barPixels, 0, 36 - barPixels, backTex.Height), barColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

                sb.Draw(fillTex, screenOrigin, new Rectangle(0, 0, barPixels, fillTex.Height), barColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
        }

        private static void DrawHoverText(Player player, Vector2 worldPos, int health)
        {
            if (!GetPortalHitbox(worldPos).Contains(Main.MouseWorld.ToPoint()))
                return;

            Main.instance.MouseText($"{(Terraria.Enums.Team)player.team} team portal: {health}/{PortalMaxHealth}");
        }

        private static void DrawTextureOutline(SpriteBatch sb, Texture2D texture, Vector2 position, Rectangle source, Vector2 origin, Color color, float distance)
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
                sb.Draw(texture, position + offsets[i], source, color, 0f, origin, 1f, SpriteEffects.None, 0f);
        }

        private static void DrawPortalDustGlow(SpriteBatch sb, Vector2 center, Color color)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;

            // Calculate a pulsing effect for the glow
            float pulse = 0.55f + (float)System.Math.Sin(Main.GameUpdateCount * 0.08f) * 0.15f;
            Color glow = color * pulse;

            // Draw the central cross and side dust bits
            DrawDust(sb, pixel, center + new Vector2(0f, -5f), 4f, glow);
            DrawDust(sb, pixel, center + new Vector2(-8f, 0f), 3f, glow * 0.8f);
            DrawDust(sb, pixel, center + new Vector2(8f, 0f), 3f, glow * 0.8f);
        }

        private static void DrawDust(SpriteBatch sb, Texture2D pixel, Vector2 pos, float size, Color color)
        {
            // Draw horizontal line
            sb.Draw(pixel, new Rectangle((int)(pos.X - size), (int)pos.Y, (int)(size * 2f), 1), color);

            // Draw vertical line
            sb.Draw(pixel, new Rectangle((int)pos.X, (int)(pos.Y - size), 1, (int)(size * 2f)), color);
        }
    }

    #endregion
}
