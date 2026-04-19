using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Debug;

#if DEBUG
internal static class DebugDrawer
{
    private readonly record struct DebugButton(string Label, Func<bool> IsEnabled, Action Toggle);

    private static readonly List<(Rectangle rect, Color color)> Rectangles = [];
    private static readonly List<(string text, Vector2 pos, Color color)> Texts = [];
    internal static bool ShowDebugStats { get; private set; } = true;
    internal static bool ShowChat { get; private set; } = true;

    internal static void DrawText(string content, Vector2 position, Color? color = null)
    {
        Texts.Add((content, position, color ?? Color.White));
    }

    internal static void DrawButtons()
    {

        Texture2D back = Main.Assets.Request<Texture2D>("Images/UI/CharCreation/SmallPanel").Value;
        Texture2D border = Main.Assets.Request<Texture2D>("Images/UI/CharCreation/SmallPanelBorder").Value;
        Texture2D highlight = Main.Assets.Request<Texture2D>("Images/UI/CharCreation/CategoryPanelHighlight").Value;

        (string text, string tooltip, Func<bool> enabled, Action toggle)[] buttons =
        [
            ("SS", "Show debug stats", () => ShowDebugStats, () => ShowDebugStats = !ShowDebugStats),
            ("CH", "Show chat", () => ShowChat, () => ShowChat = !ShowChat)
        ];

        for (int i = 0; i < buttons.Length; i++)
        {
            Rectangle rect = new(Main.screenWidth - 400, 14 + i * (back.Height + 6), back.Width, back.Height);
            bool hovered = rect.Contains(Main.MouseScreen.ToPoint());

            if (hovered)
            {
                Main.LocalPlayer.mouseInterface = true;
                Main.instance.MouseText(buttons[i].tooltip);
            }

            if (hovered && Main.mouseLeft && Main.mouseLeftRelease)
            {
                buttons[i].toggle();
                Main.mouseLeftRelease = false;
            }

            Vector2 center = rect.Center.ToVector2();
            Color stateColor = buttons[i].enabled() ? new Color(70, 145, 90) : new Color(145, 70, 70);

            Main.spriteBatch.Draw(back, center, null, Color.White * (hovered ? 1f : 0.85f), 0f, back.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            //Main.spriteBatch.Draw(highlight, center, null, stateColor * 0.7f, 0f, highlight.Size() * 0.5f, 1f, SpriteEffects.None, 0f);

            if (hovered)
            {
                Main.spriteBatch.Draw(border, center, null, Color.White, 0f, border.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            }

            Vector2 size = FontAssets.MouseText.Value.MeasureString(buttons[i].text) * 0.8f;
            DrawText(buttons[i].text, new Vector2(rect.Center.X - size.X * 0.5f, rect.Center.Y - size.Y * 0.5f), Color.White);
        }
    }

    internal static void DrawDebugInfo()
    {
        Player player = Main.LocalPlayer;
        //BlazeBatPlayer state = player.GetModPlayer<BlazeBatPlayer>();
        //bool hasBall = ProjectileLookupHelper.TryGetActive(player, out Projectile projectile);
        //float meterLeft = Main.screenWidth * 0.5f - 140f;
        //Vector2 column1Pos = new(meterLeft + 292, 6f);
        //Vector2 column2Pos = column1Pos + new Vector2(160, 0);
        Vector2 column1Pos = new(10, 600f);

        if (ShowDebugStats)
        {
            DrawColumn("Debug Stats:",
            [
                //$"Special: {state.SpecialMeter:0.00}",
            ], column1Pos);
        }
    }

    internal static void Flush(SpriteBatch sb)
    {
        Texture2D pixel = TextureAssets.MagicPixel.Value;

        foreach ((Rectangle rect, Color color) in Rectangles)
        {
            Rectangle screenRect = new(rect.X - (int)Main.screenPosition.X, rect.Y - (int)Main.screenPosition.Y, rect.Width, rect.Height);

            // Draw fill
            sb.Draw(pixel, screenRect, color * 0.7f);

            // Draw outline
            Color black = Color.Black;
            sb.Draw(pixel, new Rectangle(screenRect.X, screenRect.Y, screenRect.Width, 1), black);
            sb.Draw(pixel, new Rectangle(screenRect.X, screenRect.Y, 1, screenRect.Height), black);
            sb.Draw(pixel, new Rectangle(screenRect.X, screenRect.Bottom - 1, screenRect.Width, 1), black);
            sb.Draw(pixel, new Rectangle(screenRect.Right - 1, screenRect.Y, 1, screenRect.Height), black);
        }

        foreach ((string text, Vector2 pos, Color color) in Texts)
        {
            Utils.DrawBorderString(sb, text, pos, color, 0.8f);
        }

        Rectangles.Clear();
        Texts.Clear();
    }

    private static void DrawColumn(string header, IEnumerable<string> rows, Vector2 origin)
    {
        DrawText(header, origin, Color.Yellow);

        int i = 0;
        foreach (string row in rows)
        {
            DrawText(row, origin + new Vector2(0f, 22f + i++ * 18f));
        }
    }
}
#endif
