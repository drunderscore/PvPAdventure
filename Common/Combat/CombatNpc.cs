using PvPAdventure.Core.Config;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Combat;

public class CombatNPC : GlobalNPC
{
    public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
    {
        if (!Main.dedServ) PlayHitMarker(damageDone);
    }

    public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
    {
        if (!Main.dedServ) PlayHitMarker(damageDone);
    }

    private static void PlayHitMarker(int damage)
    {
        var marker = ModContent.GetInstance<ClientConfig>().SoundEffect.NpcHitMarker;
        if (marker != null) SoundEngine.PlaySound(marker.Create(damage));
    }
}
