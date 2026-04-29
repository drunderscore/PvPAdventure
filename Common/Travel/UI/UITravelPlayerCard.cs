using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Travel.Portals;
using PvPAdventure.Core.Utilities;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.Travel.UI;

internal sealed class UITravelPlayerCard : UIPanel
{
    private readonly Player player;

    public UITravelPlayerCard(Player player, TravelTarget bedTarget, TravelTarget portalTarget, float unitWidth, float fullHeight, float innerSpacing)
    {
        this.player = player ?? Main.LocalPlayer;

        float nameHeight = MathHelper.Clamp(fullHeight * 0.34f, 24f, 34f);
        float destinationHeight = fullHeight - nameHeight - innerSpacing;
        float width = unitWidth * 2f + innerSpacing;

        Width.Set(width, 0f);
        Height.Set(fullHeight, 0f);
        SetPadding(0f);

        //BackgroundColor = Color.Transparent;
        BackgroundColor = new Color(33, 43, 79) * 0.72f;
        BorderColor = Color.Black;

        PlayerNamePanel name = new(this.player, width, nameHeight);
        Append(name);

        UITravelIconButton bed = new(
            bedTarget,
            () => TextureAssets.Item[ItemID.Bed].Value,
            this.player.whoAmI == Main.myPlayer ? Language.GetTextValue("Mods.PvPAdventure.Travel.TeleportToMyBed") : Language.GetTextValue("Mods.PvPAdventure.Travel.TeleportToTeammatesBed", this.player.name),
            unitWidth,
            destinationHeight,
            bedTarget.WorldPosition,
            backgroundZonePlayer: null,
            null,
            0.8f
        );

        bed.Top.Set(nameHeight + innerSpacing, 0f);
        Append(bed);

        UITravelIconButton portal = new(
            portalTarget,
            () => PortalAssets.GetPortalTexture(this.player.team),
            this.player.whoAmI == Main.myPlayer ? Language.GetTextValue("Mods.PvPAdventure.Travel.TeleportToMyPortal") : Language.GetTextValue("Mods.PvPAdventure.Travel.TeleportToTeammatesPortal", this.player.name),
            unitWidth,
            destinationHeight,
            portalTarget.WorldPosition,
            null,
            null,
            1.18f
        );

        portal.Left.Set(unitWidth + innerSpacing, 0f);
        portal.Top.Set(nameHeight + innerSpacing, 0f);
        Append(portal);
    }

    public override void Draw(SpriteBatch sb)
    {
        if (player?.active != true)
            return;

        base.Draw(sb);

        if (player.dead && player.respawnTimer > 0)
            DrawRespawnTimerAndDeadIcon(sb, player.respawnTimer, GetDimensions().ToRectangle());
    }

    private void DrawRespawnTimerAndDeadIcon(SpriteBatch sb, int respawnTimer, Rectangle rect)
    {
        Texture2D texture = Ass.Icon_Dead.Value;
        Vector2 skullCenter = new(rect.X + rect.Width * 0.5f, rect.Y + rect.Height * 0.5f);

        sb.Draw(texture, skullCenter, null, Color.White * 0.5f, 0f, texture.Size() * 0.5f, 1.44f, SpriteEffects.None, 0f);

        string seconds = respawnTimer <= 2 ? "0" : (respawnTimer / 60 + 1).ToString();
        Vector2 size = FontAssets.DeathText.Value.MeasureString(seconds);
        Vector2 pos = new(rect.X + (rect.Width - size.X) * 0.5f, rect.Y + (rect.Height - size.Y) * 0.5f + 10f);

        Utils.DrawBorderStringBig(sb, seconds, pos, Color.LightGray);
    }

    private sealed class PlayerNamePanel : UIPanel
    {
        private readonly Player player;

        public PlayerNamePanel(Player player, float width, float height)
        {
            this.player = player;

            Width.Set(width, 0f);
            Height.Set(height, 0f);
            SetPadding(0f);

            //BackgroundColor = new Color(30, 35, 70) * 0.92f;
            BackgroundColor = new Color(63, 82, 151) * 0.82f;
            BorderColor = Color.Black;
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            base.LeftClick(evt);

            if (player?.active == true)
                TravelSpectateSystem.TrySetPlayerHover(player.whoAmI);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            BorderColor = IsMouseHovering ? Color.Yellow : Color.Black;

            if (IsMouseHovering)
            {
                Main.LocalPlayer.mouseInterface = true;
                TravelSpectateSystem.TrySetPlayerHover(player.whoAmI);
            }
            else
            {
                TravelSpectateSystem.ClearPlayerHoverIfMatch(player.whoAmI);
            }
        }

        protected override void DrawSelf(SpriteBatch sb)
        {
            base.DrawSelf(sb);

            Rectangle rect = GetDimensions().ToRectangle();

            if (IsMouseHovering)
            {
                Rectangle hoverRect = rect;
                hoverRect.Inflate(-3, -3);
                BiomeBackgroundDrawer.DrawFadedFill(sb, hoverRect, new Color(73, 92, 161, 150), fadePixels: 6);
            }

            DrawPlayerInfo(sb, rect);
        }

        private void DrawPlayerInfo(SpriteBatch sb, Rectangle rect)
        {
            string name = player.whoAmI == Main.myPlayer ? "You" : player.name;
            //name = "matte sevai";

            string hp = $"{player.statLife} HP";

            float textScale = MathHelper.Clamp(rect.Height / 28f, 0.75f, 1f);
            float headScale = MathHelper.Clamp(rect.Height / 28f, 0.75f, 1f);
            float padding = 14f;
            float gap = 7f;

            Vector2 headPos = new(rect.X + padding + 0f, rect.Y + rect.Height * 0.5f);
            Vector2 nameSize = FontAssets.MouseText.Value.MeasureString(name) * textScale;
            Vector2 namePos = new(headPos.X + 12f + gap, rect.Y + (rect.Height - nameSize.Y) * 0.5f + 4f);

            DrawPlayerHead(player, headPos, headScale);
            Utils.DrawBorderString(sb, name, namePos, Color.White, textScale);

            Texture2D heart = TextureAssets.Heart.Value;
            Rectangle heartFrame = heart.Frame();
            float heartScale = MathHelper.Clamp(rect.Height / 34f, 0.65f, 0.9f);
            float rightPadding = 6f;
            float heartGap = 2f;
            if (name.Length >= 14)
            {
                rightPadding = 2f;
                heartGap = 0f;
            }

            Vector2 hpSize = FontAssets.MouseText.Value.MeasureString(hp) * textScale;
            Vector2 hpPos = new(rect.Right - rightPadding - hpSize.X, rect.Y + (rect.Height - hpSize.Y) * 0.5f + 4f);
            Vector2 heartPos = new(hpPos.X - heartGap - heartFrame.Width * heartScale * 0.5f, rect.Y + rect.Height * 0.5f + 1f);

            sb.Draw(heart, heartPos, heartFrame, Color.White, 0f, heartFrame.Size() * 0.5f, heartScale, SpriteEffects.None, 0f);
            Utils.DrawBorderString(sb, hp, hpPos, Color.White, textScale);
        }

        private void DrawPlayerHead(Player player, Vector2 center, float scale)
        {
            if (player?.active != true)
                return;

            Color borderColor = player.team > 0 ? Main.teamColor[player.team] : Color.Black;
            Main.MapPlayerRenderer.DrawPlayerHead(Main.Camera, player, center, scale: scale, borderColor: borderColor);
        }
    }
}