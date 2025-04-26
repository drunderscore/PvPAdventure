using MonoMod.Cil;
using Terraria;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace PvPAdventure.System;

public class WorldGenerationManager : ModSystem
{
    public override void Load()
    {
        IL_WorldGen.UpdateWorld_GrassGrowth += EditWorldGenUpdateWorld_GrassGrowth;
        IL_WorldGen.hardUpdateWorld += OnWorldGenhardUpdateWorld;
        IL_WorldGen.AddBuriedChest_int_int_int_bool_int_bool_ushort += EditWorldGenAddBuriedChest;
    }

    private void EditWorldGenUpdateWorld_GrassGrowth(ILContext il)
    {
        var cursor = new ILCursor(il);

        // Find the first reference to NPC.downedMechBoss3...
        cursor.GotoNext(i => i.MatchLdsfld<NPC>("downedMechBoss3"));

        // ...and go to the constant load of the Plantera Bulb denominator
        cursor.Index += 3;

        // ...and replace it with a delegate that loads from our config instance.
        cursor.Remove().EmitDelegate(() =>
            ModContent.GetInstance<AdventureConfig>().WorldGeneration.PlanteraBulbChanceDenominator);

        // Find the first reference to NPC.downedMechBossAny...
        cursor.GotoNext(i => i.MatchLdsfld<NPC>("downedMechBossAny"));

        // ...and go back to the constant load of non-expert mode Life Fruit denominator
        cursor.Index -= 6;

        // ...to remove it...
        cursor.Remove()
            // ...and replace it with a delegate that loads from our config instance.
            .EmitDelegate(() => ModContent.GetInstance<AdventureConfig>().WorldGeneration.LifeFruitChanceDenominator);

        // Then, advance past else branch, to the constant load of the expert mode Life Fruit denominator...
        cursor.Index += 1;

        // ...while ensuring that instructions removed and emitted are labeled correctly...
        cursor.MoveAfterLabels();
        // ...to remove it...
        cursor.Remove()
            // ...and replace it with a delegate that loads from our config instance.
            .EmitDelegate(() =>
                ModContent.GetInstance<AdventureConfig>().WorldGeneration.LifeFruitExpertChanceDenominator);
        // Return to default labeling behavior.
        cursor.MoveBeforeLabels();

        // Then, go forward to the constant load of the minimum distance between Life Fruit.
        cursor.Index += 11;

        // ...to remove it...
        cursor.Remove()
            // ...and replace it with a delegate that loads from our config instance.
            .EmitDelegate(() =>
                ModContent.GetInstance<AdventureConfig>().WorldGeneration.LifeFruitMinimumDistanceBetween);
    }

    private void OnWorldGenhardUpdateWorld(ILContext il)
    {
        var cursor = new ILCursor(il);

        // Find the call to WorldGen.genRand.Next(3)...
        cursor.GotoNext(i => i.MatchCallvirt<UnifiedRandom>("Next") && i.Previous.MatchLdcI4(3));
        //  ...and go back to the constant load...
        cursor.Index -= 1;
        // ... to remove it...
        cursor.Remove()
            // ...and replace it with a delegate that loads from our config instance.
            .EmitDelegate(() =>
                ModContent.GetInstance<AdventureConfig>().WorldGeneration.ChlorophyteSpreadChanceModifier);
    }

    private void EditWorldGenAddBuriedChest(ILContext il)
    {
        var cursor = new ILCursor(il);

        // Find the first reference to Main.chest...
        cursor.GotoNext(i => i.MatchLdsfld<Main>("chest"));

        // ...then advance to the initial loop condition check branch...
        cursor.Index += 6;

        // ...following the branch to the loop entry point...
        cursor.GotoLabel((ILLabel)cursor.Next!.Operand);

        // ...and go past the two instructions checking the loop condition...
        cursor.Index += 2;

        // ...to load the chest index...
        cursor.EmitLdloc(17);
        // ...and emit our own delegate to invoke.
        cursor.EmitDelegate((int chestId) =>
        {
            var adventureConfig = ModContent.GetInstance<AdventureConfig>();
            var chest = Main.chest[chestId];

            foreach (var item in chest.item)
            {
                if (adventureConfig.ChestItemReplacements.TryGetValue(new(item.type), out var replacement))
                {
                    var configItem = Utils.SelectRandom(WorldGen.genRand, replacement.Items.ToArray());
                    item.SetDefaults(configItem.Item.Type);
                    item.stack = configItem.Stack;
                    item.prefix = configItem.Prefix.Type;
                }
            }
        });
    }
}