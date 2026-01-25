using PvPAdventure.Common.Combat.TeamBoss;
using PvPAdventure.Common.Npcs;
using PvPAdventure.Common.Statistics;
using PvPAdventure.Core.Config;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace PvPAdventure.Common.Combat;

public class CombatNPC : GlobalNPC
{
    public override bool? CanBeHitByProjectile(NPC npc, Projectile projectile)
    {
        var config = ModContent.GetInstance<ServerConfig>();

        var isBoss = npc.boss
                     || NpcHelpers.IsPartOfEaterOfWorlds((short)npc.type)
                     || NpcHelpers.IsPartOfTheDestroyer((short)npc.type);

        // Prevent projectiles from hitting bosses if configured, e.g dynamite (configurable from config)
        if (isBoss && config.BossInvulnerableProjectiles.Any(projectileDefinition =>
                projectileDefinition.Type == projectile.type))
            return false;

        return null;
    }

    // This only runs on the attacking player
    public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
    {
        if (!Main.dedServ)
            PlayHitMarker(damageDone);
    }

    // This only runs on the attacking player
    public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
    {
        if (!Main.dedServ)
            PlayHitMarker(damageDone);
    }

    private static void PlayHitMarker(int damage)
    {
        var marker = ModContent.GetInstance<ClientConfig>().SoundEffect.NpcHitMarker;
        if (marker != null)
            SoundEngine.PlaySound(marker.Create(damage));
    }
}
