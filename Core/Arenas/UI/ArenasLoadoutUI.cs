using Microsoft.Xna.Framework.Graphics;
using SubworldLibrary;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.UI;
using static PvPAdventure.AdventureServerConfig;

namespace PvPAdventure.Core.Arenas.UI;

public class ArenasLoadoutUI : UIState
{
    // Title bar height
    private const float TitleHeight = 52f;

    // Root draggable element
    private DraggableElement Root;

    // Container panel
    private UIPanel Container;

    // Returns item type or ItemID.None if null
    private static int ItemOrAir(ItemDefinition item)
    {
        if (item == null || item.Type <= 0)
            return ItemID.None;

        return item.Type;
    }

    public override void OnActivate()
    {
        RemoveAllChildren();

        Root = new DraggableElement
        {
            Width = new StyleDimension(420f, 0f),
            Top = new StyleDimension(50f, 0f),
            Height = new StyleDimension(468f, 0f),
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

        // Add loadouts
        var cfg = ModContent.GetInstance<AdventureServerConfig>();
        foreach (var loadout in cfg.ArenaLoadouts)
        {
            if (loadout.Name == string.Empty && loadout.Head.Type <= 0)
                continue;

            list.Add(NewLoadout(loadout));
        }

        // Add exit button
        var exitButton = ArenasJoinUI.CreateButton("Exit Arena",SubworldSystem.Exit);
        exitButton.MarginTop = 12f;
        list.Add(exitButton);

        Container.Append(list);

        // Add title last
        Root.Append(title);
    }

    private static LoadoutListItem NewLoadout(Loadout loadout)
    {
        return new LoadoutListItem(
            BuildPreviewPlayer(Main.LocalPlayer, loadout),
            loadout,
            _ => EquipLoadout(loadout)
        );
    }

    private static Player BuildPreviewPlayer(Player source, Loadout def)
    {
        Player p = (Player)source.clientClone();

        // Armor
        p.armor[0].SetDefaults(ItemOrAir(def.Head));
        p.armor[1].SetDefaults(ItemOrAir(def.Body));
        p.armor[2].SetDefaults(ItemOrAir(def.Legs));

        // Accessories
        for (int i = 0; i < def.Accessories.Count && i < 5; i++)
        {
            p.armor[3 + i].SetDefaults(ItemOrAir(def.Accessories[i]));
        }

        // Hotbar
        for (int i = 0; i < def.Hotbar.Count && i < 10; i++)
        {
            var li = def.Hotbar[i];

            p.inventory[i].SetDefaults(ItemOrAir(li.Item));
            p.inventory[i].stack = li.Stack;
        }

        // Grappling hook
        p.miscEquips[4].SetDefaults(ItemOrAir(def.GrapplingHook));

        return p;
    }

    /// <summary>
    /// Called when a loadout is selected from the UI (pressing play button).
    /// </summary>
    private static void EquipLoadout(Loadout loadout)
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
        p.armor[0].SetDefaults(ItemOrAir(loadout.Head));
        p.armor[1].SetDefaults(ItemOrAir(loadout.Body));
        p.armor[2].SetDefaults(ItemOrAir(loadout.Legs));

        // clear accessories
        for (int i = 0; i < 5; i++)
            p.armor[3 + i].TurnToAir();

        // apply accessories
        for (int i = 0; i < loadout.Accessories.Count && i < 5; i++)
        {
            p.armor[3 + i].SetDefaults(ItemOrAir(loadout.Accessories[i]));
        }

        // clear inventory
        for (int i = 0; i < 10; i++)
            p.inventory[i].TurnToAir();

        // apply inventory
        for (int i = 0; i < loadout.Hotbar.Count && i < 10; i++)
        {
            var loadoutItem = loadout.Hotbar[i];
            p.inventory[i].SetDefaults(ItemOrAir(loadoutItem.Item));
            p.inventory[i].stack = loadoutItem.Stack;
        }

        // apply grappling hook
        p.miscEquips[4].SetDefaults(ItemOrAir(loadout.GrapplingHook));

        // hide UI
        ArenasUISystem.Toggle();

        // warning:
        // this restarts onenterworld and onload!
        //Main.ActivePlayerFileData.Player.Spawn(PlayerSpawnContext.SpawningIntoWorld);
        //Player.Hooks.EnterWorld(Main.myPlayer);
        
        // teleport to world spawn
        Vector2 worldSpawn = new(Main.spawnTileX*16, Main.spawnTileY*16);
        Main.LocalPlayer.Teleport(worldSpawn);
        
        // spawn dust
        SpawnRespawnDust(Main.LocalPlayer, loadout.Name);

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

    // A clickable loadout item.
    public sealed class LoadoutListItem : UIPanel
    {
        private readonly UICharacter preview;
        private readonly UIElement slotsRow;

        public LoadoutListItem(Player previewPlayer, Loadout loadout, Action<string> equip)
        {
            Height.Set(72f, 0f);
            Width.Set(0f, 1f);
            SetPadding(6f);

            BackgroundColor = new Color(63, 82, 151) * 0.7f;
            BorderColor = new Color(89, 116, 213) * 0.7f;

            preview = new UICharacter(previewPlayer, animated: false, hasBackPanel: true, 0.8f, useAClone: true);
            preview.Left.Set(4f, 0f);
            Append(preview);

            var nameText = new UIText(loadout.Name ?? "Unnamed", 1.0f);
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

            AddSlots(loadout);

            var playButton = new UIImageButton(Main.Assets.Request<Texture2D>("Images/UI/ButtonPlay"))
            {
                VAlign = 0.5f,
                Left = { Pixels = -20f, Precent = 1f }
            };
            playButton.OnLeftClick += (_, _) => equip?.Invoke(loadout.Name);
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
            if (ItemOrAir(def.Head) != ItemID.None)
            {
                var item = new Item();
                item.SetDefaults(ItemOrAir(def.Head));

                var slot = new UILoadoutItemSlot(item);
                slot.Left.Set(x, 0f);
                slotsRow.Append(slot);

                x += 40;
            }

            // First 2 Hotbar items
            for (int i = 0; i < def.Hotbar.Count && i < 2; i++)
            {
                var li = def.Hotbar[i];

                if (ItemOrAir(li.Item) == ItemID.GreaterHealingPotion)
                    continue;

                var item = new Item();
                item.SetDefaults(ItemOrAir(li.Item));
                item.stack = li.Stack;

                var slot = new UILoadoutItemSlot(item);
                slot.Left.Set(x, 0f);
                slotsRow.Append(slot);

                x += 40f;
            }
            //x += 12;

            // First 3 accessories
            for (int i = 0; i < def.Accessories.Count && i < 3; i++)
            {
                var item = new Item();
                item.SetDefaults(ItemOrAir(def.Accessories[i]));

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

        /// <summary>
        /// Shows the item's icon and hovering shows info.
        /// </summary>
        public sealed class UILoadoutItemSlot : UIElement
        {
            private readonly Item item;
            private const float Size = 36f;

            public UILoadoutItemSlot(Item source)
            {
                item = source.Clone();
                Width.Set(Size, 0f); Height.Set(Size, 0f);
            }

            protected override void DrawSelf(SpriteBatch sb)
            {
                var dim = GetDimensions();
                var center = dim.Center();
                float bgScale = Size / TextureAssets.InventoryBack.Width();

                sb.Draw(
                    TextureAssets.InventoryBack.Value,
                    center - TextureAssets.InventoryBack.Size() * bgScale * 0.5f,
                    null, Color.White, 0f, Vector2.Zero, bgScale, SpriteEffects.None, 0f
                );

                if (!item.IsAir)
                    ItemSlot.DrawItemIcon(item, 31, sb, center, 0.8f, Size, Color.White);

                if (IsMouseHovering && !item.IsAir)
                {
                    Main.LocalPlayer.mouseInterface = true;
                    Main.HoverItem = item.Clone();
                    Main.hoverItemName = item.Name;
                }

                if (item.stack > 1)
                {
                    var text = item.stack.ToString();
                    var size = FontAssets.ItemStack.Value.MeasureString(text);
                    var pos = dim.Position() + new Vector2(dim.Width - size.X / 4f - 24f, dim.Height - size.Y + 12f);
                    Utils.DrawBorderString(sb, text, pos, Color.White, 0.65f);
                }
            }
        }
    }
}

