using System.Linq;
using PvPAdventure.System;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Reflection;
using System;
using Mono.Cecil.Cil;

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
public class ForTheWorthyMiningSpeed : ModSystem
{
    private static ILHook getPickaxeDamageHook;

    public override void PostSetupContent()
    {
        MethodInfo method = typeof(Terraria.Player).GetMethod("GetPickaxeDamage",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (method == null)
        {
            ModContent.GetInstance<ForTheWorthyMiningSpeed>().Mod.Logger.Error("Could not find GetPickaxeDamage method!");
            return;
        }

        getPickaxeDamageHook = new ILHook(method, ModifyPickaxeDamage);
    }

    public override void Unload()
    {
        getPickaxeDamageHook?.Dispose();
    }

    private static void ModifyPickaxeDamage(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        try
        {
            if (cursor.TryGotoNext(MoveType.Before,
                i => i.MatchRet()
            ))
            {
                ModContent.GetInstance<ForTheWorthyMiningSpeed>().Mod.Logger.Info("Found return in GetPickaxeDamage");

                cursor.Emit(OpCodes.Ldc_R4, 1.5f); //for the worthy mining speed
                cursor.Emit(OpCodes.Mul);
                cursor.Emit(OpCodes.Conv_I4);

                ModContent.GetInstance<ForTheWorthyMiningSpeed>().Mod.Logger.Info("Successfully modified GetPickaxeDamage to multiply damage by 1.5");
            }
            else
            {
                ModContent.GetInstance<ForTheWorthyMiningSpeed>().Mod.Logger.Error("Could not find return statement in GetPickaxeDamage");
            }
        }
        catch (Exception e)
        {
            ModContent.GetInstance<ForTheWorthyMiningSpeed>().Mod.Logger.Error($"Error in IL edit: {e}");
        }
    }
}