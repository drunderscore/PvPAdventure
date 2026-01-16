using System.Linq;
using PvPAdventure.Core.Config;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace PvPAdventure.Common.Combat;

public class CombatNpc : GlobalNPC
{
    public override bool? CanBeHitByProjectile(NPC npc, Projectile projectile)
    {
        var config = ModContent.GetInstance<AdventureServerConfig>();

        var isBoss = npc.boss
                     || IsPartOfEaterOfWorlds((short)npc.type)
                     || IsPartOfTheDestroyer((short)npc.type);

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
        var marker = ModContent.GetInstance<AdventureClientConfig>().SoundEffect.NpcHitMarker;
        if (marker != null)
            SoundEngine.PlaySound(marker.Create(damage));
    }

    public static bool IsPartOfEaterOfWorlds(short type) =>
        type is NPCID.EaterofWorldsHead or NPCID.EaterofWorldsBody or NPCID.EaterofWorldsTail;

    public static bool IsPartOfTheDestroyer(short type) =>
        type is NPCID.TheDestroyer or NPCID.TheDestroyerBody or NPCID.TheDestroyerTail;
}
