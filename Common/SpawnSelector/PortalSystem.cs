using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Utilities;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.SpawnSelector;

/// <summary>
/// Manage all portals in the world.
/// </summary>
[Autoload(Side = ModSide.Client)]
public sealed class PortalSystem : ModSystem
{
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
    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int idx = layers.FindIndex(layer => layer.Name == "Vanilla: Interface Logic 1");
        if (idx != -1)
            layers.Insert(idx + 1, new PortalInterfaceLayer());
    }

    private sealed class PortalInterfaceLayer : GameInterfaceLayer
    {
        public PortalInterfaceLayer() : base("PvPAdventure: Portal", InterfaceScaleType.Game)
        {
        }

        protected override bool DrawSelf()
        {
            DrawAllPortals(Main.spriteBatch);
            return true;
        }
        private static void DrawAllPortals(SpriteBatch spriteBatch)
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

                if (!TryGetPortalWorldPos(player, out Vector2 worldPos))
                    continue;

                Vector2 drawPos = worldPos - Main.screenPosition;
                Color portalColor = GetPortalColor(player);

                //DrawPortalDustGlow(spriteBatch, drawPos - new Vector2(0f, source.Height - 6f), portalColor);
                //spriteBatch.Draw(texture, drawPos, source, Color.White, 0f, origin, 1f, SpriteEffects.None, 0f);

                DrawOutlinedPortal(spriteBatch, texture, drawPos, source, origin, portalColor);
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
