using DragonLens.Content.Tools.Gameplay;
using Microsoft.Xna.Framework;
using PvPAdventure.Common.GameTimer;
using PvPAdventure.Common.SpawnSelector;
using PvPAdventure.Core.Config;
using PvPAdventure.Core.Debug;
using SubworldLibrary;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.Arenas.UI;

[Autoload(Side = ModSide.Client)]
public sealed class ArenasUISystem : ModSystem
{
    // UI
    public static UserInterface Interface;
    public static ArenasLoadoutUI LoadoutUIState;
    public static ArenasJoinUI JoinUIState;

    // Enabled check
    public static bool IsEnabled
    {
        get
        {
            var config = ModContent.GetInstance<ArenasConfig>();
            if (config == null)
            {
                Log.Warn("ServerConfig not loaded – Arenas disabled by default");
                return false;
            }

            return config.IsArenasEnabled;
        }
    }

    public override void OnWorldLoad()
    {
        Interface = new();
        LoadoutUIState = new();
        JoinUIState = new();

        if (IsEnabled)
        {
            Toggle();
        }
    }

    public static void Toggle()
    {
        var gm = ModContent.GetInstance<GameManager>();
        if (gm.CurrentPhase == GameManager.Phase.Playing)
        {
            Main.NewText("Arenas is unavailable when game is playing!", Color.Orange);
            ArenasUISystem.Close();
            return;
        }

        // Toggle loadout UI if in arena subworld.
        if (SubworldSystem.AnyActive())
        {
            if (Interface?.CurrentState == null)
            {
                Interface?.SetState(LoadoutUIState);
            }
            else
            {
                Interface?.SetState(null);
            }
        }

        else
        {
            // Otherwise toggle join UI.
            if (Interface?.CurrentState == null)
                Interface?.SetState(JoinUIState);
            else
                Interface?.SetState(null);
        }
    }

    public static void Close()
    {

        if (Interface?.CurrentState != null)
            Interface?.SetState(null);
    }

    public override void UpdateUI(GameTime gameTime)
    {
        //var ss = ModContent.GetInstance<SpawnSelector.SpawnSystem>();
        //if (ss.ui.CurrentState != null)
        //{
        //    if (Interface.CurrentState != null)
        //    {
        //        Interface.SetState(null);
        //    }
        //}

        Interface?.Update(gameTime);
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        if (Interface?.CurrentState == null)
            return;

        int index = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
        if (index == -1)
            return;

        layers.Insert(index, new LegacyGameInterfaceLayer(
            "PvPAdventure: Arenas UI",
            () =>
            {
                Interface.Draw(Main.spriteBatch, new GameTime());
                return true;
            },
            InterfaceScaleType.UI));
    }
}
