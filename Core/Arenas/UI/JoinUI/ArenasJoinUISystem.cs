using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Core.Arenas.UI.JoinUI;

[Autoload(Side = ModSide.Client)]
public class ArenasJoinUISystem : ModSystem
{
    public static UserInterface Interface;
    public static ArenasJoinUI State;

    public override void OnWorldLoad()
    {
        if (Main.dedServ)
            return;

        Interface = new UserInterface();
        State = new ArenasJoinUI();

        Show();
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
            "PvPAdventure: Arenas Join UI",
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

        Interface.SetState(State);
    }

    public static void Hide()
    {
        Interface?.SetState(null);
    }

    public static void Toggle()
    {
        if (Interface?.CurrentState == null)
            Show();
        else
            Hide();
    }
}