using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Projectiles;

/// <summary>
/// Removes minions if the player does not have the corresponding summoning staff in their inventory.
/// </summary>
public sealed class RemoveSummonProjectilesInventory : GlobalProjectile
{
    private static readonly Dictionary<int, int> RequiredStaffByProjectile = new()
    {
        [ProjectileID.VenomSpider] = ItemID.SpiderStaff,
        [ProjectileID.JumperSpider] = ItemID.SpiderStaff,
        [ProjectileID.DangerousSpider] = ItemID.SpiderStaff,
        [ProjectileID.ClingerStaff] = ItemID.ClingerStaff,
        [ProjectileID.SpiderHiver] = ItemID.QueenSpiderStaff,
        [ProjectileID.RainCloudRaining] = ItemID.NimbusRod,
        [ProjectileID.UFOMinion] = ItemID.XenoStaff,
        [ProjectileID.Smolstar] = ItemID.Smolstar,
        [ProjectileID.Hornet] = ItemID.HornetStaff,
        [ProjectileID.FlyingImp] = ItemID.ImpStaff,
        [ProjectileID.Pygmy] = ItemID.PygmyStaff,
        [ProjectileID.Pygmy2] = ItemID.PygmyStaff,
        [ProjectileID.Pygmy3] = ItemID.PygmyStaff,
        [ProjectileID.Pygmy4] = ItemID.PygmyStaff,
        [ProjectileID.DeadlySphere] = ItemID.DeadlySphereStaff,
        [ProjectileID.OneEyedPirate] = ItemID.PirateStaff,
        [ProjectileID.SoulscourgePirate] = ItemID.PirateStaff,
        [ProjectileID.PirateCaptain] = ItemID.PirateStaff,
        [ProjectileID.Tempest] = ItemID.TempestStaff,
        [ProjectileID.EmpressBlade] = ItemID.EmpressBlade
    };

    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
    {
        return RequiredStaffByProjectile.ContainsKey(entity.type);
    }

    public override void PostAI(Projectile projectile)
    {
        Player owner = Main.player[projectile.owner];
        int requiredItem = RequiredStaffByProjectile[projectile.type];

        bool hasItem =
            owner.HasItem(requiredItem) ||
            (owner.inventory[58].type == requiredItem && owner.inventory[58].stack > 0);

        if (!hasItem)
        {
            projectile.Kill();
        }
    }
}
