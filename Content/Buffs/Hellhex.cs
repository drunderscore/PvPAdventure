using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Content.Buffs;

public class Hellhex : ModBuff
{
    public override string Texture => $"PvPAdventure/Assets/Buff/Hellhex";

    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.buffNoSave[Type] = false;
        Main.buffNoTimeDisplay[Type] = false;
        Main.persistentBuff[Type] = false;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        // Visual effects and buff management handled in ModPlayer
    }
}
