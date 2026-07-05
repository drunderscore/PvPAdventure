using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;
using Terraria.UI;
using Terraria.UI.Chat;

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
    private const int MaxCardsPerPage = 5;
    private const int PageFrames = 360;
    private const int TitleHeight = 82;
    private const int ScoreHeight = 35;
    private const int TitleScoreGap = 10;
    private const int ScoreCardsGap = 26;
    private const int RewardHeight = 52;
    private const int RewardGap = 14;
    private const int BackButtonGap = 16;
    private const int LayoutMargin = 28;
    private const int BackButtonWidth = 240;
    private const int BackButtonHeight = 42;
    private const bool DebugGemRewardLayout = true;

    // The big "death" font measures tall (lots of trailing space), so geometric centring sits high;
    // these push the title + scoreline down to read as visually centred.
    private const int TitleYNudge = 14;
    private const int PlayerTextTopOffset = 172;
    private const int RoleTopOffset = 39;
    private const int StatRowCount = 3;
    private const int StatRowHeight = 38;
    private const int StatRowGap = 3;
    private const int StatBlockBottomPadding = 4;
    private const float PlayerNameScale = 1.12f;
    private const float RoleTitleScale = 0.84f;
    private const float RoleValueScale = 0.80f;
    private const float StatLabelScale = 0.94f;
    private const float StatValueScale = 0.96f;

    private static int ViewW => Main.screenWidth;
    private static int ViewH => Main.screenHeight;
    private static Texture2D PanelBackground => Main.Assets.Request<Texture2D>("Images/UI/PanelBackground").Value;
    private static Texture2D PanelBorder => Main.Assets.Request<Texture2D>("Images/UI/PanelBorder").Value;
    private static readonly RasterizerState ScissorRasterizer = new() { CullMode = CullMode.None, ScissorTestEnable = true };

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
    private Team selectedTeam = Team.None;
    private int soundPresentationId = -1;
    private bool playedGemSound;

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
        EnsureSelectedTeam(snapshot);
        EnsureSoundState();
        float opacity = system.Opacity;
        SpriteBatch spriteBatch = Main.spriteBatch;
        IReadOnlyList<EndScreenPlayerStats> players = GetVisiblePlayers(snapshot);
        bool showOwnTeamPanels = IsOwnTeamSelected(snapshot);
        EndScreenLayout layout = GetLayout(snapshot, players.Count, showOwnTeamPanels);

        DrawHeader(spriteBatch, snapshot, opacity, layout, showOwnTeamPanels);
        DrawCards(spriteBatch, players, opacity, layout);

        if (showOwnTeamPanels)
            DrawReward(spriteBatch, snapshot, opacity, layout.RewardBox, selectedTeam); // reward follows cards

        DrawBackButton(spriteBatch, opacity, layout.BackButtonBox);

        return true;
    }

    private void DrawHeader(SpriteBatch spriteBatch, EndScreenSnapshot snapshot, float opacity, EndScreenLayout layout, bool showResultPanel)
    {
        if (showResultPanel)
        {
            string title = ResultTitle(snapshot.Result);
            Color titleColor = ResultColor(snapshot.Result);
            Rectangle titleBox = layout.TitleBox;

            DrawGlassPanel(spriteBatch, titleBox, opacity, TeamStyle(PurpleHeader, snapshot.Team));
            DrawBigText(spriteBatch, title, titleBox, titleColor * opacity, 1.22f, TitleYNudge);
        }

        DrawScore(spriteBatch, snapshot, layout.ScoreBox, opacity); // scaled Scoreline-style team point panels
    }

    private IReadOnlyList<EndScreenPlayerStats> GetVisiblePlayers(EndScreenSnapshot snapshot)
    {
        EndScreenPlayerStats[] teamPlayers = snapshot.Players.Where(p => p.Team == selectedTeam).ToArray();

        if (teamPlayers.Length <= MaxCardsPerPage)
            return teamPlayers;

        int pageCount = (teamPlayers.Length + MaxCardsPerPage - 1) / MaxCardsPerPage;
        int page = (system.AgeFrames / PageFrames) % pageCount;
        return teamPlayers.Skip(page * MaxCardsPerPage).Take(MaxCardsPerPage).ToArray();
    }

    private void DrawCards(SpriteBatch spriteBatch, IReadOnlyList<EndScreenPlayerStats> players, float opacity, EndScreenLayout layout)
    {
        int count = players.Count;
        if (count == 0)
            return;

        int width = layout.CardWidth;
        int height = layout.CardHeight;
        int x = layout.CardsBox.X;
        int y = layout.CardsBox.Y;

        byte mvpIndex = players[0].PlayerIndex;

        for (int i = 0; i < count; i++)
        {
            float cardIn = Smooth((system.AgeFrames - 28 - i * 7) / 20f);
            if (cardIn <= 0f)
                continue;

            Rectangle card = new(x + i * (width + CardGap), y + (int)((1f - cardIn) * 24f), width, height);
            bool mvp = players[i].PlayerIndex == mvpIndex;
            DrawCard(spriteBatch, players[i], card, opacity * cardIn, mvp, i);
        }
    }

    private void DrawCard(SpriteBatch spriteBatch, EndScreenPlayerStats player, Rectangle card, float opacity, bool mvp, int cardIndex)
    {
        DrawTeamPanel(spriteBatch, card, TeamColor(player.Team), opacity, 0.72f); // simple team stat card
        DrawPlayer(spriteBatch, player.PlayerIndex, new Rectangle(card.X + 12, card.Y + 56, card.Width - 16, 160), system.AgeFrames, cardIndex);

        if (mvp)
            DrawMvpBadge(spriteBatch, card, opacity, player.Team);

        int y = card.Y + PlayerTextTopOffset;
        DrawPlayerName(spriteBatch, player.Team, player.Name, card, y, opacity);
        DrawRoleText(spriteBatch, player, card, y + RoleTopOffset, opacity);

        y = card.Bottom - StatBlockBottomPadding - (StatRowCount * StatRowHeight + (StatRowCount - 1) * StatRowGap);
        DrawStatRow(spriteBatch, player.Team, card, ref y, "Kills", player.Kills.ToString(), opacity);
        DrawStatRow(spriteBatch, player.Team, card, ref y, "Deaths", player.Deaths.ToString(), opacity);
        DrawStatRow(spriteBatch, player.Team, card, ref y, "Damage", Short(player.DamageDealt), opacity);
    }

    private void DrawReward(SpriteBatch spriteBatch, EndScreenSnapshot snapshot, float opacity, Rectangle rewardBox, Team teamTint)
    {
        DrawGlassPanel(spriteBatch, rewardBox, opacity, TeamStyle(PurpleInset, teamTint));

        float progress = Smooth((system.AgeFrames - 45) / 85f);
        uint gems = (uint)Math.Round(snapshot.LocalPlayerReward * progress);
        if (!playedGemSound && snapshot.LocalPlayerReward > 0 && progress > 0f)
        {
            playedGemSound = true;
            SoundEngine.PlaySound(SoundID.CoinPickup with { Volume = 0.45f });
        }

        const float textScale = 1.15f;
        string text = $"You earned {gems} Gems!";
        const float iconSize = 32f; // 0.75-ish of the old 42px icon
        const float iconGap = 14f;
        Vector2 textSize = FontAssets.MouseText.Value.MeasureString(text) * textScale;
        float blockWidth = iconSize + iconGap + textSize.X; // gem icon + text centered together
        Vector2 iconCenter = new(rewardBox.Center.X - blockWidth / 2f + iconSize / 2f, rewardBox.Center.Y);
        Vector2 textPosition = new(iconCenter.X + iconSize / 2f + iconGap, rewardBox.Center.Y - textSize.Y / 2f + 4f);

        float sparkle = progress;
        // if (DebugGemRewardLayout)
        //     DrawRewardDebug(spriteBatch, rewardBox, opacity);
        DrawGemRewardEffects(spriteBatch, rewardBox, iconCenter, opacity, sparkle); // clipped UI glow/sparkles above panel
        DrawCenteredTexture(spriteBatch, Ass.IconGem.Value, iconCenter, iconSize, Color.White * opacity);
        DrawText(spriteBatch, text, textPosition, Color.White * opacity, textScale);
    }

    private void DrawBackButton(SpriteBatch spriteBatch, float opacity, Rectangle button)
    {
        if (system.AgeFrames < EndScreenSystem.BackButtonDelayFrames)
            return;

        float buttonOpacity = opacity * Smooth((system.AgeFrames - EndScreenSystem.BackButtonDelayFrames) / 24f);
        bool hovered = button.Contains(Main.MouseScreen.ToPoint());

        if (hovered)
            HandleBackHover(); // consume mouse while hovering

        backButton.Hovered = hovered;
        backButton.TeamTint = TeamColor(selectedTeam);
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

    private static void DrawRewardDebug(SpriteBatch spriteBatch, Rectangle box, float opacity)
    {
        Rectangle red = box;
        Rectangle blue = ScaleRect(box, 0.75f);
        Rectangle green = ScaleRect(box, 0.5f);

        DrawDebugBox(spriteBatch, red, Color.Red * (0.42f * opacity), "1f", 1f, opacity);
        DrawDebugBox(spriteBatch, blue, Color.Blue * (0.50f * opacity), "0.75f", 0.75f, opacity);
        DrawDebugBox(spriteBatch, green, Color.Green * (0.58f * opacity), "0.5f", 0.5f, opacity);
    }

    private static Rectangle ScaleRect(Rectangle rect, float scale)
    {
        int width = (int)(rect.Width * scale);
        int height = (int)(rect.Height * scale);
        return new Rectangle(rect.Center.X - width / 2, rect.Center.Y - height / 2, width, height);
    }

    private static void DrawDebugBox(SpriteBatch spriteBatch, Rectangle rect, Color fill, string text, float scale, float opacity)
    {
        Texture2D pixel = TextureAssets.MagicPixel.Value;
        spriteBatch.Draw(pixel, rect, fill);
        DrawRect(spriteBatch, rect, Color.Black * opacity, 2);
        DrawText(spriteBatch, text, CenterText(text, rect, scale), Color.White * opacity, scale);
    }

    private static void DrawRect(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
    {
        Texture2D pixel = TextureAssets.MagicPixel.Value;
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }

    private static void DrawPlayerName(SpriteBatch spriteBatch, Team team, string name, Rectangle card, int y, float opacity)
    {
        string fitted = Fit(name, card.Width - 28, PlayerNameScale);
        float width = FontAssets.MouseText.Value.MeasureString(fitted).X * PlayerNameScale;
        Vector2 position = new(card.Center.X - width / 2f, y + 1);

        DrawText(spriteBatch, fitted, position, TeamColor(team) * opacity, PlayerNameScale);
    }

    private static void DrawRoleText(SpriteBatch spriteBatch, EndScreenPlayerStats player, Rectangle card, int y, float opacity)
    {
        string title = string.IsNullOrWhiteSpace(player.RoleTitle) ? "Adventurer" : player.RoleTitle;
        string value = string.IsNullOrWhiteSpace(player.RoleValue) ? "Ready for more" : player.RoleValue;
        Rectangle titleArea = new(card.X + 10, y, card.Width - 20, 23);
        Rectangle valueArea = new(card.X + 10, y + 19, card.Width - 20, 23);

        title = Fit(title, titleArea.Width, RoleTitleScale);
        value = Fit(value, valueArea.Width, RoleValueScale);
        DrawText(spriteBatch, title, CenterText(title, titleArea, RoleTitleScale), new Color(255, 232, 130) * opacity, RoleTitleScale);
        DrawText(spriteBatch, value, CenterText(value, valueArea, RoleValueScale), new Color(255, 244, 188) * opacity, RoleValueScale);
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

        GraphicsDevice device = spriteBatch.GraphicsDevice;
        Rectangle oldScissor = device.ScissorRectangle;
        RasterizerState oldRasterizer = device.RasterizerState;
        Rectangle clip = Rectangle.Intersect(box, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight));
        if (clip.Width <= 0 || clip.Height <= 0)
            return;

        float time = Main.GlobalTimeWrappedHourly;
        float a = opacity * intensity;
        Texture2D pixel = TextureAssets.MagicPixel.Value;

        Color violet = new(140, 70, 240);
        Color amethyst = new(178, 96, 255);
        Color magenta = new(226, 104, 255);

        spriteBatch.End();
        device.ScissorRectangle = clip;
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, ScissorRasterizer, null, Matrix.Identity);

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

        spriteBatch.End();
        device.ScissorRectangle = oldScissor;
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.None, oldRasterizer, null, Matrix.Identity);
    }

    private static float Frac(float value) => value - MathF.Floor(value);

    private static void DrawStatRow(SpriteBatch spriteBatch, Team team, Rectangle card, ref int y, string label, string value, float opacity)
    {
        Rectangle row = new(card.X + 12, y, card.Width - 24, StatRowHeight);
        DrawTeamPanel(spriteBatch, row, TeamColor(team), opacity, 0.42f);

        const int horizontalPadding = 12;
        Vector2 labelSize = FontAssets.MouseText.Value.MeasureString(label) * StatLabelScale;
        Vector2 labelPos = new(row.X + horizontalPadding, row.Center.Y - labelSize.Y / 2f + 1f);
        DrawText(spriteBatch, label, labelPos, Color.White * opacity, StatLabelScale);

        Vector2 valueSize = FontAssets.MouseText.Value.MeasureString(value) * StatValueScale;
        Vector2 valuePos = new(row.Right - horizontalPadding - valueSize.X, row.Center.Y - valueSize.Y / 2f + 1f);
        DrawText(spriteBatch, value, valuePos, Color.White * opacity, StatValueScale);

        y += StatRowHeight + StatRowGap;
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

    private void DrawScore(SpriteBatch spriteBatch, EndScreenSnapshot snapshot, Rectangle area, float opacity)
    {
        var scores = snapshot.AllScores;
        if (scores.Count == 0)
            return;

        const float basePointWidth = 50f;
        const float basePointHeight = 30f;
        const float scale = 1.15f;
        int pointWidth = (int)(basePointWidth * scale);
        int pointHeight = (int)(basePointHeight * scale);
        int x = area.Center.X - scores.Count * pointWidth / 2;
        int y = area.Center.Y - pointHeight / 2;

        for (int i = 0; i < scores.Count; i++)
        {
            Rectangle box = new(x + i * pointWidth, y, pointWidth, pointHeight);
            Team team = scores[i].Team;
            Color border = team == selectedTeam ? Color.Yellow : Color.Black;

            if (box.Contains(Main.MouseScreen.ToPoint()))
            {
                Main.LocalPlayer.mouseInterface = true;
                Main.instance.MouseText(team == selectedTeam
                    ? $"Viewing {team} Team results"
                    : $"Click to view {team} Team results");

                if (Main.mouseLeft && Main.mouseLeftRelease)
                {
                    selectedTeam = team;
                    Main.mouseLeftRelease = false;
                    SoundEngine.PlaySound(SoundID.MenuTick);
                }
            }

            Utils.DrawInvBG(spriteBatch, box, TeamColor(scores[i].Team) * (0.7f * opacity));
            Utils.DrawSplicedPanel(spriteBatch, PanelBorder, box.X, box.Y, box.Width, box.Height, 10, 10, 10, 10, border * opacity);

            string text = scores[i].Score.ToString();
            Vector2 textScale = Vector2.One * scale;
            Vector2 size = ChatManager.GetStringSize(FontAssets.MouseText.Value, text, textScale);
            Vector2 pos = new(box.Center.X - size.X / 2f, box.Y + 6f * scale);
            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.MouseText.Value, text, pos, Color.White * opacity, 0f, Vector2.Zero, textScale);
        }
    }

    private void EnsureSelectedTeam(EndScreenSnapshot snapshot)
    {
        if (system.AgeFrames <= 1)
        {
            selectedTeam = DefaultSelectedTeam(snapshot);
            return;
        }

        if (selectedTeam != Team.None && snapshot.AllScores.Any(s => s.Team == selectedTeam))
            return;

        selectedTeam = DefaultSelectedTeam(snapshot);
    }

    private void EnsureSoundState()
    {
        if (soundPresentationId == system.PresentationId)
            return;

        soundPresentationId = system.PresentationId;
        playedGemSound = false;
    }

    private bool IsOwnTeamSelected(EndScreenSnapshot snapshot) => selectedTeam == OwnTeam(snapshot);

    private static Team DefaultSelectedTeam(EndScreenSnapshot snapshot)
    {
        Team ownTeam = OwnTeam(snapshot);

        if (HasScoreTeam(snapshot, ownTeam))
            return ownTeam;

        return snapshot.AllScores.FirstOrDefault().Team;
    }

    private static Team OwnTeam(EndScreenSnapshot snapshot)
    {
        Team localTeam = (Team)Main.LocalPlayer.team;

        if (HasScoreTeam(snapshot, localTeam))
            return localTeam;

        return snapshot.Team;
    }

    private static bool HasScoreTeam(EndScreenSnapshot snapshot, Team team) => team != Team.None && snapshot.AllScores.Any(s => s.Team == team);

    private static float ScorelineWidth(EndScreenSnapshot snapshot, float scale)
    {
        return snapshot.AllScores.Count * 50f * scale;
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

    private static EndScreenLayout GetLayout(EndScreenSnapshot snapshot, int visiblePlayerCount, bool showOwnTeamPanels)
    {
        const float scoreScale = 1.15f;
        int count = Math.Max(1, visiblePlayerCount);
        int cardWidth = GetCardWidth(count);
        int cardHeight = GetCardHeight();
        int cardsWidth = cardWidth * count + CardGap * (count - 1);
        int headerHeight = showOwnTeamPanels ? TitleHeight + TitleScoreGap + ScoreHeight : ScoreHeight;
        int rewardHeight = showOwnTeamPanels ? RewardGap + RewardHeight : 0;
        int totalHeight = headerHeight + ScoreCardsGap + cardHeight + rewardHeight + BackButtonGap + BackButtonHeight;
        int top = totalHeight <= ViewH - LayoutMargin * 2 ? Math.Max(LayoutMargin, (ViewH - totalHeight) / 2) : LayoutMargin;

        int titleWidth = Math.Min(430, Math.Max(280, ViewW - 80));
        int scoreMaxWidth = Math.Max(255, ViewW - 80);
        int scoreWidth = Math.Clamp((int)ScorelineWidth(snapshot, scoreScale) + 64, 255, scoreMaxWidth);
        int rewardWidth = Math.Min(560, Math.Max(260, ViewW - 180));

        Rectangle title = showOwnTeamPanels ? CenteredBox(titleWidth, TitleHeight, top) : Rectangle.Empty;
        int scoreY = showOwnTeamPanels ? title.Bottom + TitleScoreGap : top;
        Rectangle score = CenteredBox(scoreWidth, ScoreHeight, scoreY);
        Rectangle cards = new((ViewW - cardsWidth) / 2, score.Bottom + ScoreCardsGap, cardsWidth, cardHeight);
        Rectangle reward = showOwnTeamPanels ? CenteredBox(rewardWidth, RewardHeight, cards.Bottom + RewardGap) : Rectangle.Empty;
        int backY = (showOwnTeamPanels ? reward.Bottom : cards.Bottom) + BackButtonGap;
        Rectangle backButton = CenteredBox(BackButtonWidth, BackButtonHeight, backY);

        return new EndScreenLayout(title, score, cards, reward, backButton, cardWidth, cardHeight);
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

    private readonly record struct EndScreenLayout(Rectangle TitleBox, Rectangle ScoreBox, Rectangle CardsBox, Rectangle RewardBox, Rectangle BackButtonBox, int CardWidth, int CardHeight);

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

    private static string Fit(string text, float width, float scale = 1f)
    {
        if (FontAssets.MouseText.Value.MeasureString(text).X * scale <= width)
            return text;

        while (text.Length > 1 && FontAssets.MouseText.Value.MeasureString(text + "..").X * scale > width)
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
        GlassPanelStyle style = EndScreenLayer.PurpleInset with { Primary = TeamTint, Secondary = TeamTint, Border = TeamTint };
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
