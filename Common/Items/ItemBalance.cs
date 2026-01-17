using PvPAdventure.Core.Config;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace PvPAdventure.Common.Items;

// - Applies config-driven per-item stat overrides.
// - Overrides pick power for Spectre Pickaxe and Shroomite Digging Claw.
public class ItemBalance : GlobalItem
{
    public override void SetDefaults(Item item)
    {
        var adventureConfig = ModContent.GetInstance<ServerConfig>();

        // Can't construct an ItemDefinition too early -- it'll call GetName and won't be graceful on failure.
        if (ItemID.Search.TryGetName(item.type, out var name) &&
            adventureConfig.ItemStatistics.TryGetValue(new ItemDefinition(name), out var statistics))
        {
            if (statistics.Damage != null)
                item.damage = statistics.Damage.Value;
            if (statistics.UseTime != null)
                item.useTime = statistics.UseTime.Value;
            if (statistics.UseAnimation != null)
                item.useAnimation = statistics.UseAnimation.Value;
            if (statistics.ShootSpeed != null)
                item.shootSpeed = statistics.ShootSpeed.Value;
            if (statistics.Crit != null)
                item.crit = statistics.Crit.Value;
            if (statistics.Mana != null)
                item.mana = statistics.Mana.Value;
            if (statistics.Scale != null)
                item.scale = statistics.Scale.Value;
            if (statistics.Knockback != null)
                item.knockBack = statistics.Knockback.Value;
            if (statistics.Value != null)
                item.value = statistics.Value.Value;
        }

        if (item.type == ItemID.SpectrePickaxe || item.type == ItemID.ShroomiteDiggingClaw)
            item.pick = 210;
    }
}
