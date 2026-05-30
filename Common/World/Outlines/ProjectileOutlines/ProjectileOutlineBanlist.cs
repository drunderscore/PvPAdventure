using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;

namespace PvPAdventure.Common.World.Outlines.ProjectileOutlines;

internal static class ProjectileOutlineBanlist
{
    private static readonly HashSet<int> Banned =
    [
        ProjectileID.Excalibur,
        ProjectileID.NightsEdge,
        ProjectileID.TrueExcalibur,
        ProjectileID.TrueNightsEdge,
        ProjectileID.TerraBeam,        // swing aura (the beam it fires is a separate ID)
        ProjectileID.TitaniumStormShard, // titanium shards
        ProjectileID.FirstFractal,       // First Fractal swing aura
        ProjectileID.SkyFracture,        // Sky Fracture shards
        ProjectileID.LightsBane, // way too large and weird
    ];

    public static bool IsBanned(Projectile projectile)
    {
        return projectile == null
            || Banned.Contains(projectile.type)
            || IsWhip(projectile);
    }

    private static bool IsWhip(Projectile projectile)
    {
        if (projectile.type <= ProjectileID.None)
            return false;

        if (projectile.type < ProjectileID.Sets.IsAWhip.Length && ProjectileID.Sets.IsAWhip[projectile.type])
            return true;

        return projectile.aiStyle == ProjAIStyleID.Whip;
    }
}
