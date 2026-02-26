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
        if (!SubworldSystem.AnyActive())
            return;

        var config = ModContent.GetInstance<ArenasConfig>();
        if (config == null) return;

        // Clamp hp to max
        if (Player.statLifeMax2 != config.MaxHealth)
        {
            Player.statLifeMax = config.MaxHealth;
            Player.statLifeMax2 = config.MaxHealth;
        }

        // Clamp mana to max
        if (Player.statManaMax2 != config.MaxMana)
        {
            Player.statManaMax2 = config.MaxMana;
            Player.statManaMax = config.MaxMana;
        }
    }
    public override void ModifyMaxStats(out StatModifier health, out StatModifier mana)
    {
        base.ModifyMaxStats(out health, out mana);

        if (!SubworldSystem.IsActive<ArenasSubworld>())
            return;

        var config = ModContent.GetInstance<ArenasConfig>();
        if (config == null)
            return;

        // Force base max stats to arena values.
        health.Base = config.MaxHealth - Player.statLifeMax;
        mana.Base = config.MaxMana - Player.statManaMax;
    }

    public override void OnHurt(Player.HurtInfo info)
    {
        if (!SubworldSystem.AnyActive())
            return;

        if (Player.whoAmI != Main.LocalPlayer.whoAmI)
        {
            return;
        }

        damageLockTicks = DamageLockDuration;
    }

    public override void PostUpdate()
    {
        if (!SubworldSystem.AnyActive())
            return;

        if (Player.whoAmI != Main.LocalPlayer.whoAmI)
        {
            return;
        }

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
