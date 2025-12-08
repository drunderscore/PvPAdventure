using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Core.SubworldArenas;

public class SWJoinUI : UIState
{
    private readonly UITextPanel<string>[] buttons = new UITextPanel<string>[4];

    public override void OnInitialize()
    {
        var list = new UIList();
        list.Width.Set(0f, 0.4f);
        list.Height.Set(0f, 0.3f);
        list.Top.Set(200f, 0f);
        list.Left.Set(0.3f, 0f);
        Append(list);

        for (int i = 0; i < 4; i++)
        {
            int index = i;

            var button = new UITextPanel<string>("", 0.9f, true);
            button.Width.Set(0f, 1f);
            button.Height.Set(32f, 0f);

            button.PaddingTop = 4;
            button.PaddingBottom = 4;
            button.SetPadding(6);

            button.OnLeftClick += (_, _) => SWSystem.RequestJoin(index);
            buttons[i] = button;
            list.Add(button);
        }

        RefreshTexts();
    }

    public void RefreshTexts()
    {
        for (int i = 0; i < 4; i++)
        {
            int count = SWSystem.Counts[i];
            buttons[i].SetText($"Enter SW{i + 1} ({count} players)", 0.9f, true);
        }
    }
}

[Autoload(Side=ModSide.Client)]
public class SubworldUISystem : ModSystem
{
    public static UserInterface Interface;
    public static SWJoinUI State;

    public override void Load()
    {
        if (Main.dedServ)
            return;

        Interface = new UserInterface();
        State = new SWJoinUI();
        Interface.SetState(State);
    }

    public override void Unload()
    {
        Interface = null;
        State = null;
    }

    public override void UpdateUI(GameTime gameTime)
    {
        Interface?.Update(gameTime);
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        if (Interface?.CurrentState == null)
        {
            return;
        }

        int index = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
        if (index == -1)
            return;

        layers.Insert(index, new LegacyGameInterfaceLayer(
            "PvPAdventure: Subworld Join",
            () =>
            {
                // Debug
                //Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.Red * 0.5f);

                Interface.Draw(Main.spriteBatch, new GameTime());
                return true;
            },
            InterfaceScaleType.UI));
    }

    public static void Show()
    {
        if (Interface == null || State == null)
            return;

        State.RefreshTexts();
        Interface.SetState(State);
    }

    public static void Hide()
    {
        Interface?.SetState(null);
    }

    public static void OnCountsUpdated()
    {
        State?.RefreshTexts();
    }
}

public class SubworldPlayer : ModPlayer
{
    public override void OnEnterWorld()
    {
        if (!Main.dedServ && Main.netMode == NetmodeID.MultiplayerClient)
        {
            SubworldUISystem.Show();
        }
    }
}
