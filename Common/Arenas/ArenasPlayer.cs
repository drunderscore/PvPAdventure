using PvPAdventure.Common.Arenas.UI;
using PvPAdventure.Core.Config;
using SubworldLibrary;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Arenas;

internal class ArenasPlayer : ModPlayer
{
    private const int DamageLockDuration = 120; // 2 seconds

    private int damageLockTicks;

    public bool DamageLocked => damageLockTicks > 0;
    public bool IsMoving { get; private set; }

    public override void ResetEffects()
    {
        if (SubworldSystem.AnyActive())
        {
            var config = ModContent.GetInstance<ArenasConfig>();
            if (config == null) return;

            // Ensure Clamp life to max while in arenas
            Player.statLifeMax2 = config.MaxHealth;
            if (Player.statLife > config.MaxHealth)
                Player.statLife = config.MaxHealth;

            // Ensure Clamp mana to max while in arenas
            Player.statManaMax2 = config.MaxMana;
            if (Player.statMana > config.MaxMana)
                Player.statMana = config.MaxMana;
        }
    }

    public override void OnHurt(Player.HurtInfo info)
    {
        if (Player.whoAmI != Main.LocalPlayer.whoAmI)
        {
            return;
        }

        if (!SubworldSystem.AnyActive())
            return;

        damageLockTicks = DamageLockDuration;
    }

    public override void PostUpdate()
    {
        if (Player.whoAmI != Main.LocalPlayer.whoAmI)
        {
            return;
        }

        if (!SubworldSystem.AnyActive())
            return;

        if (damageLockTicks > 0)
            damageLockTicks--;

        IsMoving =
            Player.velocity.LengthSquared() > 0.01f ||
            Player.controlLeft ||
            Player.controlRight ||
            Player.controlUp ||
            Player.controlDown;
    }

    public bool CanSelectLoadout(out string reason)
    {
        //if (Player.whoAmI != Main.LocalPlayer.whoAmI)
        //{
        //    reason = "null";
        //    return false;
        //}

        if (Player.dead)
        {
            int respawnTime = Player.respawnTimer;
            reason = "dead";
            //. respawn timer: " + respawnTime;
            return false;
        }

        if (DamageLocked)
        {
            reason = "recently damaged";
            //. damage lock duration: " + damageLockTicks;
            return false;
        }

        if (IsMoving)
        {
            int speed = (int)Player.velocity.LengthSquared();
            reason = "must stand still";
            //. your speed is: " + speed;
            return false;
        }

        reason = null;
        return true;
    }
}
