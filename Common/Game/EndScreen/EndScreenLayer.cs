using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace PvPAdventure.Common.Game.EndScreen;

/// <summary>Draws the blurred world backdrop and sharp stars behind the summary.</summary>
public class EndScreenBackdropLayer : GameInterfaceLayer
{
    private static readonly GlassPanelStyle HeavyBlur = new(new Color(10, 4, 28), new Color(92, 38, 162), new Color(150, 88, 230), 0.92f, 26f, 3.1f, 0.45f, 0.8f);
    private readonly EndScreenSystem system;

    public EndScreenBackdropLayer(EndScreenSystem system)
        : base("PvPAdventure: End Screen Backdrop", InterfaceScaleType.None)
    {
        this.system = system;
    }

    protected override bool DrawSelf()
    {
        if (!system.IsVisible)
            return true;

        Rectangle screen = new(0, 0, Main.screenWidth, Main.screenHeight);
        DrawHeavyBlur(Main.spriteBatch, screen, system.Opacity);
        EndScreenStarRendering.DrawStarsToSky(Main.spriteBatch, system.Opacity, screen);
        return true;
    }

    private static void DrawHeavyBlur(SpriteBatch spriteBatch, Rectangle screen, float opacity)
    {
        if (opacity <= 0f)
            return;

        if (!EffectLoader.TryGetLiquidGlassEffect(out Effect effect))
        {
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, screen, HeavyBlur.Primary * (0.72f * opacity));
            return;
        }

        Texture2D backdrop = Main.screenTarget ?? TextureAssets.MagicPixel.Value;
        effect.Parameters["uBackdropTexture"]?.SetValue(backdrop);
        effect.Parameters["uColor"]?.SetValue(HeavyBlur.Primary.ToVector3());
        effect.Parameters["uSecondaryColor"]?.SetValue(HeavyBlur.Secondary.ToVector3());
        effect.Parameters["uBorderColor"]?.SetValue(HeavyBlur.Border.ToVector3());
        effect.Parameters["uOpacity"]?.SetValue(HeavyBlur.Opacity * opacity);
        effect.Parameters["uSaturation"]?.SetValue(1.08f);
        effect.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
        effect.Parameters["uScreenSize"]?.SetValue(new Vector2(backdrop.Width, backdrop.Height));
        effect.Parameters["uPanelRect"]?.SetValue(new Vector4(screen.X, screen.Y, screen.Width, screen.Height));
        effect.Parameters["uBackdropOffset"]?.SetValue(Vector2.Zero);
        effect.Parameters["uShaderSpecificData"]?.SetValue(new Vector4(HeavyBlur.BlurRadius, HeavyBlur.Refraction, HeavyBlur.Gloss, HeavyBlur.BorderStrength));

        Restart(spriteBatch, BlendState.AlphaBlend, effect, SpriteSortMode.Immediate);
        spriteBatch.Draw(TextureAssets.MagicPixel.Value, screen, Color.White);
        Restart(spriteBatch, BlendState.AlphaBlend);
    }

    private static void Restart(SpriteBatch spriteBatch, BlendState blendState, Effect effect = null, SpriteSortMode sortMode = SpriteSortMode.Deferred)
    {
        spriteBatch.End();
        spriteBatch.Begin(sortMode, blendState, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, effect, Matrix.Identity);
    }
}

/// <summary>Draws the post-match team end screen.</summary>
public class EndScreenLayer : GameInterfaceLayer
{
    private const int CardGap = 20;
    private const int TitleHeight = 82;
    private const int ScoreHeight = 58;
    private const int TitleScoreGap = 10;
    private const int ScoreCardsGap = 26;
    private const int RewardHeight = 72;
    private const int RewardGap = 14;
    private const int BackButtonGap = 16;
    private const int LayoutMargin = 28;
    private const int BackButtonWidth = 240;
    private const int BackButtonHeight = 54;

    // The big "death" font measures tall (lots of trailing space), so geometric centring sits high;
    // these push the title + scoreline down to read as visually centred.
    private const int TitleYNudge = 14;
    private const int ScoreYNudge = 8;

    private static int ViewW => Main.screenWidth;
    private static int ViewH => Main.screenHeight;
    private static Texture2D PanelBackground => Main.Assets.Request<Texture2D>("Images/UI/PanelBackground").Value;
    private static Texture2D PanelBorder => Main.Assets.Request<Texture2D>("Images/UI/PanelBorder").Value;

    // --- Synchronized player animation (walk -> jump -> wave, looping) ---
    private const int FrameHeight = 56;     // player body/leg sheet frame height
    private const int AnimWalk = 96;        // frames spent walking in place
    private const int AnimJump = 48;        // frames spent on the hop
    private const int AnimWave = 120;       // frames spent waving
    private const int AnimCycle = AnimWalk + AnimJump + AnimWave;

