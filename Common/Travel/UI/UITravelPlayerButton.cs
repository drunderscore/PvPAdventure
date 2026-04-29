using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Travel.Portals;
using PvPAdventure.Content.Portals;
using PvPAdventure.Core.Utilities;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.Travel.UI;

/// <summary>
/// One player card split into player name, bed, and portal travel options.
/// </summary>
public class UITravelPlayerButton : UIPanel
{
    private readonly Player player;

    public UITravelPlayerButton(Player player, TravelTarget bedTarget, TravelTarget portalTarget, float width, float height)
    {
        this.player = player ?? Main.LocalPlayer;

        Width.Set(width, 0f);
        Height.Set(height, 0f);
        SetPadding(0f);

        BackgroundColor = Color.Transparent;
        BorderColor = Color.Black;

        float nameHeight = MathHelper.Clamp(height * 0.26f, 18f, 24f);
        float separatorWidth = 2f;
        float optionHeight = height - nameHeight;
        float optionWidth = (width - separatorWidth) * 0.5f;

        UITravelDestinationPanel bed = new(
            this.player,
            bedTarget,
            TextureAssets.Item[ItemID.Bed].Value,
            this.player.whoAmI == Main.myPlayer
                ? Language.GetTextValue("Mods.PvPAdventure.Travel.TeleportToMyBed")
                : Language.GetTextValue("Mods.PvPAdventure.Travel.TeleportToTeammatesBed", this.player.name),
            optionWidth,
            optionHeight,
            fadeBottomLeft: true,
            fadeBottomRight: false
        );
        bed.Top.Set(nameHeight, 0f);
        Append(bed);

        UITravelDestinationPanel portal = new(
            this.player,
            portalTarget,
            PortalAssets.GetPortalTexture(this.player.team),
            this.player.whoAmI == Main.myPlayer
                ? Language.GetTextValue("Mods.PvPAdventure.Travel.TeleportToMyPortal")
                : Language.GetTextValue("Mods.PvPAdventure.Travel.TeleportToTeammatesPortal", this.player.name),
            optionWidth,
            optionHeight,
            fadeBottomLeft: false,
            fadeBottomRight: true
        );
        portal.Left.Set(optionWidth + separatorWidth, 0f);
        portal.Top.Set(nameHeight, 0f);
        Append(portal);

        PlayerNamePanel name = new(this.player, width, nameHeight);
        Append(name);
    }

    private static void DrawTravelSeparator(SpriteBatch sb, Rectangle rect)
    {
        Texture2D texture = Main.Assets.Request<Texture2D>("Images/UI/CharCreation/Separator1").Value;

        int nameHeight = (int)MathHelper.Clamp(rect.Height * 0.26f, 18f, 24f);
        int top = rect.Y + nameHeight;
        int height = rect.Height - nameHeight - 6;

        if (height <= 0)
            return;

        float scaleX = 0.25f;
        float scaleY = height / (float)texture.Height;

        Vector2 position = new(
            rect.X + rect.Width * 0.5f,
            top
        );

        Vector2 origin = new(texture.Width * 0.5f, 0f);

        sb.Draw(texture, position, null, Color.White, 0f, origin, new Vector2(scaleX, scaleY), SpriteEffects.None, 0f);
    }

    //public override void Draw(SpriteBatch sb)
    //{
    //    if (player?.active != true)
    //        return;

    //    base.Draw(sb);

    //    Rectangle rect = GetDimensions().ToRectangle();
    //    DrawTravelSeparator(sb, rect);

    //    if (player.dead && player.respawnTimer > 0)
    //        DrawRespawnTimerAndDeadIcon(sb, player.respawnTimer, rect);
    //}

