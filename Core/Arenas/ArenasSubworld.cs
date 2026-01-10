using Microsoft.Xna.Framework;
using PvPAdventure.Core.Arenas.UI.JoinUI;
using PvPAdventure.Core.Arenas.UI.LoadoutUI;
using SubworldLibrary;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Utilities;
using Terraria.WorldBuilding;

namespace PvPAdventure.Core.Arenas;

public class ArenasSubworld : Subworld
{
    public override int Width => 825; // 680
    public override int Height => 275; // 169

    public override bool ShouldSave => false;
    public override bool NoPlayerSaving => true;

    public override List<GenPass> Tasks => ArenasWorldGen.GenPasses();

    // Sets the time to the middle of the day whenever the subworld loads
    public override void OnLoad()
    {
        Main.dayTime = true;
        Main.time = 27000;
        Main.NewText("Welcome to /arenas!", Color.MediumPurple);
        Main.NewText("Select a loadout to get started.", Color.MediumPurple);

        // become a ghost
        Main.LocalPlayer.ghost = true;

        ArenasJoinUISystem.Hide();
        ArenasLoadoutUISystem.Show();
    }
    public override bool GetLight(Tile tile, int x, int y, ref FastRandom rand, ref Vector3 color)
    {
        return base.GetLight(tile, x, y, ref rand, ref color);
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