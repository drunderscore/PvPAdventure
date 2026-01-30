//using System.Collections.Generic;
//using Terraria;
//using Terraria.DataStructures;
//using Terraria.ID;
//using Terraria.ModLoader;
//using Terraria.ObjectData;

//namespace PvPAdventure.Common.Teams;

//[Autoload(Side = ModSide.Client)]
//public class TeamColoredBeds : ModSystem
//{
//    private static readonly Dictionary<Terraria.Enums.Team, byte> _teamPaints = new()
//    {
//        [Terraria.Enums.Team.Red] = PaintID.RedPaint,
//        [Terraria.Enums.Team.Green] = PaintID.GreenPaint,
//        [Terraria.Enums.Team.Blue] = PaintID.BluePaint,
//        [Terraria.Enums.Team.Yellow] = PaintID.YellowPaint,
//        [Terraria.Enums.Team.Pink] = PaintID.PinkPaint,
//    };

//    public override void PostSetupContent()
//    {
//        // Automatically paint beds that are placed with a corresponding paint.
//        var placementHook = new PlacementHook(BedAfterPlacement, -1, 0, processedCoordinates: true);

//        // Bed with head on the right side
//        TileObjectData.GetTileData(TileID.Beds, 0).HookPostPlaceMyPlayer = placementHook;
//        // Bed with head on the left side
//        TileObjectData.GetTileData(TileID.Beds, 0, 1).HookPostPlaceMyPlayer = placementHook;

//        // Prevent people from painting beds.
//        On_Player.ApplyPaint += OnPlayerApplyPaint;
//        // Prevent people from scrapping the paint off of beds.
//        On_Player.PlaceThing_PaintScrapper_TryScrapping += OnPlayerPlaceThing_PaintScrapper_TryScrapping;
//    }

//    private void OnPlayerApplyPaint(On_Player.orig_ApplyPaint orig, Player self, int x, int y, bool paintingawall,
//        bool applyitemanimation, Item targetitem)
//    {
//        var tile = Main.tile[x, y];
//        if (paintingawall || tile.TileType != TileID.Beds)
//            orig(self, x, y, paintingawall, applyitemanimation, targetitem);
//    }

//    private void OnPlayerPlaceThing_PaintScrapper_TryScrapping(
//        On_Player.orig_PlaceThing_PaintScrapper_TryScrapping orig, Player self, int x, int y)
//    {
//        var tile = Main.tile[x, y];
//        if (tile.TileType != TileID.Beds)
//            orig(self, x, y);
//    }

//    private int BedAfterPlacement(int x, int y, int type, int style, int direction, int alternate)
//    {
//        if (!_teamPaints.TryGetValue((Terraria.Enums.Team)Main.LocalPlayer.team, out var paint))
//            return 0;

//        for (var xx = 0; xx < 4; xx++)
//        {
//            for (var yy = 0; yy < 2; yy++)
//                WorldGen.paintTile(x + xx, y + yy, paint, true);
//        }

//        return 0;
//    }
//}