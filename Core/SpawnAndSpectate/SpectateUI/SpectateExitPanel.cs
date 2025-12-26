using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common;
using PvPAdventure.System;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Core.SpawnAndSpectate.SpectateUI;

/// <summary>
/// A UI element representing a exit button to leave spectate mode.
/// </summary>
internal class SpectateExitPanel : UIPanel
{
    public SpectateExitPanel(float startX, float itemHeight, int playerCount, float itemWidth, float Spacing, float y)
    {
        Width.Set(itemHeight, 0f);
        Height.Set(itemHeight, 0f);
        Top.Set(y, 0f);

        float randomX = startX + playerCount * (itemWidth + Spacing);
        Left.Set(randomX, 0f);

        BackgroundColor = new Color(63, 82, 151) * 0.8f;
        BorderColor = Color.Black;
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);

        // Redundant check, just to be sure we don't allow any shenanigans
        var gm = ModContent.GetInstance<GameManager>();
        if (gm.CurrentPhase != GameManager.Phase.Playing)
            return;

        // Exit spectate mode
        var sys = ModContent.GetInstance<SpawnAndSpectateSystem>();
        sys.ui.SetState(null);
    }

    public override void MouseOver(UIMouseEvent evt)
    {
        base.MouseOver(evt);
        BackgroundColor = new Color(73, 92, 161, 150);
    }

    public override void MouseOut(UIMouseEvent evt)
    {
        base.MouseOut(evt);
        BackgroundColor = new Color(63, 82, 151) * 0.8f;
    }

    public override void Draw(SpriteBatch sb)
    {
        base.Draw(sb);

        if (IsMouseHovering)
        {
            Main.instance.MouseText(Language.GetTextValue("Mods.PvPAdventure.SpawnAndSpectate.ExitSpectate"));
        }

        // Draw exit icon
        var d = GetDimensions();
        var tex = Ass.Stop_Icon.Value;
        var rect = new Rectangle(
            (int)(d.X + (d.Width - tex.Width) * 0.25f),
            (int)(d.Y + (d.Height - tex.Height) * 0.25f),
            tex.Width*2,
            tex.Height*2
        );
        sb.Draw(tex, rect, Color.White);

        // Debug
        //sb.Draw(TextureAssets.MagicPixel.Value, rect, Color.Red * 0.45f);

        // Draw teleportation potion
        //Item icon = new(ItemID.TeleportationPotion);
        //Vector2 pos = new(rect.X + 37, rect.Y + 36);
        //ItemSlot.DrawItemIcon(icon, 31, sb, pos, 1.0f, 32f, Color.White);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
    }
}
