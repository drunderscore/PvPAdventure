using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common;
using PvPAdventure.System;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Core.SpawnAndSpectate.UI;

/// <summary>
/// A UI element representing a question mark button for random teleportation.
/// </summary>
public class RandomTeleportPanel : UIPanel
{
    public RandomTeleportPanel(float size)
    {
        Width.Set(size, 0f);
        Height.Set(size, 0f);

        BackgroundColor = new Color(63, 82, 151) * 0.8f;
        BorderColor = Color.Black;
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);

        var gm = ModContent.GetInstance<GameManager>();
        if (gm.CurrentPhase != GameManager.Phase.Playing)
            return;

        var respawnPlayer = Main.LocalPlayer.GetModPlayer<RespawnPlayer>();

        if (SpawnAndSpectateSystem.IsAliveSpawnRegionInstant)
        {
            respawnPlayer.RandomTeleport();
            return;
        }

        if (Main.LocalPlayer.dead)
        {
            respawnPlayer.ToggleCommitRandom();
        }
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
            string text;
            var respawnPlayer = Main.LocalPlayer.GetModPlayer<RespawnPlayer>();
            bool readyToRespawn = SpawnAndSpectateSystem.CanRespawn;
            bool committed = respawnPlayer.IsRandomCommitted;

            if (Main.LocalPlayer.dead && !readyToRespawn)
            {
                text = committed
                    ? Language.GetTextValue("Mods.PvPAdventure.SpawnAndSpectate.CancelRandomSpawn")
                    : Language.GetTextValue("Mods.PvPAdventure.SpawnAndSpectate.SelectRandomSpawn");
            }
            else
            {
                text = Language.GetTextValue("Mods.PvPAdventure.SpawnAndSpectate.Random");
            }

            Main.instance.MouseText(text);
        }

        // Draw question mark centered
        var d = GetDimensions();
        var tex = Ass.Question_Mark.Value;

        float baseScale = 0.9f;
        float hoverScale = 0.9f;

        float scale = IsMouseHovering ? hoverScale : baseScale;

        sb.Draw(
            tex,
            position: new(d.X + d.Width * 0.5f, d.Y + d.Height * 0.5f),
            sourceRectangle: null,
            color: Color.White,
            rotation: 0f,
            origin: tex.Size() * 0.5f,
            scale: scale,
            effects: SpriteEffects.None,
            layerDepth: 0f
        );

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

        if (!Main.LocalPlayer.dead)
        {
            BorderColor = Color.Black;
            return;
        }

        var respawnPlayer = Main.LocalPlayer?.GetModPlayer<RespawnPlayer>();
        bool committed = respawnPlayer != null && respawnPlayer.IsRandomCommitted;

        if (committed)
            BackgroundColor = Color.Yellow;
        else if (IsMouseHovering)
            BackgroundColor = new Color(73, 92, 161, 150);
        else
            BackgroundColor = new Color(63, 82, 151);
    }
}
