using MonoMod.Cil;
using PvPAdventure.Core.Config;
using Terraria;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace PvPAdventure.Common.WorldGenChanges;

internal class ModifyChlorophyteGrowth : ModSystem
{
    public override void Load()
    {
        IL_WorldGen.hardUpdateWorld += OnWorldGenhardUpdateWorld;
        IL_WorldGen.Chlorophyte += OnWorldGenChlorophyte;
    }

    public override void Unload()
    {
        IL_WorldGen.hardUpdateWorld -= OnWorldGenhardUpdateWorld;
        IL_WorldGen.Chlorophyte -= OnWorldGenChlorophyte;
    }

    private void OnWorldGenhardUpdateWorld(ILContext il)
    {
        var cursor = new ILCursor(il);

        // Find the call to WorldGen.genRand.Next(300)...
        if (!cursor.TryGotoNext(i => i.MatchCallvirt<UnifiedRandom>("Next") && i.Previous.MatchLdcI4(300)))
        {
            Mod.Logger.Warn("Couldn't find WorldGen.genRand.Next(300) in hardUpdateWorld. Skipping IL patch (another mod likely changed the method).");
            return;
        }

        //  ...and go back to the constant load...
        cursor.Index -= 1;
        // ... to remove it...
        cursor.Remove()
            // ...and replace it with a delegate that loads from our config instance.
            .EmitDelegate(() =>
                AtLeastOne(ModContent.GetInstance<ServerConfig>().WorldGeneration.ChlorophyteGrowChanceModifier));

        // Find the call to WorldGen.genRand.Next(3)...
        if (!cursor.TryGotoNext(i => i.MatchCallvirt<UnifiedRandom>("Next") && i.Previous.MatchLdcI4(3)))
        {
            Mod.Logger.Warn("Couldn't find WorldGen.genRand.Next(3) after Next(300) in hardUpdateWorld. Skipping second IL patch.");
            return;
        }

        //  ...and go back to the constant load...
        cursor.Index -= 1;
        // ... to remove it...
        cursor.Remove()
            // ...and replace it with a delegate that loads from our config instance.
            .EmitDelegate(() =>
                AtLeastOne(ModContent.GetInstance<ServerConfig>().WorldGeneration.ChlorophyteSpreadChanceModifier));
    }

    private void OnWorldGenChlorophyte(ILContext il)
    {
        var cursor = new ILCursor(il);

        // Find the constant 40...
        if (cursor.TryGotoNext(i => i.MatchLdcI4(40)))
        {
            cursor.Remove()
                .EmitDelegate(() =>
                    ModContent.GetInstance<ServerConfig>().WorldGeneration.ChlorophyteGrowLimitModifier);
        }
        else
        {
            Log.Warn("Couldn't find ldc.i4 40 in WorldGen.Chlorophyte. Skipping that replacement.");
        }

        cursor.Index = 0;

        // Find the constant 130...
        if (cursor.TryGotoNext(i => i.MatchLdcI4(130)))
        {
            cursor.Remove()
                .EmitDelegate(() =>
                    ModContent.GetInstance<ServerConfig>().WorldGeneration.ChlorophyteGrowLimitModifier);
        }
        else
        {
            Mod.Logger.Warn("Couldn't find ldc.i4 130 in WorldGen.Chlorophyte. Skipping that replacement.");
        }
    }

    private static int AtLeastOne(int value)
    {
        if (value < 1)
        {
            return 1;
        }

        return value;
    }
}
