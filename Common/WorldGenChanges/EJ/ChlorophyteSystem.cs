using MonoMod.Cil;
using PvPAdventure.Core.Config;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.WorldGenChanges.EJ;
class ChlorophyteSystem : ModSystem
{
    public override void Load()
    {
        IL_WorldGen.Chlorophyte += OnWorldGenChlorophyte;
    }

    private void OnWorldGenChlorophyte(ILContext il)
    {
        var cursor = new ILCursor(il);

        // Find the call to WorldGen.genRand.Next(300)...
        cursor.GotoNext(i => i.MatchLdcI4(40));
        // ... to remove it...
        cursor.Remove()
            // ...and replace it with a delegate that loads from our config instance.
            .EmitDelegate(() =>
                ModContent.GetInstance<ServerConfig>().WorldGeneration.ChlorophyteGrowLimitModifier);
        cursor.Index = 0;

        cursor.GotoNext(i => i.MatchLdcI4(130));
        // ... to remove it...
        cursor.Remove()
            // ...and replace it with a delegate that loads from our config instance.
            .EmitDelegate(() =>
                ModContent.GetInstance<ServerConfig>().WorldGeneration.ChlorophyteGrowLimitModifier);
    }
}
