using System.Linq;
using PvPAdventure.System;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure;

public class AdventureTile : GlobalTile
{
    public override bool CanKillTile(int i, int j, int type, ref bool blockDamaged)
    {
        return !ModContent.GetInstance<RegionManager>().GetRegionsContaining(new(i, j))
            .Any(region => !region.CanModifyTiles);
    }

    public override bool CanExplode(int i, int j, int type)
    {
        return !ModContent.GetInstance<RegionManager>().GetRegionsContaining(new(i, j))
            .Any(region => !region.CanModifyTiles);
    }

    public override bool CanPlace(int i, int j, int type)
    {
        return !ModContent.GetInstance<RegionManager>().GetRegionsContaining(new(i, j))
            .Any(region => !region.CanModifyTiles);
    }

    public override bool CanReplace(int i, int j, int type, int tileTypeBeingPlaced)
    {
        return !ModContent.GetInstance<RegionManager>().GetRegionsContaining(new(i, j))
            .Any(region => !region.CanModifyTiles);
    }
}
public class AncientManipulatorGlobalTile : GlobalTile
{
    public override void SetStaticDefaults()
    {
        // Make Ancient Manipulator count as all crafting stations
        TileID.Sets.CountsAsWaterSource[TileID.LunarCraftingStation] = true;
        TileID.Sets.CountsAsShimmerSource[TileID.LunarCraftingStation] = true;
        TileID.Sets.CountsAsHoneySource[TileID.LunarCraftingStation] = true;
        TileID.Sets.CountsAsLavaSource[TileID.LunarCraftingStation] = true;
        // Set it as all the basic crafting station types
        Main.tileTable[TileID.LunarCraftingStation] = true;
        Main.tileLighted[TileID.LunarCraftingStation] = true;
        // Make it provide adjacency for all crafting stations
        TileID.Sets.BasicChest[TileID.LunarCraftingStation] = false;
        TileID.Sets.BasicChestFake[TileID.LunarCraftingStation] = false;
    }

    public override int[] AdjTiles(int type)
    {
        if (type == TileID.LunarCraftingStation)
        {
            return new int[] {
                TileID.WorkBenches,
                TileID.Furnaces,
                TileID.Anvils,
                TileID.AdamantiteForge,
                TileID.MythrilAnvil,
                TileID.Chairs,
                TileID.Tables,
                TileID.Loom,
                TileID.Kegs,
                TileID.Bookcases,
                TileID.TinkerersWorkbench,
                TileID.ImbuingStation,
                TileID.DyeVat,
                TileID.HeavyWorkBench,
                TileID.GlassKiln,
                TileID.LivingLoom,
                TileID.SkyMill,
                TileID.Solidifier,
                TileID.FleshCloningVat,
                TileID.SteampunkBoiler,
                TileID.HoneyDispenser,
                TileID.IceMachine,
                TileID.Campfire,
                TileID.BewitchingTable,
                TileID.AlchemyTable,
                TileID.CrystalBall,
                TileID.Autohammer,
                TileID.BoneWelder,
                TileID.LesionStation,
                TileID.Sinks,
                TileID.Sawmill,
                TileID.CookingPots,
                TileID.DemonAltar,
                TileID.TeaKettle,
                TileID.Blendomatic,
                TileID.MeatGrinder,
                TileID.AlchemyTable,
                TileID.AlchemyTable,
                TileID.AlchemyTable,
                TileID.AlchemyTable,
                TileID.AlchemyTable,
                TileID.Bottles,
                TileID.AlchemyTable


            };

        }
        return base.AdjTiles(type);
    }
    public override void NearbyEffects(int i, int j, int type, bool closer)
    {
        if (type == TileID.LunarCraftingStation && closer)
        {
            Player player = Main.LocalPlayer;

            // Apply Alchemy Table effect (33% chance to not consume ingredients when crafting potions)
            player.alchemyTable = true;
        }
    }
}