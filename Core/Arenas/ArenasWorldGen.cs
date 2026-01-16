using PvPAdventure.Core.Debug;
using StructureHelper;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Generation;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace PvPAdventure.Core.Arenas;

public class ArenasWorldGen : ModSystem
{
    public static List<GenPass> GenPasses()
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

        // Optional but strongly recommended on dedicated server to avoid huge SendTileSquare net payloads.
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

    #region Helpers
    private static GenPass Pass(string name, Action action, string message = null, float weight = 1f)
    {
        message ??= "Generating " + name;
        Log.Info("Arenas subworld is " + message);
        return new PassLegacy(name, (p, _) => { p.Message = message; action(); }, weight);
    }
   
    #endregion

}