    // --- Purple "liquid glass" chrome, to match the purple admin tools + amethyst gems ---
    internal static readonly GlassPanelStyle PurpleHeader = new(new Color(58, 30, 110), new Color(120, 86, 200), new Color(184, 146, 255), 0.99f, 12.0f, 2.10f, 0.84f, 0.96f);
    internal static readonly GlassPanelStyle PurpleInset = new(new Color(38, 20, 80), new Color(96, 70, 168), new Color(156, 120, 240), 0.99f, 11.0f, 1.85f, 0.58f, 0.76f);
    internal static readonly GlassPanelStyle PurpleBadge = new(new Color(126, 72, 224), new Color(184, 142, 255), new Color(238, 216, 255), 0.99f, 10.5f, 2.22f, 1.00f, 1.70f);

    private readonly EndScreenSystem system;
    private readonly EndScreenBackButton backButton;
    private readonly UserInterface backInterface;
    private readonly UIState backState;

    public EndScreenLayer(EndScreenSystem system)
        : base("PvPAdventure: End Screen", InterfaceScaleType.None)
    {
        this.system = system;

        backButton = new EndScreenBackButton();
        backState = new UIState();
        backState.Append(backButton);
        backState.Activate();

        backInterface = new UserInterface();
        backInterface.SetState(backState);
    }

    protected override bool DrawSelf()
    {
        if (!system.IsVisible)
            return true;

        EndScreenSnapshot snapshot = system.CurrentSnapshot;
        float opacity = system.Opacity;
        SpriteBatch spriteBatch = Main.spriteBatch;
        EndScreenLayout layout = GetLayout(snapshot);

        DrawHeader(spriteBatch, snapshot, opacity, layout);
        DrawCards(spriteBatch, snapshot, opacity, layout);
        DrawReward(spriteBatch, snapshot, opacity, layout.RewardBox); // reward follows cards
        DrawBackButton(spriteBatch, snapshot, opacity, layout.RewardBox);

        return true;
    }

    private void DrawHeader(SpriteBatch spriteBatch, EndScreenSnapshot snapshot, float opacity, EndScreenLayout layout)
    {
        string title = ResultTitle(snapshot.Result);
        Color titleColor = ResultColor(snapshot.Result);

        const float scoreScale = 0.72f;
        Rectangle titleBox = layout.TitleBox;
        Rectangle scoreBox = layout.ScoreBox;

        DrawGlassPanel(spriteBatch, titleBox, opacity, TeamStyle(PurpleHeader, snapshot.Team));
        DrawGlassPanel(spriteBatch, scoreBox, opacity, TeamStyle(PurpleInset, snapshot.Team));

        DrawBigText(spriteBatch, title, titleBox, titleColor * opacity, 1.22f, TitleYNudge);
        DrawScore(spriteBatch, snapshot, scoreBox, opacity, scoreScale); // every team, each in its own colour
    }

    private void DrawCards(SpriteBatch spriteBatch, EndScreenSnapshot snapshot, float opacity, EndScreenLayout layout)
    {
        int count = snapshot.Players.Count;
        if (count == 0)
            return;

        int width = layout.CardWidth;
        int height = layout.CardHeight;
        int x = layout.CardsBox.X;
        int y = layout.CardsBox.Y;

        for (int i = 0; i < count; i++)
        {
            float cardIn = Smooth((system.AgeFrames - 28 - i * 7) / 20f);
            if (cardIn <= 0f)
                continue;

            Rectangle card = new(x + i * (width + CardGap), y + (int)((1f - cardIn) * 24f), width, height);
            DrawCard(spriteBatch, snapshot, snapshot.Players[i], card, opacity * cardIn, i == 0, i);
        }
    }

    private void DrawCard(SpriteBatch spriteBatch, EndScreenSnapshot snapshot, EndScreenPlayerStats player, Rectangle card, float opacity, bool mvp, int cardIndex)
    {
        DrawTeamPanel(spriteBatch, card, TeamColor(snapshot.Team), opacity, 0.72f); // simple team stat card
        DrawPlayer(spriteBatch, player.PlayerIndex, new Rectangle(card.X + 12, card.Y + 56, card.Width - 16, 160), system.AgeFrames, cardIndex);

        if (mvp)
            DrawMvpBadge(spriteBatch, card, opacity, snapshot.Team);

        int y = card.Y + 178;
        DrawPlayerName(spriteBatch, snapshot.Team, player.Name, card, y, opacity);
        DrawGemText(spriteBatch, player.Reward, card, y + 38, opacity);

        y += 76;
        DrawStatRow(spriteBatch, snapshot.Team, card, ref y, "Kills", player.Kills.ToString(), opacity);
        DrawStatRow(spriteBatch, snapshot.Team, card, ref y, "Deaths", player.Deaths.ToString(), opacity);
        DrawStatRow(spriteBatch, snapshot.Team, card, ref y, "Damage", Short(player.DamageDealt), opacity);
    }

