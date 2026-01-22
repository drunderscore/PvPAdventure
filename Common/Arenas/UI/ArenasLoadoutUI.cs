using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.SpawnSelector;
using PvPAdventure.Core.Config;
using ReLogic.Content;
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

namespace PvPAdventure.Common.Arenas.UI;

/// <summary>
/// The UI and state for selecting loadouts in arenas.
/// Provides a list of loadouts from <see cref="ArenasConfig"/>
/// Shows a list of loadouts, containing previews of the loadout and a play button to equip it.
/// </summary>
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

        // Calculate height based on number of loadouts
        // FIXME: Not very accurate
        var config = ModContent.GetInstance<ArenasConfig>();
        int loadoutsCount = config.ArenaLoadouts.Count;
        int loadoutItemHeight = 72;
        int baseHeight = 400;
        int rootHeight = baseHeight + loadoutItemHeight * loadoutsCount;
        int width = 440;

        Root = new DraggableElement
        {
            Width = new StyleDimension(width, 0f),
            Top = new StyleDimension(50f, 0f),
            Height = new StyleDimension(rootHeight, 0f),
            HAlign = 0.5f
        };
        Append(Root);

        // Title
        var title = new UITextPanel<string>("Choose Your Loadout", 0.75f, large: true)
        {
            HAlign = 0.5f,
            BackgroundColor = new Color(73, 94, 171)
        };
        title.Width.Set(0f, 1f);
        title.SetPadding(15f);
        title.OnLeftMouseDown += (evt, _) => Root.BeginDrag(evt);
        title.OnLeftMouseUp += (evt, _) => Root.EndDrag(evt);

        // Append once to measure height
        Root.Append(title);
        Root.Recalculate();

        float panelHeight = title.GetOuterDimensions().Height;
        if (panelHeight <= 1f)
            panelHeight = TitleHeight;

        // Container
        Container = new UIPanel
        {
            BackgroundColor = new Color(33, 43, 79) * 0.8f
        };
        Container.Top.Set(panelHeight, 0f);
        Container.Width.Set(0f, 1f);
        Container.Height.Set(-panelHeight, 1f);
        Root.Append(Container);

        // Ensure title is always drawn above everything else
        title.Remove();
        Root.Append(title);

        var list = new UIList();
        list.Width.Set(0, 1f);
        list.Height.Set(0f, 1f);
        list.SetPadding(16);
        list.PaddingTop = -4;
        list.PaddingLeft = -4; // make room for scrollbar

        var scrollbar = new UIScrollbar();
        scrollbar.SetView(100f, 1000f);
        scrollbar.Width.Set(20f, 0f);
        scrollbar.Height.Set(0f, 1f);
        scrollbar.Top.Set(0f, 0f);
        scrollbar.Left.Set(-12f, 1f);

        list.SetScrollbar(scrollbar);

        Container.Append(list);
        Container.Append(scrollbar);

        // Add loadouts
        var cfg = ModContent.GetInstance<ArenasConfig>();
        foreach (var loadout in cfg.ArenaLoadouts)
        {
            if (loadout.Name == string.Empty && loadout.Armor.Head.Type <= 0)
                continue;

            list.Add(NewLoadout(loadout));
        }

        // Add exit button
        var exitButton = ArenasJoinUI.CreateButton("Exit Arena",SubworldSystem.Exit);
        exitButton.MarginTop = 8f;
        exitButton.SetPadding(8f);
        list.Add(exitButton);

        Container.Append(list);
    }

    private static LoadoutListItem NewLoadout(Loadout loadout)
    {
        return new LoadoutListItem(
            BuildPreviewPlayer(Main.LocalPlayer, loadout),
            loadout,
            _ => EquipLoadout(loadout)
        );
    }

    private static void ApplyLoadoutToPlayer(Player p, Loadout loadout)
    {
        // Armor
        p.armor[0].SetDefaults(ItemOrAir(loadout.Armor.Head));
        p.armor[1].SetDefaults(ItemOrAir(loadout.Armor.Body));
        p.armor[2].SetDefaults(ItemOrAir(loadout.Armor.Legs));

        // Accessories
        p.armor[3].SetDefaults(ItemOrAir(loadout.Accessories.Accessory1));
        p.armor[4].SetDefaults(ItemOrAir(loadout.Accessories.Accessory2));
        p.armor[5].SetDefaults(ItemOrAir(loadout.Accessories.Accessory3));
        p.armor[6].SetDefaults(ItemOrAir(loadout.Accessories.Accessory4));
        p.armor[7].SetDefaults(ItemOrAir(loadout.Accessories.Accessory5));

        // Clear inventory
        for (int i = 0; i < 50; i++)
            p.inventory[i].TurnToAir();

        // Inventory
        for (int i = 0; i < loadout.Inventory.Count && i < 50; i++)
        {
            var item = loadout.Inventory[i];
            p.inventory[i].SetDefaults(ItemOrAir(item.Item));
            p.inventory[i].stack = item.Stack;
        }

        // Grappling hook
        p.miscEquips[4].SetDefaults(ItemOrAir(loadout.GrapplingHook));

        // Mount
        p.mount.Dismount(p);
        if (loadout.Mount?.Type > 0)
            p.mount.SetMount(loadout.Mount.Type, p);
    }

    private static Player BuildPreviewPlayer(Player source, Loadout loadout)
    {
        Player p = (Player)source.clientClone();
        ApplyLoadoutToPlayer(p, loadout);
        return p;
    }

    /// <summary>
    /// Called when a loadout is selected from the UI (pressing play button).
    /// </summary>
    private static void EquipLoadout(Loadout loadout)
    {
        Player p = Main.LocalPlayer;

        // Check if player can select loadout
        var arenaPlayer = p.GetModPlayer<ArenasPlayer>();
        bool inSpawnRegion = p.GetModPlayer<SpawnPlayer>().IsPlayerInSpawnRegion();
        if (!inSpawnRegion && !arenaPlayer.CanSelectLoadout(out string reason))
        {
            Main.NewText($"Cannot select loadout: {reason}", Color.OrangeRed);
            return;
        }

        // Disable ghost mode
        p.ghost = false;

        // Apply loadout
        ApplyLoadoutToPlayer(p, loadout);

        // Hide UI
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

        // set health to max
        var config = ModContent.GetInstance<ArenasConfig>();
        p.statLife = config.MaxHealth;
        p.statLifeMax = config.MaxHealth;
        p.statLifeMax2 = config.MaxHealth;
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

    // A clickable loadout item with preview player and play button
    public sealed class LoadoutListItem : UIPanel
    {
        private readonly UICharacter preview;
        private readonly UIElement slotsRow;
        private UIImageButton playButton;

        private readonly Loadout loadout;

        public LoadoutListItem(Player previewPlayer, Loadout loadout, Action<string> equip)
        {
            this.loadout = loadout;

            //Height.Set(108f, 0f); // overriden later
            Width.Set(0f, 1f);
            SetPadding(6f);

            BackgroundColor = new Color(63, 82, 151) * 0.7f;
            BorderColor = new Color(89, 116, 213) * 0.7f;

            preview = new UICharacter(previewPlayer, animated: false, hasBackPanel: true, 0.8f, useAClone: true);
            preview.Left.Set(4f, 0f);
            Append(preview);

            var nameText = new UIText(loadout.Name ?? "Unnamed", 1.0f);
            nameText.Left.Set(72f, 0f);
            //nameText.VAlign = 0.01f;
            nameText.Top.Set(-2, 0);
            Append(nameText);

            slotsRow = new UIElement();
            slotsRow.Left.Set(72f, 0f);
            slotsRow.Width.Set(-96f, 1f);
            Append(slotsRow);

            AddSlots(loadout);

            var asset = Main.Assets.Request<Texture2D>("Images/UI/ButtonPlay", AssetRequestMode.ImmediateLoad);

            var playScale = 1.25f;
            playButton = new UIImageButton(asset);
            playButton.Width.Set(asset.Width() * playScale, 0f);
            playButton.Height.Set(asset.Height() * playScale, 0f);
            playButton.SetVisibility(0f, 0f);

            playButton.OnDraw += _ =>
            {
                var dim = playButton.GetDimensions();
                float alpha = playButton.IsMouseHovering ? 1f : 0.4f;

                Main.spriteBatch.Draw(asset.Value, dim.Position(), null, Color.White * alpha, 0f, Vector2.Zero, playScale, SpriteEffects.None, 0f);

                if (playButton._borderTexture != null && playButton.IsMouseHovering)
                    Main.spriteBatch.Draw(playButton._borderTexture.Value, dim.Position(), null, Color.White, 0f, Vector2.Zero, playScale, SpriteEffects.None, 0f);
            };

            playButton.OnLeftClick += (_, _) => equip?.Invoke(loadout.Name);
            Append(playButton);

            Recalculate(); // ensure preview/button have dimensions

            var pb = playButton.GetOuterDimensions();
            var pr = preview.GetOuterDimensions();

            // center under preview, and place just below it
            playButton.Left.Set(pr.X + (pr.Width - pb.Width) * 0.5f - GetInnerDimensions().X, 0f);
            playButton.Top.Set(pr.Y + pr.Height - GetInnerDimensions().Y+20, 0f);

            Recalculate();
        }

        private void AddSlots(Loadout def)
        {
            const float Step = 40f;
            const float Slot = 36f;
            const int Cols = 8;

            float x = 0f;
            float y = 16f;
            int col = 0;

            int count = 0;

            void Add(Item it, int? hotbarNumber = null)
            {
                var slot = new UILoadoutItemSlot(it, hotbarNumber);
                slot.Left.Set(x, 0f);
                slot.Top.Set(y, 0f);
                slotsRow.Append(slot);

                count++;

                col++;
                x += Step;

                if (col >= Cols)
                {
                    col = 0;
                    x = 0f;
                    y += Step;
                }
            }

            int t;

            t = ItemOrAir(def.Armor.Head);
            if (t != ItemID.None)
                Add(new Item(t));

            t = ItemOrAir(def.Armor.Body);
            if (t != ItemID.None)
                Add(new Item(t));

            t = ItemOrAir(def.Armor.Legs);
            if (t != ItemID.None)
                Add(new Item(t));

            t = ItemOrAir(def.Accessories.Accessory1);
            if (t != ItemID.None)
                Add(new Item(t));

            t = ItemOrAir(def.Accessories.Accessory2);
            if (t != ItemID.None)
                Add(new Item(t));

            t = ItemOrAir(def.Accessories.Accessory3);
            if (t != ItemID.None)
                Add(new Item(t));

            t = ItemOrAir(def.Accessories.Accessory4);
            if (t != ItemID.None)
                Add(new Item(t));

            t = ItemOrAir(def.Accessories.Accessory5);
            if (t != ItemID.None)
                Add(new Item(t));

            for (int i = 0; i < def.Inventory.Count && i < 50; i++)
            {
                var li = def.Inventory[i];
                t = ItemOrAir(li.Item);
                if (t == ItemID.None) continue;

                var it = new Item();
                it.SetDefaults(t);
                it.stack = li.Stack;

                // Only label hotbar slots (0-9)
                int? hotbarNumber = i < 10 ? (i == 9 ? 10 : i + 1) : null;
                Add(it, hotbarNumber);
            }

            t = ItemOrAir(def.GrapplingHook);
            if (t != ItemID.None) { var it = new Item(); it.SetDefaults(t); Add(it); }

            if (count == 0)
                return;

            int rows = (count + Cols - 1) / Cols;

            float slotsHeight = y + (rows - 1) * Step + Slot;
            slotsRow.Top.Set(0f, 0f);
            slotsRow.Height.Set(slotsHeight, 0f);

            Height.Set(144f, 0f);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (playButton?.IsMouseHovering == true)
            {
                Main.LocalPlayer.mouseInterface = true;
                Main.instance.MouseText("Play");
                return;
            }

            if (IsMouseHovering)
            {
                Main.LocalPlayer.mouseInterface = true;
                //Main.instance.MouseText(BuildTooltip(loadout));
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (SubworldSystem.AnyActive())
            {
                DrawPlayerFullBright.ForceFullBrightOnce = true;
            }
            base.DrawSelf(spriteBatch);
            //DrawPlayerFullBright.ForceFullBrightOnce = false;
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
            private readonly int? hotbarNumber;
            private const float Size = 36f;

            public UILoadoutItemSlot(Item source, int? hotbarNumber = null)
            {
                item = source.Clone();
                this.hotbarNumber = hotbarNumber;
                Width.Set(Size, 0f);
                Height.Set(Size, 0f);
            }

            protected override void DrawSelf(SpriteBatch sb)
            {
                var dim = GetDimensions();
                var center = dim.Center();
                float bgScale = Size / TextureAssets.InventoryBack.Width();

                sb.Draw(
                    TextureAssets.InventoryBack.Value,
                    center - TextureAssets.InventoryBack.Size() * bgScale * 0.5f,
                    null,
                    Color.White,
                    0f,
                    Vector2.Zero,
                    bgScale,
                    SpriteEffects.None,
                    0f
                );

                if (!item.IsAir)
                    ItemSlot.DrawItemIcon(item, 31, sb, center, 0.8f, Size, Color.White);

                if (hotbarNumber.HasValue)
                {
                    string text = hotbarNumber.Value == 10 ? "0" : hotbarNumber.Value.ToString();
                    Vector2 pos = dim.Position() + new Vector2(4f, 2f);
                    float scale = 0.7f;

                    Utils.DrawBorderStringFourWay(
                        sb,
                        FontAssets.ItemStack.Value,
                        text,
                        pos.X,
                        pos.Y,
                        Color.White,
                        new Color(20,20,20),
                        Vector2.Zero,
                        scale
                    );
                }

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
                    var pos = dim.Position() + new Vector2(dim.Width - size.X / 4f - 20f, dim.Height - size.Y + 10f);
                    Utils.DrawBorderString(sb, text, pos, Color.White, 0.55f);
                }
            }
        }

    }
}

