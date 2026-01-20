using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Core.Utilities;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;
using static PvPAdventure.Common.SpawnSelector.SpawnSystem;

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
        return Main.LocalPlayer.SpawnX != 1 && Main.LocalPlayer.SpawnY != -1;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        var sp = Main.LocalPlayer?.GetModPlayer<SpawnPlayer>();
        bool selected = sp?.SelectedType == SpawnType.MyBed;

        if (IsMouseHovering && HasBed())
        {
            SpectateSystem.TrySetHover(SpawnType.MyBed, Main.myPlayer);
        }
        else
        {
            SpectateSystem.ClearHoverIfMatch(SpawnType.MyBed, Main.myPlayer);
        }

        BackgroundColor =
            selected ? new Color(220, 220, 0) :
            !hasBed ? new Color(230, 40, 10) * 0.37f:
            IsMouseHovering ? new Color(73, 92, 161, 150) :
            new Color(63, 82, 151) * 0.8f;
    }

    public override void Draw(SpriteBatch sb)
    {
        base.Draw(sb);

        if (IsMouseHovering)
        {
            DrawHoverText();
        }

        // Draw bed icon
        var d = GetDimensions();
        Vector2 pos = new(
            d.X + d.Width * 0.5f,
            d.Y + d.Height * 0.5f
        );
        float scale = 1.25f;
        var bedIcon = new Item(ItemID.Bed);

        // Draw the item icon centered in the panel
        ItemSlot.DrawItemIcon(bedIcon, ItemSlot.Context.InventoryItem, sb, pos, scale, 31, Color.White);

        if (!hasBed)
        {
            Vector2 origin = Ass.Icon_Forbidden.Value.Size() * 0.5f;
            sb.Draw(Ass.Icon_Forbidden.Value,pos,null, Color.White,0f,origin,2f,SpriteEffects.None, 0f);
        }
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
