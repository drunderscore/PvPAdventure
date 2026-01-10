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
            Pass("ArenasMiniWorld", ArenasMiniWorld),
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

    private static void ArenasMiniWorld()
    {
        // size: 680x169

        var mod = ModContent.GetInstance<PvPAdventure>();
        const string path = "Core/Arenas/Structures/arenas_v2";
        Point16 pos = new(0, 0);

        if (!StructureHelper.API.Generator.IsInBounds(path, mod, pos))
        {
            Log.Error("Miniworld does not fit subworld. Aborting gen.");
            Log.Chat("Miniworld does not fit subworld. Aborting gen.");
            return;
        }

        StructureHelper.API.Generator.GenerateStructure(path, pos, mod);
    }

    private static void BottomMudLayer()
    {
        int bottom = Main.maxTilesY - 2;

        for (int x = 0; x < Main.maxTilesX; x++)
        {
            for (int y = bottom; y > bottom - 10; y--)
            {
                WorldGen.KillTile(x, y, noItem: true);
                WorldGen.PlaceTile(x, y, TileID.Mud, mute: true, forced: true);
            }
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