    private void DrawReward(SpriteBatch spriteBatch, EndScreenSnapshot snapshot, float opacity, Rectangle rewardBox)
    {
        DrawGlassPanel(spriteBatch, rewardBox, opacity, TeamStyle(PurpleInset, snapshot.Team));

        float progress = Smooth((system.AgeFrames - 45) / 85f);
        uint gems = (uint)Math.Round(snapshot.LocalPlayerReward * progress);

        const float textScale = 1.15f;
        string text = $"You earned {gems} Gems!";
        const float iconSize = 32f; // 0.75-ish of the old 42px icon
        const float iconGap = 14f;
        Vector2 textSize = FontAssets.MouseText.Value.MeasureString(text) * textScale;
        float blockWidth = iconSize + iconGap + textSize.X; // gem icon + text centered together
        Vector2 iconCenter = new(rewardBox.Center.X - blockWidth / 2f + iconSize / 2f, rewardBox.Center.Y);
        Vector2 textPosition = new(iconCenter.X + iconSize / 2f + iconGap, rewardBox.Center.Y - textSize.Y / 2f);

        float sparkle = snapshot.LocalPlayerReward > 0 ? progress : 0f;
        SpawnGemDust(rewardBox, iconCenter, opacity, sparkle, system.AgeFrames); // first 3 seconds: ramp up, then fade
        DrawGemRewardEffects(spriteBatch, rewardBox, iconCenter, opacity, sparkle); // intense amethyst glow + dust
        DrawCenteredTexture(spriteBatch, Ass.IconGem.Value, iconCenter, iconSize, Color.White * opacity);
        DrawText(spriteBatch, text, textPosition, Color.White * opacity, textScale);
    }

    private void DrawBackButton(SpriteBatch spriteBatch, EndScreenSnapshot snapshot, float opacity, Rectangle rewardBox)
    {
        if (system.AgeFrames < EndScreenSystem.BackButtonDelayFrames)
            return;

        float buttonOpacity = opacity * Smooth((system.AgeFrames - EndScreenSystem.BackButtonDelayFrames) / 24f);
        Rectangle button = new((ViewW - BackButtonWidth) / 2, rewardBox.Bottom + BackButtonGap, BackButtonWidth, BackButtonHeight);
        bool hovered = button.Contains(Main.MouseScreen.ToPoint());

        if (hovered)
            HandleBackHover(); // consume mouse while hovering

        backButton.Hovered = hovered;
        backButton.TeamTint = TeamColor(snapshot.Team);
        backButton.Opacity = buttonOpacity;
        backButton.Left.Set(button.X, 0f);
        backButton.Top.Set(button.Y, 0f);
        backButton.Width.Set(button.Width, 0f);
        backButton.Height.Set(button.Height, 0f);

        backState.Recalculate();
        backInterface.Draw(spriteBatch, Main._drawInterfaceGameTime);
    }

    private void HandleBackHover()
    {
        Main.LocalPlayer.mouseInterface = true;

        if (!Main.mouseLeft || !Main.mouseLeftRelease)
            return;

        Main.mouseLeftRelease = false;
        SoundEngine.PlaySound(SoundID.MenuClose);
        system.Hide(); // close summary early
    }

    private static void DrawPlayerName(SpriteBatch spriteBatch, Team team, string name, Rectangle card, int y, float opacity)
    {
        string fitted = Fit(name, card.Width - 28);
        float width = FontAssets.MouseText.Value.MeasureString(fitted).X;
        Vector2 position = new(card.Center.X - width / 2f, y + 5);

        DrawText(spriteBatch, fitted, position, TeamColor(team) * opacity, 1f);
    }

    private static void DrawGemText(SpriteBatch spriteBatch, uint reward, Rectangle card, int y, float opacity)
    {
        string text = $"{reward} Gems";
        Rectangle area = new(card.X, y, card.Width, 26);
        Color color = new(231, 213, 144);

        DrawText(spriteBatch, text, CenterText(text, area, 0.82f), color * opacity, 0.82f);
    }

    private static void SpawnGemDust(Rectangle box, Vector2 gemCenter, float opacity, float intensity, int ageFrames)
    {
        if (opacity <= 0f || intensity <= 0f || ageFrames > 180)
            return;

        float ramp = Smooth(ageFrames / 120f);
        float fade = 1f - Smooth((ageFrames - 120) / 60f);
        float dustPower = opacity * intensity * ramp * fade;
        if (dustPower <= 0.01f)
            return;

        int count = Math.Max(1, (int)MathF.Ceiling(9f * ramp * fade));

        for (int i = 0; i < count; i++)
        {
            float angle = Main.rand.NextFloat(MathHelper.TwoPi);
            float radius = Main.rand.NextFloat(9f, 34f);
            Vector2 jitter = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
            Vector2 world = Main.screenPosition + gemCenter + jitter;
            Vector2 velocity = jitter.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(0.25f, 0.95f) + new Vector2(0f, Main.rand.NextFloat(-0.85f, -0.15f));
            Dust dust = Dust.NewDustPerfect(world, DustID.PurpleTorch, velocity, 70, new Color(214, 98, 255), Main.rand.NextFloat(0.85f, 1.65f) * dustPower);

            dust.noGravity = true;
            dust.fadeIn = 1.35f * dustPower;
            dust.alpha = (int)MathHelper.Lerp(130f, 20f, dustPower);
        }
    }

