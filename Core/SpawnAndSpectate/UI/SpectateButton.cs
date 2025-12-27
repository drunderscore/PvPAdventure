using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;

namespace PvPAdventure.Core.SpawnAndSpectate.UI;

public sealed class SpectateButton : UIElement
{
    private readonly int playerIndex;
    private readonly Item binoculars;

    public SpectateButton(int playerIndex)
    {
        this.playerIndex = playerIndex;

        binoculars = new Item();
        binoculars.SetDefaults(ItemID.Binoculars);

        Width.Set(34f, 0f);
        Height.Set(34f, 0f);
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);

        SpawnAndSpectateSystem.ToggleSpectateOnPlayerIndex(playerIndex);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        var dims = GetDimensions();
        Rectangle rect = dims.ToRectangle();

        bool locked = SpawnAndSpectateSystem.SpectatePlayerIndex == playerIndex;

        Texture2D bg = TextureAssets.InventoryBack7.Value;
        if (locked)
            bg = TextureAssets.InventoryBack14.Value;
        else if (IsMouseHovering)
            bg = TextureAssets.InventoryBack15.Value;

        spriteBatch.Draw(bg, rect, Color.White);

        Vector2 iconCenter = new(rect.X + rect.Width * 0.5f, rect.Y + rect.Height * 0.5f);
        ItemSlot.DrawItemIcon(binoculars, 31, spriteBatch, iconCenter, 1f, 32f, Color.White);

        if (IsMouseHovering)
        {
            Main.LocalPlayer.mouseInterface = true;

            Player p = Main.player[playerIndex];
            string name = p != null && p.active ? p.name : "player";

            if (locked)
                Main.instance.MouseText("Stop spectating " + name);
            else
                Main.instance.MouseText("Spectate " + name);
        }
    }
}
