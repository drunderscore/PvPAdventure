using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Misc;
/// <summary>
/// Makes the three types of dungeon bricks unable to be blockswapped or mined without the requisite pickaxe power, even on the surface.
/// </summary>
internal class DungeonBrickTileChanges : GlobalTile
{
    private static Dictionary<int, int> TilePickaxeRequirements = new Dictionary<int, int>
    {
            { TileID.BlueDungeonBrick, 100 },
            { TileID.GreenDungeonBrick, 100 },
            { TileID.PinkDungeonBrick, 100 }
    };

    public override bool CanKillTile(int i, int j, int type, ref bool blockDamaged)
    {
        if (TilePickaxeRequirements.TryGetValue(type, out int requiredPick))
        {
            Player player = Main.LocalPlayer;
            if (player.HeldItem.pick < requiredPick)
            {
                return false;
            }
        }
        return base.CanKillTile(i, j, type, ref blockDamaged);
    }
    public override bool CanPlace(int i, int j, int type)
    {
        Tile existingTile = Main.tile[i, j];
        if (existingTile.HasTile)
        {
            int existingType = existingTile.TileType;
            if (TilePickaxeRequirements.TryGetValue(existingType, out int requiredPick))
            {
                Player player = Main.LocalPlayer;
                    return false;
            }
        }
        return base.CanPlace(i, j, type);
    }
}
