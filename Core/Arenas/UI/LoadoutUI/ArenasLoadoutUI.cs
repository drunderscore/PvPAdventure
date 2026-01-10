using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.IO;
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

    private static LoadoutListItem NewLoadout(LoadoutDef def)
    {
        return new LoadoutListItem(
            LoadoutApplier.BuildPlayer(Main.LocalPlayer, def),
            def,
            _ => EquipLoadout(def)
        );
    }

    private static void EquipLoadout(LoadoutDef def)
    {
        Player p = Main.LocalPlayer;
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

        // hide UI
        ArenasLoadoutUISystem.Hide();

        // spawn into world with dust
        Main.ActivePlayerFileData.Player.Spawn(PlayerSpawnContext.SpawningIntoWorld);
        Player.Hooks.EnterWorld(Main.myPlayer);
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
