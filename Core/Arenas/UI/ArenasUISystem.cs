using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PvPAdventure.System;
using PvPAdventure.System.Client;
using SubworldLibrary;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Core.Arenas.UI;

[Autoload(Side = ModSide.Client)]
public sealed class ArenasUISystem : ModSystem
{
    // UI
    public static UserInterface Interface;
    public static ArenasLoadoutUI LoadoutUIState;
    public static ArenasJoinUI JoinUIState;

    // Enabled check
    public static bool IsEnabled => ModContent.GetInstance<AdventureServerConfig>()?.IsArenasEnabled ?? false;

    public override void OnWorldLoad()
    {
        if (Main.dedServ)
            return;

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
        // Toggle loadout UI if in arena subworld.
        if (SubworldSystem.AnyActive())
        {
            if (Interface?.CurrentState == null)
                Interface?.SetState(LoadoutUIState);
            else
                Interface?.SetState(null);
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
        => Interface?.Update(gameTime);

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
