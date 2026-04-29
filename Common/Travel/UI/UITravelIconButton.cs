using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Utilities;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace PvPAdventure.Common.Travel.UI;

internal sealed class UITravelIconButton : UIPanel
{
    private readonly TravelTarget target;
    private readonly Func<Texture2D> textureProvider;
    private readonly string hoverText;
    private readonly int? forcedMapBgIndex;
    private readonly float iconScaleMultiplier;

    public UITravelIconButton(TravelTarget target, Func<Texture2D> textureProvider, string hoverText, float width, float height, int? forcedMapBgIndex, float iconScaleMultiplier)
    {
        this.target = target;
        this.textureProvider = textureProvider;
        this.hoverText = hoverText;
        this.forcedMapBgIndex = forcedMapBgIndex;
        this.iconScaleMultiplier = iconScaleMultiplier;

        Width.Set(width, 0f);
        Height.Set(height, 0f);
        SetPadding(0f);

        BackgroundColor = new Color(33, 43, 79) * 0.72f;
        BorderColor = Color.Black;
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);

        if (target.Available)
            TravelTeleportSystem.ActivateTarget(target);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        bool selected = TravelTeleportSystem.IsSelected(target);
        bool hoverable = target.Available || target.Type is not (TravelType.Bed or TravelType.Portal);

        BorderColor =
            selected ? Color.Yellow :
            hoverable && IsMouseHovering ? Color.Yellow :
            Color.Black;

        if (IsMouseHovering)
        {
            Main.LocalPlayer.mouseInterface = true;

            if (target.Available)
                TravelSpectateSystem.TrySetHover(target);
            else if (target.Type is TravelType.Bed or TravelType.Portal)
                TravelSpectateSystem.ClearHoverIfMatch(target);
        }
        else
        {
            TravelSpectateSystem.ClearHoverIfMatch(target);
        }
    }

    protected override void DrawSelf(SpriteBatch sb)
    {
        Rectangle rect = GetDimensions().ToRectangle();
        base.DrawSelf(sb);

        DrawBackground(sb, rect);
        DrawIcon(sb, rect);
        DrawForbiddenIcon(sb, rect);
        DrawTooltip();
    }

    private void DrawBackground(SpriteBatch sb, Rectangle rect)
    {
        if (!target.Available && target.Type is TravelType.Bed or TravelType.Portal)
            return;

        Color color =
         TravelTeleportSystem.IsSelected(target) ? Color.Yellow :
         target.Available ? Color.White :
         new Color(55, 55, 55) * 0.65f;

        if (forcedMapBgIndex.HasValue)
        {
            BiomeBackgroundDrawer.DrawMapFullscreenBackground(sb, rect, forcedMapBgIndex.Value, fadePixels: 5, shrinkPadding: 0, overrideColor: color);
            return;
        }

        BiomeBackgroundDrawer.DrawMapFullscreenBackground(sb, rect, target.WorldPosition, fadePixels: 8, shrinkPadding: 0, overrideColor: color);
    }

    private void DrawIcon(SpriteBatch sb, Rectangle rect)
    {
        Texture2D icon = textureProvider();

        if (icon == null)
            return;

        int frameCount = target.Type == TravelType.Portal ? 8 : 1;
        int frameIndex = frameCount > 1 ? (int)(Main.GameUpdateCount / 5 % frameCount) : 0;
        Rectangle frame = icon.Frame(1, frameCount, 0, frameIndex);
        Vector2 origin = frame.Size() * 0.5f;
        Vector2 position = new(rect.X + rect.Width * 0.5f, rect.Y + rect.Height * 0.5f);

        float fitScale = MathHelper.Min((rect.Width - 18f) / frame.Width, (rect.Height - 18f) / frame.Height);
        float scale = MathHelper.Min(1.8f, fitScale) * iconScaleMultiplier;
        Color color = target.Available ? Color.White : new Color(95, 95, 105) * 0.8f;
        //Color color = target.Available ? Color.White : new Color(75, 75, 75) * 0.72f;
        sb.Draw(icon, position, frame, color, 0f, origin, scale, SpriteEffects.None, 0f);
    }

    private void DrawTooltip()
    {
        if (!IsMouseHovering)
            return;

        string text = !target.Available ? target.DisabledReason : TravelTeleportSystem.IsSelected(target) ? "Cancel selection" : hoverText;
        Main.instance.MouseText(text);
    }

    private void DrawForbiddenIcon(SpriteBatch sb, Rectangle rect)
    {
        if (target.Available)
            return;

        Texture2D forbidden = Ass.Icon_Forbidden.Value;
        Vector2 position = new(rect.X + rect.Width * 0.5f, rect.Y + rect.Height * 0.5f);

        sb.Draw(forbidden, position, null, Color.PaleVioletRed, 0f, forbidden.Size() * 0.5f, 1.45f, SpriteEffects.None, 0f);
    }
}