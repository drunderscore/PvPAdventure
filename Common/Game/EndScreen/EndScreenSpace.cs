using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.Utilities;

namespace PvPAdventure.Common.Game.EndScreen;

/// <summary>ZensSky-style generated star state for the end screen.</summary>
public static class EndScreenStarSystem
{
    private const float CircularRadius = 2400f;
    private const float GameDayRateDivisor = 70000f;
    private const int DefaultStarGenerationSeed = 100;

    public const int StarCount = 1200;
    public static readonly EndScreenStar[] Stars = new EndScreenStar[StarCount];

    public static float StarRotation { get; private set; }

    static EndScreenStarSystem()
    {
        GenerateStars();
    }

    public static void GenerateStars(int seed = DefaultStarGenerationSeed)
    {
        if (Main.dedServ)
        {
            Array.Clear(Stars);
            return;
        }

        UnifiedRandom rand = new(seed);
        StarRotation = 0f;

        for (int i = 0; i < StarCount; i++)
            Stars[i] = new EndScreenStar(rand, CircularRadius);
    }

    public static void UpdateStars()
    {
        float dayRate = Math.Max(1f, (float)Main.dayRate);
        StarRotation += dayRate / GameDayRateDivisor;
        StarRotation %= MathHelper.TwoPi;

    }
}

/// <summary>Draws ZensSky-style rotating stars for the end screen backdrop.</summary>
public static class EndScreenStarRendering
{
    public static void DrawStarsToSky(SpriteBatch spriteBatch, float alpha, Rectangle area)
    {
        if (alpha <= 0f)
            return;

        float rotation = -EndScreenStarSystem.StarRotation;
        Vector2 center = new(area.X + area.Width * 0.5f, area.Y + area.Height * 0.5f);
        EndScreenStar[] stars = EndScreenStarSystem.Stars;

        for (int i = 0; i < stars.Length; i++)
            DrawStar(spriteBatch, alpha, rotation, center, stars[i], (EndScreenStarVisual)(i % 4));
    }

    public static void DrawStar(SpriteBatch spriteBatch, float alpha, float rotation, Vector2 center, EndScreenStar star, EndScreenStarVisual style)
    {
        if (!star.IsActive)
            return;

        Vector2 position = center + star.RotatedPosition(rotation);

        switch (style)
        {
            case EndScreenStarVisual.Vanilla:
                star.DrawVanilla(spriteBatch, position, alpha);
                return;
            case EndScreenStarVisual.Diamond:
                star.DrawDiamond(spriteBatch, TextureAssets.Star[0].Value, position, alpha, rotation);
                return;
            case EndScreenStarVisual.FourPointed:
                star.DrawFlare(spriteBatch, TextureAssets.Star[1].Value, position, alpha, rotation);
                return;
            default:
                star.DrawCircle(spriteBatch, TextureAssets.Star[3].Value, position, alpha, rotation);
                return;
        }
    }
}

/// <summary>One generated end-screen star.</summary>
public struct EndScreenStar
{
    public Vector2 Position;
    public Color Color;
    public float Scale;
    public float Opacity;
    public float Twinkle;
    public bool IsActive;

    public EndScreenStar(UnifiedRandom rand, float radius)
    {
        float angle = rand.NextFloat() * MathHelper.TwoPi;
        float distance = MathF.Sqrt(rand.NextFloat()) * radius;

        Position = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;
        Color = rand.Next(5) == 0 ? new Color(176, 118, 255) : Color.White;
        Scale = rand.NextFloat(0.55f, 1.75f);
        Opacity = rand.NextFloat(0.42f, 1f);
        Twinkle = rand.NextFloat() * MathHelper.TwoPi;
        IsActive = true;
    }

    public Vector2 RotatedPosition(float rotation)
    {
        float sin = MathF.Sin(rotation);
        float cos = MathF.Cos(rotation);

        return new Vector2(Position.X * cos - Position.Y * sin, Position.X * sin + Position.Y * cos);
    }

    public void DrawVanilla(SpriteBatch spriteBatch, Vector2 position, float alpha)
    {
        Texture2D pixel = TextureAssets.MagicPixel.Value;
        float twinkle = 0.72f + 0.28f * MathF.Sin(Main.GlobalTimeWrappedHourly * 2.4f + Twinkle);
        int size = Scale > 1.35f ? 2 : 1;
        Rectangle rect = new((int)position.X, (int)position.Y, size, size);

        spriteBatch.Draw(pixel, rect, Color * (Opacity * twinkle * alpha));
    }

    public void DrawDiamond(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, float alpha, float rotation)
    {
        DrawTexture(spriteBatch, texture, position, alpha, rotation + MathHelper.PiOver4, Scale * 0.09f);
    }

    public void DrawFlare(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, float alpha, float rotation)
    {
        DrawTexture(spriteBatch, texture, position, alpha, rotation, Scale * 0.12f);
    }

    public void DrawCircle(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, float alpha, float rotation)
    {
        DrawTexture(spriteBatch, texture, position, alpha, rotation, Scale * 0.08f);
    }

    private void DrawTexture(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, float alpha, float rotation, float scale)
    {
        float twinkle = 0.58f + 0.42f * MathF.Sin(Main.GlobalTimeWrappedHourly * 2.8f + Twinkle);
        Color color = Color * (Opacity * twinkle * alpha);
        color.A = 0;

        spriteBatch.Draw(texture, position, null, color, rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
    }
}

/// <summary>Star visual variants copied from the ZensSky renderer shape.</summary>
public enum EndScreenStarVisual
{
    Vanilla,
    Diamond,
    FourPointed,
    OuterWilds
}