    /// <summary>
    /// Intense amethyst reward VFX: a diffuse purple haze, a central bloom, two hero star flares,
    /// a dense twinkling field of <see cref="Main.DrawPrettyStarSparkle"/> crosses plus fine
    /// <see cref="TextureAssets.Star"/> dots, and rising motes — all additive. Sparkle positions are
    /// stable (golden-ratio scatter); only their brightness/flare twinkles.
    /// </summary>
    private static void DrawGemRewardEffects(SpriteBatch spriteBatch, Rectangle box, Vector2 gemCenter, float opacity, float intensity)
    {
        if (opacity <= 0f || intensity <= 0f)
            return;

        float time = Main.GlobalTimeWrappedHourly;
        float a = opacity * intensity;
        Texture2D pixel = TextureAssets.MagicPixel.Value;

        Color violet = new(140, 70, 240);
        Color amethyst = new(178, 96, 255);
        Color magenta = new(226, 104, 255);

        Restart(spriteBatch, BlendState.Additive);

        // 0) Diffuse purple haze lifting the whole bar (wide wash + brighter core band).
        spriteBatch.Draw(pixel, box, new Color(96, 40, 184) * (0.10f * a));
        Rectangle core = box;
        core.Inflate(-box.Width / 5, 2);
        spriteBatch.Draw(pixel, core, new Color(150, 70, 240) * (0.09f * a));

        // 1) Central bloom on the gem — huge fatness, tiny rays = round glow.
        float bloomPulse = 0.72f + 0.28f * MathF.Sin(time * 2.4f);
        Main.DrawPrettyStarSparkle(opacity, SpriteEffects.None, gemCenter,
            new Color(255, 255, 255, 0) * (intensity * bloomPulse), amethyst, 0.5f,
            0f, 0.5f, 0.5f, 1f, time * 0.2f,
            new Vector2(0f, 0.6f) * intensity, Vector2.One * (3.4f * intensity));

        // 2) Two hero 4-point flares on the gem, counter-rotating.
        float flare = 0.5f + 0.5f * MathF.Sin(time * 3f);
        Main.DrawPrettyStarSparkle(opacity, SpriteEffects.None, gemCenter,
            new Color(255, 255, 255, 0) * (0.55f * intensity), amethyst, flare,
            0f, 0.5f, 0.5f, 1f, MathHelper.PiOver4 + time * 0.35f,
            new Vector2(0f, 1.8f) * intensity, Vector2.One * (0.9f * intensity));
        Main.DrawPrettyStarSparkle(opacity, SpriteEffects.None, gemCenter,
            new Color(255, 255, 255, 0) * (0.8f * intensity), magenta, 1f - flare,
            0f, 0.5f, 0.5f, 1f, -time * 0.5f,
            new Vector2(0f, 2.8f) * intensity, Vector2.One * (1.1f * intensity));

        // 3) Dense field of crisp twinkling cross-sparkles. flareCounter (0..1) drives fade in/out.
        for (int i = 0; i < 34; i++)
        {
            Vector2 pos = new(box.X + Frac(i * 0.61803398f + 0.11f) * box.Width,
                              box.Y + Frac(i * 0.75487766f + 0.39f) * box.Height);
            float phase = Frac(time * (0.18f + 0.05f * (i % 4)) + i * 0.137f);
            bool feature = i % 7 == 0;
            float size = (feature ? 1.4f : 0.7f) * intensity;
            Color shine = (i % 3) switch { 0 => magenta, 1 => amethyst, _ => Color.White };
            Main.DrawPrettyStarSparkle(opacity, SpriteEffects.None, pos,
                new Color(255, 255, 255, 0) * (intensity * (feature ? 0.9f : 0.6f)), shine, phase,
                0f, 0.5f, 0.5f, 1f, i * 0.7f,
                new Vector2(0f, feature ? 3.0f : 1.8f) * size, Vector2.One * size);
        }

        // 4) Fine twinkling dots for grain.
        for (int i = 0; i < 40; i++)
        {
            Vector2 pos = new(box.X + Frac(i * 0.41421356f + 0.27f) * box.Width,
                              box.Y + Frac(i * 0.30277563f + 0.53f) * box.Height);
            float tw = 0.5f + 0.5f * MathF.Sin(time * (4f + i % 5) + i * 1.7f);
            if (tw < 0.18f)
                continue;

            Texture2D star = TextureAssets.Star[i % 4].Value;
            float s = (0.045f + 0.085f * tw) * intensity;
            Color col = Color.Lerp(violet, i % 3 == 0 ? Color.White : magenta, tw) * (tw * a);
            col.A = 0;
            spriteBatch.Draw(star, pos, null, col, i, star.Size() * 0.5f, s, SpriteEffects.None, 0f);
        }

        // 5) Purple motes rising and fading across the bar.
        for (int i = 0; i < 16; i++)
        {
            float phase = Frac(time * (0.10f + 0.04f * (i % 3)) + i * 0.167f); // 0 bottom -> 1 top
            float x = box.X + Frac(i * 0.61803398f + 0.07f) * box.Width + MathF.Sin(time * 2f + i) * 5f;
            float y = box.Bottom - phase * (box.Height + 14f);
            float fade = MathF.Sin(phase * MathF.PI); // ease in/out over the climb

            Texture2D star = TextureAssets.Star[i % 4].Value;
            Color col = Color.Lerp(amethyst, magenta, i % 2) * (fade * a * 0.9f);
            col.A = 0;
            spriteBatch.Draw(star, new Vector2(x, y), null, col, time + i, star.Size() * 0.5f, 0.07f * intensity, SpriteEffects.None, 0f);
        }

        Restart(spriteBatch, BlendState.AlphaBlend);
    }

