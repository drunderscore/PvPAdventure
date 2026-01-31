using Microsoft.Xna.Framework;
using MonoMod.Cil;
using PvPAdventure.Core.Config;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Combat;

/// <summary>
/// - Hit marker sound when hurt by another player
/// </summary>
internal class HitMarkerSounds : ModPlayer
{
    public override void Load()
    {
        // Allow player hurt sound to be silenced or not, without regards to the networked value or mutating it.
        IL_Player.Hurt_HurtInfo_bool += EditPlayerHurt;
    }

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

    private void EditPlayerHurt(ILContext il)
    {
        var cursor = new ILCursor(il);

        // Find the load of Player.HurtInfo.SoundDisabled...
        cursor.GotoNext(i => i.MatchLdfld<Player.HurtInfo>("SoundDisabled"))
            // ...and remove it...
            .Remove()
            // ...emitting a load of argument 0 (this)...
            .EmitLdarg0()
            // ...and a delegate, whose return value will take the place of the above-removed load.
            .EmitDelegate((Player.HurtInfo hurtInfo, Player target) =>
                ShouldSilenceHurtSound(target, hurtInfo) ?? hurtInfo.SoundDisabled);
    }
    private static bool? ShouldSilenceHurtSound(Player target, Player.HurtInfo info)
    {
        // Only silence hurt sound on clients that we hurt that aren't ourselves
        if (!Main.dedServ && info.PvP && target.whoAmI != Main.myPlayer &&
            info.DamageSource.SourcePlayerIndex == Main.myPlayer)
        {
            var marker = ModContent.GetInstance<ClientConfig>().SoundEffect.PlayerHitMarker;
            if (marker != null && marker.SilenceVanilla)
                return true;
        }

        return null;
    }
}
