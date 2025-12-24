using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common;
using PvPAdventure.System;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Core.SpawnSelector.UI;

/// <summary>
/// A UI element representing a question mark button for random teleportation.
/// </summary>
internal class SpawnSelectorQuestionMark : UIPanel
{
    public SpawnSelectorQuestionMark(float startX, float itemHeight, int playerCount, float itemWidth, float Spacing, float y)
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

        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            NetMessage.SendData(MessageID.RequestTeleportationByServer);
        }
        else
        {
            Main.LocalPlayer.TeleportationPotion();
        }

        Main.mapFullscreen = false;
        SpawnSelectorSystem.SetEnabled(false);
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
            Main.instance.MouseText(Language.GetTextValue("Mods.PvPAdventure.SpawnSelector.RandomTeleport"));
        }

        // Draw question mark
        var d = GetDimensions();
        var tex = Ass.Question_Mark.Value;
        var rect = new Rectangle(
            (int)(d.X + (d.Width - tex.Width) * 0.5f),
            (int)(d.Y + (d.Height - tex.Height) * 0.5f),
            tex.Width,
            tex.Height
        );
        sb.Draw(tex, rect, Color.White);

        // debug
        //sb.Draw(TextureAssets.MagicPixel.Value, rect, Color.Red * 0.45f);

        // Draw teleportation potion
        Item icon = new(ItemID.TeleportationPotion);
        Vector2 pos = new(rect.X + 37, rect.Y + 36);
        //ItemSlot.DrawItemIcon(icon, 31, sb, pos, 1.0f, 32f, Color.White);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
    }
}
