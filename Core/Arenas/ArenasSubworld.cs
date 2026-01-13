using Microsoft.Xna.Framework;
using PvPAdventure.Core.Arenas.UI;
using PvPAdventure.System.Client;
using SubworldLibrary;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.Utilities;
using Terraria.WorldBuilding;

namespace PvPAdventure.Core.Arenas;

public class ArenasSubworld : Subworld
{
    public override int Width => 1000; // 680
    public override int Height => 400; // 169

    public override bool ShouldSave => false;
    public override bool NoPlayerSaving => true;

    public override List<GenPass> Tasks => ArenasWorldGen.GenPasses();

    // Sets the time to the middle of the day whenever the subworld loads
    public override void OnLoad()
    {
        if (Main.netMode != NetmodeID.Server)
        {
            var keybinds = ModContent.GetInstance<Keybinds>();

            string loadoutKeybind =
                keybinds.ArenasMenu.GetAssignedKeys().Count > 0
                    ? keybinds.ArenasMenu.GetAssignedKeys()[0]
                    : "Unbound";

            Main.dayTime = true;
            Main.time = 12000;

            Main.NewText("Welcome to arenas!", Color.MediumPurple);
            Main.NewText(
                $"Use [{loadoutKeybind}] to show loadouts to get started.",
                Color.MediumPurple
            );
        }

        RevealMap();

        // become a ghost
        //Main.LocalPlayer.ghost = true;

        //ArenasUISystem.Toggle();
    }

    public override void OnEnter()
    {
        //ArenaPlayerCountNet.Broadcast();
    }
    public override void OnExit()
    {
        //ArenaPlayerCountNet.Broadcast();
    }

    // Modify light here
    public override bool GetLight(Tile tile, int x, int y, ref FastRandom rand, ref Vector3 color)
    {
        return base.GetLight(tile, x, y, ref rand, ref color);
    }

    public static void RevealMap()
    {
        if (Main.LocalPlayer == null || Main.Map == null)
        {
            Log.Warn("No player exists, cant reveal map yet");
            return;
        }

        for (int i = 0; i < Main.maxTilesX; i++)
        {
            for (int j = 0; j < Main.maxTilesY; j++)
            {
                if (WorldGen.InWorld(i, j))
                    Main.Map.Update(i, j, 255);
            }
        }

        Main.refreshMap = true;
    }
}

public class UpdateSubworldSystem : ModSystem
{
    public override void PreUpdateWorld()
    {
        if (SubworldSystem.IsActive<ArenasSubworld>())
        {
            // Update mechanisms
            Wiring.UpdateMech();

            // Update tile entities
            TileEntity.UpdateStart();
            foreach (TileEntity te in TileEntity.ByID.Values)
            {
                te.Update();
            }
            TileEntity.UpdateEnd();

            // Update liquid
            if (++Liquid.skipCount > 1)
            {
                Liquid.UpdateLiquid();
                Liquid.skipCount = 0;
            }
        }
    }
}