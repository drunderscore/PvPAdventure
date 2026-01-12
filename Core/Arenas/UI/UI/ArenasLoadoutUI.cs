using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.UI;

namespace PvPAdventure.Core.Arenas.UI.LoadoutUI;

public class ArenasLoadoutUI : UIState
{
    private const float TitleHeight = 52f;

    private DraggableElement Root;
    private UIPanel Container;

    public override void OnActivate()
    {
        RemoveAllChildren();

        Root = new DraggableElement
        {
            Width = new StyleDimension(420f, 0f),
            Top = new StyleDimension(50f, 0f),
            Height = new StyleDimension(510f, 0f),
            HAlign = 0.5f
        };
        Append(Root);

        var title = new UITextPanel<string>("Choose Your Loadout", 0.7f, large: true)
        {
            HAlign = 0.5f,
            Height = new StyleDimension(TitleHeight, 0f),
            BackgroundColor = new Color(73, 94, 171)
        };
        title.SetPadding(15f);
        title.OnLeftMouseDown += (evt, _) => Root.BeginDrag(evt);
        title.OnLeftMouseUp += (evt, _) => Root.EndDrag(evt);

        Container = new UIPanel
        {
            BackgroundColor = new Color(33, 43, 79) * 0.8f
        };
        Container.Top.Set(TitleHeight - 12f, 0f);
        Container.Width.Set(0f, 1f);
        Container.Height.Set(-TitleHeight, 1f);
        Root.Append(Container);

        var list = new UIList
        {
            PaddingTop = 8f
        };
        list.Width.Set(-24f, 1f);
        list.Height.Set(-24f, 1f);
        list.Left.Set(12f, 0f);
        list.Top.Set(12f, 0f);
        Container.Append(list);

        // Add loadouts
        AddLoadouts(list);

        // Add title last
        Root.Append(title);
    }

    private static void AddLoadouts(UIList list)
    {
        list.Add(NewLoadout(Loadouts.Melee));
        list.Add(NewLoadout(Loadouts.Ranger));
        list.Add(NewLoadout(Loadouts.Mage));
        list.Add(NewLoadout(Loadouts.Summoner));
    }

    private static LoadoutListItem NewLoadout(Loadout def)
    {
        return new LoadoutListItem(
            BuildPreviewPlayer(Main.LocalPlayer, def),
            def,
            _ => EquipLoadout(def)
        );
    }

    private static Player BuildPreviewPlayer(Player source, Loadout def)
    {
        Player p = (Player)source.clientClone();

        // armor
        p.armor[0].SetDefaults(def.Head);
        p.armor[1].SetDefaults(def.Body);
        p.armor[2].SetDefaults(def.Legs);

        // accessories
        for (int i = 0; i < def.Accessories.Count && i < 5; i++)
            p.armor[3 + i].SetDefaults(def.Accessories[i]);

        // inventory
        for (int i = 0; i < def.Hotbar.Count && i < 10; i++)
        {
            var li = def.Hotbar[i];
            p.inventory[i].SetDefaults(li.Type);
            p.inventory[i].stack = li.Stack;
        }

        // eqipment
        p.miscEquips[4].SetDefaults(def.GrapplingHook);

        return p;
    }

    /// <summary>
    /// Called when a loadout is selected from the UI (pressing play button).
    /// </summary>
    private static void EquipLoadout(Loadout def)
    {
        Player p = Main.LocalPlayer;

        var arenaPlayer = p.GetModPlayer<ArenasPlayer>();

        if (!arenaPlayer.CanSelectLoadout(out string reason))
        {
            Main.NewText($"Cannot select loadout: {reason}", Color.OrangeRed);
            return;
        }

        p.ghost = false;

        // apply armor
        p.armor[0].SetDefaults(def.Head);
        p.armor[1].SetDefaults(def.Body);
        p.armor[2].SetDefaults(def.Legs);

        // clear accessories
        for (int i = 0; i < 5; i++)
            p.armor[3 + i].TurnToAir();

        // apply accessories
        for (int i = 0; i < def.Accessories.Count && i < 5; i++)
            p.armor[3 + i].SetDefaults(def.Accessories[i]);

        // clear inventory
        for (int i = 0; i < 10; i++)
            p.inventory[i].TurnToAir();

        // apply inventory
        for (int i = 0; i < def.Hotbar.Count && i < 10; i++)
        {
            var item = def.Hotbar[i];
            p.inventory[i].SetDefaults(item.Type);
            p.inventory[i].stack = item.Stack;
        }

        p.miscEquips[4].SetDefaults(def.GrapplingHook);

        // hide UI
        ArenasLoadoutUISystem.Toggle();

        // warning:
        // this restarts onenterworld and onload!
        //Main.ActivePlayerFileData.Player.Spawn(PlayerSpawnContext.SpawningIntoWorld);
        //Player.Hooks.EnterWorld(Main.myPlayer);
        
        // teleport to world spawn
        Vector2 worldSpawn = new(Main.spawnTileX*16, Main.spawnTileY*16);
        Main.LocalPlayer.Teleport(worldSpawn);
        
        // spawn dust
        SpawnRespawnDust(Main.LocalPlayer, def.Name);

        // set health to 400
        p.statLife = 400;
        p.statLifeMax = 400;
        p.statLifeMax2 = 400;
    }

    private static void SpawnRespawnDust(Player p, string defName)
    {
        Color col = defName switch
        {
            "Melee" => new Color(220, 60, 60),   // deep red 
            "Ranger" => new Color(70, 160, 90),   // green
            "Mage" => new Color(90, 120, 220),  // blue
            "Summoner" => new Color(170, 110, 200), // purple 
            _ => Color.White
        };

        for (int i = 0; i < 80; i++)
        {
            int d = Dust.NewDust(
                p.position,
                p.width,
                p.height,
                DustID.FireworksRGB,
                Main.rand.NextFloat(-6f, 6f),
                Main.rand.NextFloat(-6f, 6f),
                150,
                col,
                1.0f
            );
            Dust dust = Main.dust[d];
            dust.color = col;
            dust.shader = GameShaders.Armor.GetShaderFromItemId(ItemID.None);
            dust.noGravity = true;
        }
    }
}

public sealed class LoadoutListItem : UIPanel
{
    private readonly UICharacter preview;
    private readonly UIText nameText;
    private readonly UIElement slotsRow;

    public LoadoutListItem(Player previewPlayer, Loadout def, Action<string> equip)
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
        playButton.OnMouseOver += (_, _) =>
        {
            playButton.OnUpdate += _ =>
            {
                if (!Main.LocalPlayer.mouseInterface)
                {
                    Main.LocalPlayer.mouseInterface = true;
                    if (IsMouseHovering)
                        Main.instance.MouseText("Play");
                }
            };
        };

        Append(playButton);
    }

    private void AddSlots(Loadout def)
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
        private float Size = 36f;

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

            // Draw stack
            if (item.stack > 1)
            {
                string stackText = item.stack.ToString();

                Vector2 textSize = FontAssets.ItemStack.Value.MeasureString(stackText);
                Vector2 textPos = dim.Position() + new Vector2(
                    dim.Width - textSize.X / 4f - 24f,
                    dim.Height - textSize.Y + 12f
                );

                Utils.DrawBorderString(sb,stackText,textPos,Color.White,0.65f);
            }
        }
    }

}
