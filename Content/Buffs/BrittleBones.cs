using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Content.Buffs;

public class BrittleBones : ModBuff
{
    public override string Texture => $"PvPAdventure/Assets/Buff/BrittleBones";

    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.buffNoSave[Type] = false;
        Main.buffNoTimeDisplay[Type] = false;
        Main.persistentBuff[Type] = false;
    }
}