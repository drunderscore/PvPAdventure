using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Content.Buffs;
public class UncoutHandboring : ModBuff
{
    public override string Texture => $"PvPAdventure/Assets/Buff/uncouthandboring";

    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.buffNoSave[Type] = false;
        Main.buffNoTimeDisplay[Type] = false;
        Main.persistentBuff[Type] = true;
    }
}