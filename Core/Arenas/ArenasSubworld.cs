using Microsoft.Xna.Framework;
using PvPAdventure.Core.Arenas.UI;
using PvPAdventure.Core.Debug;
using PvPAdventure.Core.Input;
using SubworldLibrary;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Generation;
using Terraria.ID;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.Utilities;
using Terraria.WorldBuilding;

namespace PvPAdventure.Core.Arenas;

public class ArenasSubworld : Subworld
{
    public override int Width => 780; // 680
    public override int Height => 269; // 169

    public override bool ShouldSave => false;
    public override bool NoPlayerSaving => true;

    public override List<GenPass> Tasks => GenPasses();

    #region World gen
    private List<GenPass> GenPasses()
    {
        return
        [
            Pass("AdjustWorldHeight", AdjustWorldHeight),
            Pass("Arenas", GenerateArenas),
        ];
    }

    private static void AdjustWorldHeight()
    {
        Main.worldSurface = Main.maxTilesY;
        Main.rockLayer = Main.maxTilesY;

        // adjust spawn pos
        Main.spawnTileX += 38;
        Main.spawnTileY += 45;
    }

    private static void GenerateArenas()
    {
        // size: ~680x169

        var mod = ModContent.GetInstance<PvPAdventure>();
        const string path = "Core/Arenas/Structures/arenas_v3";

        Point16 dims = StructureHelper.API.Generator.GetStructureDimensions(path, mod);

        const int margin = 20;

        // Center the structure
        int x = (Main.maxTilesX - dims.X) / 2;
        int y = (Main.maxTilesY - dims.Y) / 2;

        x = Utils.Clamp(x, margin, Main.maxTilesX - dims.X - margin);
        y = Utils.Clamp(y, margin, Main.maxTilesY - dims.Y - margin);

        Point16 pos = new(x, y);

        Log.Debug($"Miniworld dims: {dims.X}x{dims.Y}");
        Log.Debug($"World dims: {Main.maxTilesX}x{Main.maxTilesY}");
        Log.Debug($"Placing at: {pos.X},{pos.Y}");

        if (!StructureHelper.API.Generator.IsInBounds(path, mod, pos))
        {
            Log.Error("Miniworld does not fit subworld. Aborting gen.");
            Log.Chat("Miniworld does not fit subworld. Aborting gen.");
            return;
        }

        // Avoid huge SendTileSquare net payloads on dedicated servers
        int oldNetMode = Main.netMode;
        try
        {
            if (Main.netMode == NetmodeID.Server)
                Main.netMode = NetmodeID.SinglePlayer;

            StructureHelper.API.Generator.GenerateStructure(
                path,
                pos,
                mod
            );
        }
        finally
        {
            Main.netMode = oldNetMode;
        }
    }

    private static GenPass Pass(string name, Action action, string message = null, float weight = 1f)
    {
        message ??= "Generating " + name;
        Log.Info("Arenas subworld is " + message);
        Log.Chat("Arenas subworld is " + message);
        return new PassLegacy(name, (p, _) => { p.Message = message; action(); }, weight);
    }
    #endregion

    // Sets the time to the middle of the day whenever the subworld loads
    public override void OnLoad()
    {
        SendWelcomeMessage();

        RevealMap();

        // become a ghost
        //Main.LocalPlayer.ghost = true;

        //ArenasUISystem.Toggle();
    }

    private void SendWelcomeMessage()
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