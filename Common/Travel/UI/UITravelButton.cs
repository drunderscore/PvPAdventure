using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Utilities;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace PvPAdventure.Common.Travel.UI;

/// <summary>
/// Small clickable travel option used by world, random, bed, and portal buttons.
/// </summary>
public class UITravelButton : UIPanel
{
    private readonly TravelTarget target;
    private readonly Texture2D icon;
    private readonly string hoverText;

    public UITravelButton(TravelTarget target, Texture2D icon, string label, string hoverText, float width, float height)
    {
        this.target = target;
        this.icon = icon;
        this.hoverText = hoverText;

        Width.Set(width, 0f);
        Height.Set(height, 0f);
        SetPadding(0f);

        BackgroundColor = new Color(63, 82, 151) * 0.8f;
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
        bool selected = TravelTeleportSystem.IsSelected(target);

        BackgroundColor =
            selected ? new Color(220, 220, 0) :
            //!target.Available ? new Color(45, 45, 55) * 0.8f :
            IsMouseHovering ? new Color(73, 92, 161, 150) :
            new Color(63, 82, 151) * 0.8f;

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

    public override void Draw(SpriteBatch sb)
    {
        base.Draw(sb);

        CalculatedStyle d = GetDimensions();
        Rectangle rect = d.ToRectangle();
        Rectangle bgRect = rect;

        Player bgPlayer = target.PlayerIndex >= 0 && target.PlayerIndex < Main.maxPlayers && Main.player[target.PlayerIndex]?.active == true
            ? Main.player[target.PlayerIndex]
            : Main.LocalPlayer;

        if (target.Type == TravelType.Random)
        {
            BiomeBackgroundDrawer.DrawMapFullscreenBackground(
                sb,
                bgRect,
                mapBgIndex: 7,
                fadePixels: 5,
                shrinkPadding: 0
            );
        }
        else if (target.Type == TravelType.World && target.WorldPosition != Vector2.Zero)
        {
            BiomeBackgroundDrawer.DrawMapFullscreenBackground(
                sb,
                bgRect,
                target.WorldPosition,
                fadePixels: 8,
                shrinkPadding: 0,
                null
            );
        }

        Rectangle highlightRect = rect;
        highlightRect.Inflate(-2, -2);

        if (!target.Available)
            sb.Draw(TextureAssets.MagicPixel.Value, highlightRect, new Color(35, 35, 35) * 0.7f);

        Color color = target.Available ? Color.White : Color.Gray;

        Rectangle iconFrame = icon.Frame();
        Vector2 iconOrigin = iconFrame.Size() * 0.5f;
        Vector2 iconPos = new(d.X + d.Width * 0.5f, d.Y + d.Height * 0.5f);

        float iconScaleMultiplier = target.Type switch
        {
            TravelType.World => 0.9f,
            TravelType.Random => 1.1f,
            _ => 1f
        };

        float iconScale = MathHelper.Min(1.6f, MathHelper.Min((d.Width - 16f) / iconFrame.Width, (d.Height - 16f) / iconFrame.Height)) * iconScaleMultiplier;

        sb.Draw(icon, iconPos, iconFrame, color, 0f, iconOrigin, iconScale, SpriteEffects.None, 0f);

        if (!target.Available)
        {
            Texture2D forbidden = Ass.Icon_Forbidden.Value;
            sb.Draw(forbidden, iconPos, null, Color.White, 0f, forbidden.Size() * 0.5f, 1.25f, SpriteEffects.None, 0f);
        }

        if (IsMouseHovering)
            Main.instance.MouseText(!target.Available ? target.DisabledReason : TravelTeleportSystem.IsSelected(target) ? "Cancel selection" : hoverText);
    }
}