using Microsoft.Xna.Framework;
using PvPAdventure.Core.Config;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Combat.PvP;

/// <summary>
/// PvP combat effects applied when players are hurt or killed.
/// Includes:
/// - Volcano projectile spawn when hit by Fiery Greatsword
/// - Muramasa projectile spawn when hit by Muramasa
/// - Hit marker sound when hurt by another player
/// - PvP-specific immunity frames handling
/// </summary>
internal class HitMarkerSounds : ModPlayer
{
    public override void OnHurt(Player.HurtInfo info)
    {
        if (!info.PvP)
            return;

        // Only play hit markers on clients that we hurt that aren't ourselves
        if (!Main.dedServ && Player.whoAmI != Main.myPlayer && info.DamageSource.SourcePlayerIndex == Main.myPlayer)
            PlayHitMarker(info.Damage);
    }

    public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
    {
        // Only play kill markers on clients that we hurt that aren't ourselves
        if (!Main.dedServ && pvp && damageSource.SourcePlayerIndex == Main.myPlayer && Player.whoAmI != Main.myPlayer)
            PlayKillMarker((int)damage);

    }
    private static void PlayHitMarker(int damage)
    {
        var marker = ModContent.GetInstance<ClientConfig>().SoundEffect.PlayerHitMarker;
        if (marker != null)
            SoundEngine.PlaySound(marker.Create(damage));
    }
    private static void PlayKillMarker(int damage)
    {
        var marker = ModContent.GetInstance<ClientConfig>().SoundEffect.PlayerKillMarker;
        if (marker != null)
            SoundEngine.PlaySound(marker.Create(damage));
    }
}
