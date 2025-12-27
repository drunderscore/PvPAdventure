using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace PvPAdventure.Core.SpawnAndSpectate.UI;

public sealed class SpectateButton : UIElement
{
    private const float ButtonSize = 26f;
    private const float IconPadding = 6f;

    private readonly int playerIndex;
    private readonly Item binoculars;

    public SpectateButton(int playerIndex)
    {
        this.playerIndex = playerIndex;

        binoculars = new Item();
        binoculars.SetDefaults(ItemID.Binoculars);

        Width.Set(ButtonSize, 0f);
        Height.Set(ButtonSize, 0f);

        // Optional but nice: ensures the element doesn't reserve extra inner space.
        SetPadding(0f);
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);
        SpawnAndSpectateSystem.ToggleSpectateOnPlayerIndex(playerIndex);
    }

    protected override void DrawSelf(SpriteBatch sb)
    {
        var dims = GetDimensions();
        Rectangle rect = dims.ToRectangle();

        bool locked = SpawnAndSpectateSystem.SpectatePlayerIndex == playerIndex;

        Texture2D bg = TextureAssets.InventoryBack7.Value;
        if (locked)
            bg = TextureAssets.InventoryBack14.Value;
        else if (IsMouseHovering)
            bg = TextureAssets.InventoryBack15.Value;

        // Draw bg
        sb.Draw(bg, rect, Color.White);

        // Center icon within the button.
        Vector2 iconCenter = rect.Center.ToVector2();

        // Fit icon inside the button nicely.
        float sizeLimit = ButtonSize + 1;
        float iconScale = sizeLimit / 32f; 

        ItemSlot.DrawItemIcon(
            item: binoculars,
            context: ItemSlot.Context.InventoryItem,
            spriteBatch: sb,
            screenPositionForItemCenter: iconCenter,
            scale: iconScale,
            sizeLimit: sizeLimit,
            environmentColor: Color.White
        );

        if (IsMouseHovering)
        {
            Main.LocalPlayer.mouseInterface = true;

            Player p = Main.player[playerIndex];
            string name = p != null && p.active ? p.name : "player";

            // Locked means "currently spectating", so the action is "stop spectating".
            string text = locked
                ? Language.GetTextValue("Mods.PvPAdventure.SpawnAndSpectate.StopSpectatingPlayer", name)
                : Language.GetTextValue("Mods.PvPAdventure.SpawnAndSpectate.SpectatePlayer", name);

            Main.instance.MouseText(text);
        }
    }
}
