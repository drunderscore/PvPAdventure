using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace PvPAdventure.Core.Arenas.UI.LoadoutUI;

public sealed class LoadoutListItem : UIPanel
{
    private readonly UICharacter preview;
    private readonly UIText nameText;
    private readonly UIElement slotsRow;

    public LoadoutListItem(Player previewPlayer, LoadoutDef def, Action<string> equip)
    {
        Height.Set(72f, 0f);
        Width.Set(0f, 1f);
        SetPadding(6f);

        BackgroundColor = new Color(63, 82, 151) * 0.7f;
        BorderColor = new Color(89, 116, 213) * 0.7f;

        preview = new UICharacter(previewPlayer, animated: false, hasBackPanel: true, 0.8f, useAClone: true);
        preview.Left.Set(4f, 0f);
        Append(preview);

        nameText = new UIText(def.Name, 1.0f);
        nameText.Left.Set(72f, 0f);
        nameText.VAlign = 0.01f;
        Append(nameText);

        slotsRow = new UIElement();
        slotsRow.Left.Set(72f, 0f);
        slotsRow.Top.Set(-8, 0);
        slotsRow.VAlign = 1;
        slotsRow.Height.Set(26f, 0f);
        slotsRow.Width.Set(-96f, 1f);
        Append(slotsRow);

        AddSlots(def);

        var playButton = new UIImageButton(Main.Assets.Request<Texture2D>("Images/UI/ButtonPlay"))
        {
            VAlign = 0.5f,
            Left = { Pixels = -20f, Precent = 1f }
        };
        playButton.OnLeftClick += (_, _) => equip?.Invoke(def.Name);
        playButton.OnMouseOver += (_, _) => Main.instance.MouseText("Play");
        Append(playButton);
    }

    private void AddSlots(LoadoutDef def)
    {
        float x = 0f;

        // Head slot
        if (def.Head > 0)
        {
            var item = new Item();
            item.SetDefaults(def.Head);

            var slot = new UILoadoutItemSlot(item);
            slot.Left.Set(x, 0f);
            slotsRow.Append(slot);

            x += 52;
        }

        // First 2 Hotbar items
        for (int i = 0; i < def.Hotbar.Count && i < 2; i++)
        {
            var li = def.Hotbar[i];

            // Skip heal potion
            if (li.Type == ItemID.GreaterHealingPotion)
                continue;

            var item = new Item();
            item.SetDefaults(li.Type);
            item.stack = li.Stack;

            var slot = new UILoadoutItemSlot(item);
            slot.Left.Set(x, 0f);
            slotsRow.Append(slot);

            x += 40f; // tighter spacing
        }
        x += 12;

        // First 3 accessories
        for (int i = 0; i < def.Accessories.Count && i < 3; i++)
        {
            var item = new Item();
            item.SetDefaults(def.Accessories[i]);

            var slot = new UILoadoutItemSlot(item);
            slot.Left.Set(x, 0f);
            slotsRow.Append(slot);

            x += 40f;
        }
    }

    public override void MouseOver(UIMouseEvent evt)
    {
        base.MouseOver(evt);
        BackgroundColor = new Color(73, 94, 171);
        BorderColor = new Color(89, 116, 213);
        preview.SetAnimated(true);
    }

    public override void MouseOut(UIMouseEvent evt)
    {
        base.MouseOut(evt);
        BackgroundColor = new Color(63, 82, 151) * 0.7f;
        BorderColor = new Color(89, 116, 213) * 0.7f;
        preview.SetAnimated(false);
    }

    public sealed class UILoadoutItemSlot : UIElement
    {
        private readonly Item item;

        private const float SlotSize = 26f;
        private float Size = 36f;
        private const float IconScale = 0.75f;

        public UILoadoutItemSlot(Item source)
        {
            item = source.Clone();
            Width.Set(Size, 0f);
            Height.Set(Size, 0f);
        }

        protected override void DrawSelf(SpriteBatch sb)
        {
            CalculatedStyle dim = GetDimensions();
            Vector2 center = dim.Center();

            // Draw slot background (scaled & centered)
            float bgScale = Size / TextureAssets.InventoryBack.Width();
            Vector2 bgPos =
                center - TextureAssets.InventoryBack.Size() * bgScale * 0.5f;

            sb.Draw(
                TextureAssets.InventoryBack.Value,
                bgPos,
                null,
                Color.White,
                0f,
                Vector2.Zero,
                bgScale,
                SpriteEffects.None,
                0f
            );

            // Draw item icon (centered)
            if (!item.IsAir)
            {
                ItemSlot.DrawItemIcon(
                    item,
                    context: 31,
                    spriteBatch: sb,
                    screenPositionForItemCenter: center,
                    scale: 0.8f,
                    sizeLimit: Size,
                    environmentColor: Color.White
                );
            }

            // Tooltip only
            if (IsMouseHovering && !item.IsAir)
            {
                Main.LocalPlayer.mouseInterface = true;
                Main.HoverItem = item.Clone();
                Main.hoverItemName = item.Name;
            }
        }
    }

}
