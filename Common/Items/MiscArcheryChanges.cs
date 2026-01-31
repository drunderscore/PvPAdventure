using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Items;

public class MiscArcheryChanges : GlobalBuff
{
    public override void ModifyBuffText(int type, ref string buffName, ref string tip, ref int rare)
    {
        if (type == BuffID.Archery)
        {
            tip = "20% increased arrow speed\nNo longer grants bow damage";
        }
    }
}
public class ArcheryNerf : GlobalItem
{
    public override void ModifyWeaponDamage(Item item, Player player, ref StatModifier damage)
    {
        if (player.HasBuff(BuffID.Archery) && item.useAmmo == AmmoID.Arrow)
        {
            damage /= 1.1f;
        }
    }
}
