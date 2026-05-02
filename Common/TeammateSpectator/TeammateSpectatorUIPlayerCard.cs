using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Spectator.Drawers;
using PvPAdventure.Core.Utilities;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace PvPAdventure.Common.TeammateSpectator;

internal sealed class TeammateSpectatorUIPlayerCard : UIPanel
{
    public const float CardWidth = 150f;
    public const float CardHeight = 34f;

    private readonly int playerIndex;
    private readonly float scale;

    internal TeammateSpectatorUIPlayerCard(int playerIndex, float scale)
    {
        this.playerIndex = playerIndex;
        this.scale = scale;

        Width.Set(CardWidth * scale, 0f);
        Height.Set(CardHeight * scale, 0f);
        SetPadding(0f);

        BackgroundColor = new Color(63, 82, 151) * 0.82f;
        BorderColor = Color.Black;
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);

        if (CanLockSpectate())
            TeammateSpectateSystem.TogglePlayerHudLock(playerIndex);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        Player player = GetPlayer();
        bool locked = TeammateSpectateSystem.IsPlayerHudLocked(playerIndex);

        BackgroundColor = locked
            ? new Color(73, 94, 171)
            : IsMouseHovering
                ? new Color(73, 94, 171) * 0.95f
                : new Color(63, 82, 151) * 0.82f;

        BorderColor = locked
            ? Color.Yellow
            : IsMouseHovering
                ? Color.Yellow
                : Color.Black;

        if (IsMouseHovering)
        {
            Main.LocalPlayer.mouseInterface = true;

            if (CanLockSpectate(player))
                TeammateSpectateSystem.TrySetPlayerHover(playerIndex);
            else
                TeammateSpectateSystem.ClearPlayerHoverIfMatch(playerIndex);
        }
        else
        {
            TeammateSpectateSystem.ClearPlayerHoverIfMatch(playerIndex);
        }
    }

    protected override void DrawSelf(SpriteBatch sb)
    {
        base.DrawSelf(sb);

        Player player = GetPlayer();

        if (player is null || !player.active)
            return;

        Rectangle rect = GetDimensions().ToRectangle();
        bool locked = TeammateSpectateSystem.IsPlayerHudLocked(playerIndex);

        if (locked || IsMouseHovering)
            DrawHighlight(sb, rect, IsMouseHovering ? 0.18f : 0.1f);

        DrawPlayerInfo(sb, player, rect);
        DrawTooltip(player);

        if (player.dead && player.respawnTimer > 0)
            DrawRespawnTimerAndDeadIcon(sb, player.respawnTimer, rect);
    }

    private static void DrawHighlight(SpriteBatch sb, Rectangle rect, float opacity)
    {
        Rectangle hoverRect = rect;
        hoverRect.Inflate(-3, -3);
        sb.Draw(TextureAssets.MagicPixel.Value, hoverRect, Color.Yellow * opacity);
    }

    private void DrawPlayerInfo(SpriteBatch sb, Player player, Rectangle rect)
    {
        string name = player.name;
        float textScale = MathHelper.Clamp(rect.Height / 28f, 0.75f, 1f);
        float headScale = MathHelper.Clamp(rect.Height / 28f, 0.75f, 1f);
        float leftPadding = 14f * scale;
        float gap = 7f * scale;

        Vector2 headPos = new(rect.X + leftPadding, rect.Y + rect.Height * 0.5f - 4f * scale);
        Vector2 nameStart = new(headPos.X + 12f * scale + gap, rect.Y);
        float maxTextWidth = rect.Right - nameStart.X - 8f * scale;
        string displayName = StatDrawer.Truncate(FontAssets.MouseText.Value, name, maxTextWidth, textScale);
        Vector2 nameSize = FontAssets.MouseText.Value.MeasureString(displayName) * textScale;
        Vector2 namePos = new(nameStart.X, rect.Y + (rect.Height - nameSize.Y) * 0.5f + 4f * scale);

        DrawPlayerHead(player, headPos, headScale);
        Utils.DrawBorderString(sb, displayName, namePos, Color.White, textScale);
    }

    private static void DrawPlayerHead(Player player, Vector2 center, float scale)
    {
        if (player?.active != true)
            return;

        Color borderColor = player.team > 0 ? Main.teamColor[player.team] : Color.Black;
        Main.MapPlayerRenderer.DrawPlayerHead(Main.Camera, player, center, scale: scale, borderColor: borderColor);
    }

    private void DrawTooltip(Player player)
    {
        if (!IsMouseHovering || !CanLockSpectate(player))
            return;

        //string text = TeammateSpectateSystem.IsPlayerHudLocked(playerIndex)
        //    ? $"Click to stop spectating {player.name}"
        //    : $"Click to spectate {player.name}";

        //Main.instance.MouseText(text);
    }

    private static void DrawRespawnTimerAndDeadIcon(SpriteBatch sb, int respawnTimer, Rectangle rect)
    {
        Texture2D texture = Ass.Icon_Dead.Value;
        Vector2 skullCenter = new(rect.X + rect.Width * 0.5f, rect.Y + rect.Height * 0.5f);

        sb.Draw(texture, skullCenter, null, Color.White * 0.5f, 0f, texture.Size() * 0.5f, 0.68f, SpriteEffects.None, 0f);

        string seconds = respawnTimer <= 2 ? "0" : (respawnTimer / 60 + 1).ToString();
        Vector2 size = FontAssets.DeathText.Value.MeasureString(seconds) * 0.45f;
        Vector2 pos = new(rect.X + (rect.Width - size.X) * 0.5f, rect.Y + (rect.Height - size.Y) * 0.5f + 4f);

        Utils.DrawBorderString(sb, seconds, pos, Color.LightGray, 0.45f);
    }

    private Player GetPlayer()
    {
        if (playerIndex < 0 || playerIndex >= Main.maxPlayers)
            return null;

        return Main.player[playerIndex];
    }

    private bool CanLockSpectate()
    {
        return CanLockSpectate(GetPlayer());
    }

    private bool CanLockSpectate(Player player)
    {
        return player?.active == true && player.whoAmI != Main.myPlayer;
    }
}
