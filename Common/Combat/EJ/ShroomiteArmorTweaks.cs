using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Combat;

// this is kind of scuffed but its all temp anyways
public class ShroomiteArmorTweaks : ModPlayer
{
    private float _savedStealth;

    public override void OnHurt(Player.HurtInfo info)
    {
        bool hasFullShroomite =
            (Player.head == ArmorIDs.Head.ShroomiteHeadgear ||
             Player.head == ArmorIDs.Head.ShroomiteMask ||
             Player.head == ArmorIDs.Head.ShroomiteHelmet) &&
            Player.body == ArmorIDs.Body.ShroomiteBreastplate &&
            Player.legs == ArmorIDs.Legs.ShroomiteLeggings;

        if (hasFullShroomite)
            _savedStealth = Player.stealth;
    }

    public override void PostHurt(Player.HurtInfo info)
    {
        bool hasFullShroomite =
            (Player.head == ArmorIDs.Head.ShroomiteHeadgear ||
             Player.head == ArmorIDs.Head.ShroomiteMask ||
             Player.head == ArmorIDs.Head.ShroomiteHelmet) &&
            Player.body == ArmorIDs.Body.ShroomiteBreastplate &&
            Player.legs == ArmorIDs.Legs.ShroomiteLeggings;

        if (hasFullShroomite)
            Player.stealth = _savedStealth;
    }
}
public class ShroomiteAmmoBoost : GlobalItem
{
    public override bool InstancePerEntity => true;
    private int _originalDamage = -1; 

    public override void UpdateInventory(Item item, Player player)
    {
        if (item.ammo == AmmoID.None && !IsSpecialAmmo(item.type))
            return;

        int helmet = player.head;
        bool matchesHelmet = false;

        if (helmet == ArmorIDs.Head.ShroomiteHeadgear)
        {
            if (item.ammo == AmmoID.Arrow)
                matchesHelmet = true;
        }
        else if (helmet == ArmorIDs.Head.ShroomiteMask)
        {
            if (item.ammo == AmmoID.Bullet)
                matchesHelmet = true;
        }
        else if (helmet == ArmorIDs.Head.ShroomiteHelmet)
        {
            if (item.ammo == AmmoID.Rocket ||
                item.ammo == AmmoID.Dart ||
                IsSpecialAmmo(item.type))
            {
                matchesHelmet = true;
            }
        }

        if (!matchesHelmet)
        {
            if (_originalDamage != -1 && item.damage != _originalDamage)
            {
                item.damage = _originalDamage;
                _originalDamage = -1;
            }
            return;
        }

        if (_originalDamage == -1)
        {
            _originalDamage = item.damage;
        }

        int boostedDamage = (int)(_originalDamage * 1.15);
        if (boostedDamage < 1) boostedDamage = 1;

        item.damage = boostedDamage;
    }

    private static bool IsSpecialAmmo(int itemType)
    {
        return itemType == ItemID.Stake ||
               itemType == ItemID.Nail ||
               itemType == ItemID.ExplosiveJackOLantern ||
               itemType == ItemID.StyngerBolt ||
               itemType == ItemID.CandyCorn;
    }
}