    private static float Frac(float value) => value - MathF.Floor(value);

    private static void DrawStatRow(SpriteBatch spriteBatch, Team team, Rectangle card, ref int y, string label, string value, float opacity)
    {
        Rectangle row = new(card.X + 14, y, card.Width - 28, 36);
        DrawTeamPanel(spriteBatch, row, TeamColor(team), opacity, 0.42f);

        DrawText(spriteBatch, label, new Vector2(row.X + 12, row.Y + 8), Color.White * opacity, 0.88f);

        float valueWidth = FontAssets.MouseText.Value.MeasureString(value).X * 0.9f;
        Vector2 valuePos = new(row.Right - 12 - valueWidth, row.Y + 8);
        DrawText(spriteBatch, value, valuePos, Color.White * opacity, 0.9f);

        y += 38;
    }

    private static void DrawPlayer(SpriteBatch spriteBatch, byte id, Rectangle area, int animClock, int cardIndex)
    {
        if (id >= Main.maxPlayers || Main.player[id]?.active != true)
            return;

        Player player = (Player)Main.player[id].Clone();
        player.dead = false;
        player.ghost = false;
        player.isDisplayDollOrInanimate = true;

        float hop = ApplyPlayerAnimation(player, animClock, cardIndex);

        float scale = MathHelper.Clamp(area.Width / 86f, 1.45f, 2.35f);
        Vector2 position = new(area.Center.X - player.width * scale / 2f, area.Bottom - player.height * scale - 4f - hop * scale);
        RasterizerState oldRasterizer = spriteBatch.GraphicsDevice.RasterizerState;

        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);

