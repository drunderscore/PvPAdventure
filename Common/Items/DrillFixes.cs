using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Items;

/// <summary>
/// Fixes two issues with drills
/// First, fixes the issue where drills mine 1 tick slower than they are supposed to
/// Second, fixes the issue where drills mine much faster when spam clicking. Instead, they mine at this speed constantly.
/// </summary>
public class DrillRework : GlobalItem
{
    public static bool IsDrill(Item item) =>
        item.pick > 0 &&
        item.shoot > ProjectileID.None &&
        item.useStyle == ItemUseStyleID.Shoot;

    public override void SetDefaults(Item item)
    {
        if (!IsDrill(item))
            return;
        item.useTime = Math.Max(2, item.useTime / 2); // Might have to decrease this icl
    }
}

public class DrillReworkPlayer : ModPlayer
{
    private bool wasChannelingDrill;
    private int lastToolTime = -1;
    private int virtualToolTime = -1; 

    public override bool PreItemCheck()
    {
        Item item = Player.HeldItem;
        if (!DrillRework.IsDrill(item))
            return true;

        if (!wasChannelingDrill && virtualToolTime >= 0
            && Player.controlUseItem && Player.itemAnimation == 0)
        {
            Player.itemAnimation = 2;
        }

        return true;
    }

    public override void PostUpdate()
    {
        Item item = Player.HeldItem;
        bool isDrill = DrillRework.IsDrill(item);
        bool isChanneling = isDrill && Player.channel;

        if (!isChanneling)
        {
            if (wasChannelingDrill)
            {
                virtualToolTime = lastToolTime - 1;
                if (virtualToolTime < 0)
                    virtualToolTime = item.useTime - 1;
            }
            else if (isDrill && virtualToolTime >= 0)
            {
                virtualToolTime--;
                if (virtualToolTime < 0)
                    virtualToolTime = item.useTime - 1;
            }
            else if (!isDrill)
            {
                virtualToolTime = -1;
            }

            wasChannelingDrill = false;
            lastToolTime = -1;
            return;
        }

        if (!wasChannelingDrill)
        {
            if (virtualToolTime >= 0)
            {
                virtualToolTime--;
                if (virtualToolTime < 0)
                    virtualToolTime = item.useTime - 1;
                if (virtualToolTime == 0)
                    virtualToolTime = 1;

                Player.toolTime = virtualToolTime;
                Player.itemAnimation = 2; //

                if (Player.whoAmI == Main.myPlayer)
                    SpawnDrillProjectile(item);
            }
            else
            {
                Player.toolTime = 2;
            }

            virtualToolTime = -1;
        }
        else if (lastToolTime == 0 && Player.toolTime == item.useTime)
        {
            Player.toolTime = item.useTime - 1;
        }

        wasChannelingDrill = true;
        lastToolTime = Player.toolTime;
    }

    private void SpawnDrillProjectile(Item item)
    {
        Vector2 center = Player.Center;
        Vector2 velocity = Main.MouseWorld - center;
        if (velocity.LengthSquared() > 0f)
            velocity.Normalize();
        velocity *= item.shootSpeed;

        Projectile.NewProjectile(
            Player.GetSource_ItemUse(item),
            center, velocity,
            item.shoot, 0, 0f,
            Player.whoAmI);
    }
}