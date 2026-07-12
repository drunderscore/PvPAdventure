using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Content.Buffs;

public class GodsProtection : ModBuff
{
    public override string Texture => $"PvPAdventure/Assets/Buff/GodsProtection";
    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.buffNoSave[Type] = false;
        Main.buffNoTimeDisplay[Type] = true;
        Main.persistentBuff[Type] = false;
    }
}

public class GodsProtectionPlayer : ModPlayer
{
    public override void ModifyHurt(ref Player.HurtModifiers modifiers)
    {
        if (Player.HasBuff<GodsProtection>())
        {
            modifiers.FinalDamage *= 0.4f;
        }
    }
}