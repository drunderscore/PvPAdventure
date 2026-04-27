using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.World.Outlines.ItemOutlines;
using PvPAdventure.Core.Utilities;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.SpawnSelector.UI;

public class UIMyBedButton : UIPanel
{
    private bool hasBed;
    public UIMyBedButton(float size, bool hasBed)
    {
        this.hasBed = hasBed;

        Width.Set(size, 0f);
        Height.Set(size, 0f);

        BackgroundColor = new Color(63, 82, 151) * 0.8f;
        BorderColor = Color.Black;
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);

        if (HasBed())
        {
            Main.LocalPlayer.GetModPlayer<SpawnPlayer>().ToggleSelection(SpawnType.MyBed);
        }
    }

    private bool HasBed()
    {
        Player local = Main.LocalPlayer;
        return local?.active == true && local.SpawnX >= 0 && local.SpawnY >= 0 && Player.CheckSpawn(local.SpawnX, local.SpawnY);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        bool nowHasBed = HasBed();
        if (hasBed != nowHasBed)
            hasBed = nowHasBed;

        var sp = Main.LocalPlayer?.GetModPlayer<SpawnPlayer>();
        bool selected = sp?.SelectedType == SpawnType.MyBed;
        bool cooldown = SpawnSystem.IsLocalPlayerOnTeleportCooldown;

        if (IsMouseHovering && HasBed())
        {
            SpectateSystem.TrySetHover(SpawnType.MyBed, Main.myPlayer);
        }
        else
        {
            SpectateSystem.ClearHoverIfMatch(SpawnType.MyBed, Main.myPlayer);
        }

        BackgroundColor =
            !hasBed ? SpawnSystem.DisabledButtonColor :
            selected ? new Color(220, 220, 0) :
            cooldown ? SpawnSystem.DisabledButtonColor :
            IsMouseHovering ? new Color(73, 92, 161, 150) :
            new Color(63, 82, 151) * 0.8f;
    }

    public override void Draw(SpriteBatch sb)
    {
        base.Draw(sb);

        if (IsMouseHovering)
            DrawHoverText();

        var d = GetDimensions();
        Vector2 pos = new(d.X + d.Width * 0.5f, d.Y + d.Height * 0.5f);

        float iconScale = 1.25f;
        const int iconSize = 31;

        var bedItem = new Item(ItemID.Bed);

        // Draw bed outline
        if (hasBed)
        {
            Player local = Main.LocalPlayer;
            if (local != null && local.active && local.team != 0)
            {
                Color borderColor = Main.teamColor[local.team];
                int drawW = (int)(iconSize * iconScale);
                int drawH = (int)(iconSize * iconScale);
                var outlineSys = ModContent.GetInstance<ItemOutlineSystem>();
                if (outlineSys != null && outlineSys.TryGet(bedItem.type, drawW, drawH, borderColor, out RenderTarget2D rt, out Vector2 rtOrigin))
                {
                    sb.Draw(rt, pos, null, Color.White, 0f, rtOrigin, 1.17f, SpriteEffects.None, 0f);
                }
            }
        }


        // Draw icon on top
        ItemSlot.DrawItemIcon(bedItem, ItemSlot.Context.InventoryItem, sb, pos, iconScale, iconSize, Color.White);

        if (!hasBed || SpawnSystem.IsLocalPlayerOnTeleportCooldown)
            SpawnSystem.DrawForbiddenIcon(sb, pos, 2f);
    }


    private void DrawHoverText()
    {
        Player p = Main.LocalPlayer;
        if (p == null || !p.active)
            return;

        // Prevent clicks while hovering the UI element.
        p.mouseInterface = true;

        var sp = p.GetModPlayer<SpawnPlayer>();

        bool committed = sp.SelectedType == SpawnType.MyBed;
        bool ready = !SpawnSystem.CanTeleport;

        string text;

        if (!HasBed())
        {
            text = "No bed set";
        }
        else if (SpawnSystem.IsLocalPlayerOnTeleportCooldown)
        {
            text = SpawnSystem.LocalTeleportCooldownText;
        }
        else if (ready)
        {
            text = committed
                ? Language.GetTextValue("Mods.PvPAdventure.Spawn.CancelMyBed", Main.LocalPlayer.name)
                : Language.GetTextValue("Mods.PvPAdventure.Spawn.SelectMyBed", Main.LocalPlayer.name);
        }
        else
        {
            text = Language.GetTextValue("Mods.PvPAdventure.Spawn.TeleportToMyBed", Main.LocalPlayer.name);
        }

        Main.instance.MouseText(text);
    }
}