        EndScreenPlayerDrawPlayer.ForceFullBright = true;
        try
        {
            Main.PlayerRenderer.DrawPlayer(Main.Camera, player, position + Main.screenPosition, 0f, Vector2.Zero, 0f, scale);
        }
        finally
        {
            EndScreenPlayerDrawPlayer.ForceFullBright = false;
        }

        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, oldRasterizer, null, Matrix.Identity);
    }

    /// <summary>
    /// Drives every preview through one shared looping routine — walk in place, a synchronized hop,
    /// then a rippling wave down the line — by setting body/leg frames and the composite front arm.
    /// Returns an upward hop offset (in player-space pixels) used during the jump phase.
    /// </summary>
    private static float ApplyPlayerAnimation(Player player, int clock, int cardIndex)
    {
        int c = ((clock % AnimCycle) + AnimCycle) % AnimCycle;

        // Start from a clean arm pose every frame (the live clone may carry gameplay arm state).
        player.SetCompositeArmFront(false, Player.CompositeArmStretchAmount.Full, 0f);
        player.SetCompositeArmBack(false, Player.CompositeArmStretchAmount.Full, 0f);
        player.direction = 1;

        if (c < AnimWalk)
        {
            // Walk cycle uses body/leg frames 7..19 (13 frames), ~5 ticks each.
            int frame = 7 + (c / 5) % 13;
            SetBodyFrame(player, frame);
            return 0f;
        }

        if (c < AnimWalk + AnimJump)
        {
            SetBodyFrame(player, 5); // airborne/jump pose
            float jumpProgress = (c - AnimWalk) / (float)AnimJump;
            return MathF.Sin(jumpProgress * MathF.PI) * 30f; // smooth hop up and back down
        }

        // Wave: idle body, raise the front arm and oscillate it. Stagger per card so the wave
        // ripples across the line like a crowd wave.
        SetBodyFrame(player, 0);
        int wave = c - AnimWalk - AnimJump - cardIndex * 8;
        if (wave > 0)
        {
            // For direction == 1 the composite-arm rotation is negative to raise the arm
            // (cf. ErkySSC MapHoldingPlayer: -1.9 holds the arm out). ~-2.5 lifts it up high.
            const float armUp = -2.5f;
            float rotation = armUp + MathF.Sin(wave * 0.26f) * 0.22f; // oscillate the raised arm
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation);
        }

        return 0f;
    }

    private static void SetBodyFrame(Player player, int frame)
    {
        int y = frame * FrameHeight;
        player.bodyFrame.Y = y;
        player.legFrame.Y = y;
        player.headFrame.Y = 0; // keep the head neutral/forward
    }

    private static void DrawMvpBadge(SpriteBatch spriteBatch, Rectangle card, float opacity, Team team)
    {
        Rectangle badge = new(card.X - 7, card.Y - 9, 72, 34);

        DrawTeamPanel(spriteBatch, badge, TeamColor(team), opacity, 1f);
        DrawText(spriteBatch, "MVP", new Vector2(badge.X + 15, badge.Y + 8), Color.White * opacity, 0.86f); // white with black stroke
    }

    private static void DrawScore(SpriteBatch spriteBatch, EndScreenSnapshot snapshot, Rectangle area, float opacity, float scale)
    {
        var scores = snapshot.AllScores;
        if (scores.Count == 0)
            return;

        float width = ScorelineWidth(snapshot, scale);
        float height = FontAssets.DeathText.Value.MeasureString("0").Y * scale;
        Vector2 position = new(area.Center.X - width / 2f, area.Center.Y - height / 2f + ScoreYNudge);

        for (int i = 0; i < scores.Count; i++)
        {
            if (i > 0)
                DrawBigAt(spriteBatch, "  -  ", ref position, Color.White * opacity, scale); // neutral separator
            DrawBigAt(spriteBatch, scores[i].Score.ToString(), ref position, TeamColor(scores[i].Team) * opacity, scale);
        }
    }

    private static float ScorelineWidth(EndScreenSnapshot snapshot, float scale)
    {
        var font = FontAssets.DeathText.Value;
        var scores = snapshot.AllScores;
        float width = 0f;

        for (int i = 0; i < scores.Count; i++)
        {
            if (i > 0)
                width += font.MeasureString("  -  ").X * scale;
            width += font.MeasureString(scores[i].Score.ToString()).X * scale;
        }

        return width;
    }

    private static void DrawBigAt(SpriteBatch spriteBatch, string text, ref Vector2 position, Color color, float scale)
    {
        Utils.DrawBorderStringBig(spriteBatch, text, position, color, scale);
        position.X += FontAssets.DeathText.Value.MeasureString(text).X * scale;
    }

    internal static void DrawGlassPanel(SpriteBatch spriteBatch, Rectangle rect, float opacity, GlassPanelStyle style, Color? borderOverride = null)
    {
        if (opacity <= 0f)
            return;

        Color teamColor = style.Primary;
        GlassPanelStyle light = new(
            teamColor,
            Color.Lerp(teamColor, Color.White, 0.12f),
            Color.Black,
            0.72f * opacity,
            4.0f,
            0.45f,
            0.10f,
            0.85f);

        DrawGlassFill(spriteBatch, rect, light);
        Utils.DrawSplicedPanel(spriteBatch, PanelBackground, rect.X, rect.Y, rect.Width, rect.Height, 10, 10, 10, 10, teamColor * (0.38f * opacity));
        Utils.DrawSplicedPanel(spriteBatch, PanelBorder, rect.X, rect.Y, rect.Width, rect.Height, 10, 10, 10, 10, (borderOverride ?? Color.Black) * opacity);
    }

    private static void DrawGlassFill(SpriteBatch spriteBatch, Rectangle rect, GlassPanelStyle style)
    {
        if (EffectLoader.TryGetLiquidGlassEffect(out Effect effect))
        {
            Texture2D backdrop = Main.screenTarget ?? TextureAssets.MagicPixel.Value;
            effect.Parameters["uBackdropTexture"]?.SetValue(backdrop);
            effect.Parameters["uColor"]?.SetValue(style.Primary.ToVector3());
            effect.Parameters["uSecondaryColor"]?.SetValue(style.Secondary.ToVector3());
            effect.Parameters["uBorderColor"]?.SetValue(style.Border.ToVector3());
            effect.Parameters["uOpacity"]?.SetValue(style.Opacity);
            effect.Parameters["uSaturation"]?.SetValue(1.08f);
            effect.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
            effect.Parameters["uScreenSize"]?.SetValue(new Vector2(backdrop.Width, backdrop.Height));
            effect.Parameters["uPanelRect"]?.SetValue(new Vector4(rect.X, rect.Y, rect.Width, rect.Height));
            effect.Parameters["uBackdropOffset"]?.SetValue(new Vector2(-6f, -6f));
            effect.Parameters["uShaderSpecificData"]?.SetValue(new Vector4(style.BlurRadius, style.Refraction, style.Gloss, style.BorderStrength));

            Restart(spriteBatch, BlendState.AlphaBlend, effect, SpriteSortMode.Immediate);
            Utils.DrawSplicedPanel(spriteBatch, PanelBackground, rect.X, rect.Y, rect.Width, rect.Height, 10, 10, 10, 10, Color.White);
            Restart(spriteBatch, BlendState.AlphaBlend);
            return;
        }

        Color tint = Color.Lerp(style.Primary, style.Secondary, 0.38f) * (style.Opacity * 0.46f);
        Utils.DrawSplicedPanel(spriteBatch, PanelBackground, rect.X, rect.Y, rect.Width, rect.Height, 10, 10, 10, 10, tint);
        Utils.DrawSplicedPanel(spriteBatch, PanelBackground, rect.X + 3, rect.Y + 3, rect.Width - 6, Math.Min(12, rect.Height / 3), 10, 10, 10, 10, Color.White * (0.035f + style.Gloss * 0.03f));
    }

    internal static void DrawTeamPanel(SpriteBatch spriteBatch, Rectangle rect, Color teamColor, float opacity, float fill = 0.72f)
    {
        Color background = teamColor * (fill * opacity);
        Color border = Color.Black * opacity;

        Utils.DrawSplicedPanel(spriteBatch, PanelBackground, rect.X, rect.Y, rect.Width, rect.Height, 10, 10, 10, 10, background);
        Utils.DrawSplicedPanel(spriteBatch, PanelBorder, rect.X, rect.Y, rect.Width, rect.Height, 10, 10, 10, 10, border);
    }

    private static void DrawCenteredTexture(SpriteBatch spriteBatch, Texture2D texture, Vector2 center, float size, Color color)
    {
        float scale = size / Math.Max(texture.Width, texture.Height);
        spriteBatch.Draw(texture, center, null, color, 0f, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
    }

    private static void Restart(SpriteBatch spriteBatch, BlendState blendState, Effect effect = null, SpriteSortMode sortMode = SpriteSortMode.Deferred)
    {
        spriteBatch.End();
        spriteBatch.Begin(sortMode, blendState, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, effect, Matrix.Identity);
    }

    private static GlassPanelStyle TeamStyle(GlassPanelStyle template, Team team)
    {
        Color teamColor = TeamColor(team);
        // Keep the template's glass params (gloss, opacity, blur) but recolour to the exact team colour.
        return template with { Primary = teamColor, Secondary = teamColor, Border = teamColor };
    }

    private static string ResultTitle(EndScreenResult result) => result switch
    {
        EndScreenResult.Victory => "Victory!",
        EndScreenResult.Defeat => "Defeat",
        _ => "Tie!"
    };

    private static Color ResultColor(EndScreenResult result) => result switch
    {
        EndScreenResult.Victory => new Color(218, 255, 197),
        EndScreenResult.Defeat => new Color(255, 162, 150),
        _ => new Color(255, 236, 168)
    };

    private static Rectangle CenteredBox(int width, int height, int y)
    {
        return new Rectangle((ViewW - width) / 2, y, width, height);
    }

    private static EndScreenLayout GetLayout(EndScreenSnapshot snapshot)
    {
        const float scoreScale = 0.72f;
        int count = Math.Max(1, snapshot.Players.Count);
        int cardWidth = GetCardWidth(count);
        int cardHeight = GetCardHeight();
        int cardsWidth = cardWidth * count + CardGap * (count - 1);
        int totalHeight = TitleHeight + TitleScoreGap + ScoreHeight + ScoreCardsGap + cardHeight + RewardGap + RewardHeight + BackButtonGap + BackButtonHeight;
        int top = totalHeight <= ViewH - LayoutMargin * 2 ? Math.Max(LayoutMargin, (ViewH - totalHeight) / 2) : LayoutMargin;

        int titleWidth = Math.Min(430, Math.Max(280, ViewW - 80));
        int scoreMaxWidth = Math.Max(255, ViewW - 80);
        int scoreWidth = Math.Clamp((int)ScorelineWidth(snapshot, scoreScale) + 64, 255, scoreMaxWidth);
        int rewardWidth = Math.Min(560, Math.Max(260, ViewW - 180));

        Rectangle title = CenteredBox(titleWidth, TitleHeight, top);
        Rectangle score = CenteredBox(scoreWidth, ScoreHeight, title.Bottom + TitleScoreGap);
        Rectangle cards = new((ViewW - cardsWidth) / 2, score.Bottom + ScoreCardsGap, cardsWidth, cardHeight);
        Rectangle reward = CenteredBox(rewardWidth, RewardHeight, cards.Bottom + RewardGap);

        return new EndScreenLayout(title, score, cards, reward, cardWidth, cardHeight);
    }

    private static int GetCardWidth(int count)
    {
        return Math.Clamp((ViewW - 68 - CardGap * (count - 1)) / count, 145, 232);
    }

    private static int GetCardHeight()
    {
        // Sized to its content: the card ends just below the Damage stat row (no trailing empty space).
        int fixedHeight = TitleHeight + TitleScoreGap + ScoreHeight + ScoreCardsGap + RewardGap + RewardHeight + BackButtonGap + BackButtonHeight + LayoutMargin * 2 + 28;
        int available = ViewH - fixedHeight;
        return Math.Clamp(available, 280, 380);
    }

    private readonly record struct EndScreenLayout(Rectangle TitleBox, Rectangle ScoreBox, Rectangle CardsBox, Rectangle RewardBox, int CardWidth, int CardHeight);

    private static void DrawBigText(SpriteBatch spriteBatch, string text, Rectangle area, Color color, float scale, float yOffset = 0f)
    {
        Vector2 position = CenterBigText(text, area, scale);
        position.Y += yOffset;
        Utils.DrawBorderStringBig(spriteBatch, text, position, color, scale);
    }

    private static void DrawText(SpriteBatch spriteBatch, string text, Vector2 position, Color color, float scale)
    {
        Utils.DrawBorderString(spriteBatch, text, position, color, scale);
    }

    private static Vector2 CenterText(string text, Rectangle area, float scale)
    {
        Vector2 size = FontAssets.MouseText.Value.MeasureString(text) * scale;
        return new Vector2(area.Center.X - size.X / 2f, area.Center.Y - size.Y / 2f);
    }

    private static Vector2 CenterBigText(string text, Rectangle area, float scale)
    {
        Vector2 size = FontAssets.DeathText.Value.MeasureString(text) * scale;
        return new Vector2(area.Center.X - size.X / 2f, area.Center.Y - size.Y / 2f);
    }

    private static string Short(uint value)
    {
        return value >= 1000 ? $"{value / 1000f:0.0}k" : value.ToString();
    }

    private static string Fit(string text, float width)
    {
        if (FontAssets.MouseText.Value.MeasureString(text).X <= width)
            return text;

        while (text.Length > 1 && FontAssets.MouseText.Value.MeasureString(text + "..").X > width)
            text = text[..^1];

        return text + "..";
    }

    private static float Smooth(float value)
    {
        value = MathHelper.Clamp(value, 0f, 1f);
        return value * value * (3f - 2f * value);
    }

    private static Color TeamColor(Team team)
    {
        int index = (int)team;
        return index >= 0 && index < Main.teamColor.Length ? Main.teamColor[index] : Color.White;
    }
}