    public override void Draw(SpriteBatch sb)
    {
        if (player?.active != true)
            return;

        Rectangle rect = GetDimensions().ToRectangle();

        BiomeBackgroundDrawer.DrawMapFullscreenBackground(
            sb,
            rect,
            player.Center,
            fadePixels: 8,
            shrinkPadding: 0,
            player
        );

        base.Draw(sb);

        DrawTravelSeparator(sb, rect);

        if (player.dead && player.respawnTimer > 0)
            DrawRespawnTimerAndDeadIcon(sb, player.respawnTimer, rect);
    }

    private void DrawRespawnTimerAndDeadIcon(SpriteBatch sb, int respawnTimer, Rectangle rect)
    {
        Texture2D tex = Ass.Icon_Dead.Value;
        Vector2 skullCenter = new(rect.X + rect.Width * 0.5f, rect.Y + rect.Height * 0.5f);
        sb.Draw(tex, skullCenter, null, Color.White * 0.5f, 0f, tex.Size() * 0.5f, 1.44f, SpriteEffects.None, 0f);

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

            BackgroundColor = new Color(30, 35, 70) * 0.82f;
            BorderColor = Color.Black;
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
            Rectangle rect = GetDimensions().ToRectangle();

            Color overlay = IsMouseHovering ? new Color(73, 92, 161, 150) : Color.Transparent;

            if (overlay != Color.Transparent)
            {
                Rectangle highlightRect = rect;
                highlightRect.Inflate(2, 6);
                highlightRect.Y += 4;
                BiomeBackgroundDrawer.DrawFadedFill(sb, highlightRect, overlay, fadePixels: 8);
            }

            string name = player.whoAmI == Main.myPlayer ? "You" : player.name;
            //name = "Matte Sevai";

            float textScale = MathHelper.Clamp(rect.Height / 20f, 0.7f, 1f);
            float headScale = MathHelper.Clamp(rect.Height / 24f, 0.75f, 1.05f);
            float headWidth = 22f * headScale;
            float gap = 6f;

            Vector2 nameSize = FontAssets.MouseText.Value.MeasureString(name) * textScale;
            float totalWidth = headWidth + gap + nameSize.X;
            float startX = rect.X + (rect.Width - totalWidth) * 0.5f;

            Vector2 headPos = new(startX + headWidth * 0.5f, rect.Y + rect.Height * 0.5f);
            headPos += new Vector2(-4, 0);
            Vector2 namePos = new(startX + headWidth + gap, rect.Y + (rect.Height - nameSize.Y) * 0.5f + 4f);

            DrawPlayerHead(player, headPos, headScale);
            Utils.DrawBorderString(sb, name, namePos, Color.White, textScale);
        }