/// <summary>Glass Back button for closing the end screen.</summary>
public class EndScreenBackButton : UIAutoScaleTextTextPanel<string>
{
    public bool Hovered;
    public float Opacity = 1f;
    public Color TeamTint = Color.White;

    public EndScreenBackButton() : base("Back", 1f)
    {
        PaddingLeft = PaddingRight = 14f;
        PaddingTop = PaddingBottom = 10f;
        BackgroundColor = Color.Transparent;
        BorderColor = Color.Transparent;
        TextColor = Color.White;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        GlassPanelStyle style = EndScreenLayer.PurpleInset with { Primary = TeamTint };
        EndScreenLayer.DrawGlassPanel(spriteBatch, GetDimensions().ToRectangle(), Opacity, style, Hovered ? Color.Yellow : Color.Black);
        TextColor = Color.White * Opacity;
        base.DrawSelf(spriteBatch);
    }
}

/// <summary>Forces player previews to render bright.</summary>
public class EndScreenPlayerDrawPlayer : ModPlayer
{
    public static bool ForceFullBright;

    public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
    {
        if (!ForceFullBright)
            return;

        Player player = drawInfo.drawPlayer;

        drawInfo.colorEyeWhites = Color.White;
        drawInfo.colorArmorHead = Color.White;
        drawInfo.colorArmorBody = Color.White;
        drawInfo.colorArmorLegs = Color.White;
        drawInfo.colorMount = Color.White;
        drawInfo.colorEyes = player.eyeColor;
        drawInfo.colorHair = player.GetHairColor(false);
        drawInfo.colorHead = player.skinColor;
        drawInfo.colorBodySkin = player.skinColor;
        drawInfo.colorLegs = player.skinColor;
        drawInfo.colorShirt = player.shirtColor;
        drawInfo.colorUnderShirt = player.underShirtColor;
        drawInfo.colorPants = player.pantsColor;
        drawInfo.colorShoes = player.shoeColor;
    }
}