        private void DrawPlayerHead(Player player, Vector2 center, float scale)
        {
            if (player?.active != true)
                return;

            try
            {
                Color borderColor = player.team > 0 ? Main.teamColor[player.team] : Color.Black;
                Main.MapPlayerRenderer.DrawPlayerHead(Main.Camera, player, center, scale: scale, borderColor: borderColor);
            }
            catch (Exception e)
            {
                Log.Error("Failed to draw player head: " + e);
            }
        }
    }

    private sealed class UITravelDestinationPanel : UIPanel
    {
        private readonly Player player;
        private readonly TravelTarget target;
        private readonly Texture2D icon;
        private readonly string hoverText;
        private readonly bool fadeBottomLeft;
        private readonly bool fadeBottomRight;

        public UITravelDestinationPanel(Player player, TravelTarget target, Texture2D icon, string hoverText, float width, float height, bool fadeBottomLeft, bool fadeBottomRight)
        {
            this.player = player;
            this.target = target;
            this.icon = icon;
            this.hoverText = hoverText;
            this.fadeBottomLeft = fadeBottomLeft;
            this.fadeBottomRight = fadeBottomRight;

            Width.Set(width, 0f);
            Height.Set(height, 0f);
            SetPadding(0f);

            BackgroundColor = Color.Transparent;
            BorderColor = Color.Black;
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            base.LeftClick(evt);
            TravelTeleportSystem.ActivateTarget(target);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            BorderColor = IsMouseHovering ? Color.Yellow : Color.Black;

            if (IsMouseHovering)
            {
                Main.LocalPlayer.mouseInterface = true;

                if (target.Available)
                    TravelSpectateSystem.TrySetHover(target);
            }
            else
            {
                TravelSpectateSystem.ClearHoverIfMatch(target);
            }
        }

        protected override void DrawSelf(SpriteBatch sb)
        {
            Rectangle rect = GetDimensions().ToRectangle();
            Rectangle overlayRect = GetSplitColumnRect();
            bool selected = TravelTeleportSystem.IsSelected(target);

            Color overlay =
                selected ? new Color(220, 220, 0) * 0.8f :
                //!target.Available ? new Color(15, 15, 15) * 0.92f :
                IsMouseHovering ? new Color(180, 180, 180) * 0.5f :
                Color.Transparent;

            if (overlay != Color.Transparent)
                BiomeBackgroundDrawer.DrawFadedFill(sb, overlayRect, overlay, fadePixels: 10);
            
            Color iconColor = target.Available ? Color.White : new Color(95, 95, 105) * 0.8f;
            
            //iconColor = Color.White;

            float iconYOffset =
                target.Type == TravelType.Bed ? -4f :
                target.Type == TravelType.Portal ? -6f :
                0f;

            Vector2 position = new(rect.X + rect.Width * 0.5f, rect.Y + rect.Height * 0.58f + iconYOffset);

            int npcType = ModContent.NPCType<PortalNPC>();
            int frameCount = Main.npcFrameCount[npcType];
            bool isPortal = frameCount > 1 && icon == PortalAssets.GetPortalTexture(player.team);

            if (isPortal && frameCount > 0)
            {
                // portal
                int frameIndex = (int)(Main.GameUpdateCount / 5 % frameCount);
                Rectangle frame = icon.Frame(1, frameCount, 0, frameIndex);
                Vector2 origin = frame.Size() * 0.5f;
                float scale = MathHelper.Min(1.85f, MathHelper.Min((rect.Width - 8f) / frame.Width, (rect.Height - 16f) / frame.Height));
                scale *= 1.1f;
                sb.Draw(icon, position, frame, iconColor, 0f, origin, scale, SpriteEffects.None, 0f);
            }
            else
            {
                // bed
                Rectangle frame = icon.Frame();
                Vector2 origin = frame.Size() * 0.5f;
                float scale = MathHelper.Min(1.25f, MathHelper.Min((rect.Width - 12f) / frame.Width, (rect.Height - 24f) / frame.Height));
                scale *= 1.1f;
                sb.Draw(icon, position, frame, iconColor, 0f, origin, scale, SpriteEffects.None, 0f);
            }

            if (!target.Available)
            {
                Texture2D forbidden = Ass.Icon_Forbidden.Value;
                float forbiddenScale = target.Type == TravelType.Portal ? 1.6f : 1.6f;

                sb.Draw(forbidden, position, null, Color.PaleVioletRed, 0f, forbidden.Size() * 0.5f, forbiddenScale, SpriteEffects.None, 0f);
            }

            if (IsMouseHovering)
                Main.instance.MouseText(!target.Available ? target.DisabledReason : selected ? "Cancel selection" : hoverText);
        }

        // Get parent height.
        private Rectangle GetSplitColumnRect()
        {
            Rectangle parentRect = Parent.GetDimensions().ToRectangle();
            int halfWidth = parentRect.Width / 2;

            if (target.Type == TravelType.Bed)
                return new Rectangle(parentRect.X, parentRect.Y, halfWidth + 4, parentRect.Height);

            if (target.Type == TravelType.Portal)
                return new Rectangle(parentRect.X + halfWidth -4, parentRect.Y, parentRect.Width - halfWidth + 4, parentRect.Height);

            return GetDimensions().ToRectangle();
        }
    }
}