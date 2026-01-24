using PvPAdventure.Content.Buffs;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Combat;

internal class PreventHotswapPlayer : ModPlayer
{
    private bool hadPhilostoneLastFrame;
    private bool hadShinyStoneLastFrame;

    public override void PostUpdateEquips()
    {
        // Check if Shiny Stone is equipped
        bool hasShinyStone = IsShinyStoneEquipped();

        // Apply debuff when first equipped or after respawn
        if (hasShinyStone && !hadShinyStoneLastFrame)
        {
            Player.AddBuff(ModContent.BuffType<ShinyStoneHotswap>(), 3600); // 60 seconds
        }

        // Disable Shiny Stone effects while debuffed
        if (Player.HasBuff(ModContent.BuffType<ShinyStoneHotswap>()))
        {
            Player.shinyStone = false;
        }
        bool hasPhilostone = IsPhilostoneEquipped();
        hadShinyStoneLastFrame = hasShinyStone;

        if (hasPhilostone && !hadPhilostoneLastFrame)
        {
            Player.AddBuff(ModContent.BuffType<UncoutHandboring>(), 3600); // 60 seconds
        }

        hadPhilostoneLastFrame = hasPhilostone;


        if (Player.beetleOffense)
        {
            Player.GetDamage<MeleeDamageClass>() += 0;
            Player.GetAttackSpeed<MeleeDamageClass>() += 0;
        }
        else
        {
            // If we don't have the beetle offense set bonus, remove all possible buffs.
            Player.ClearBuff(BuffID.BeetleMight1);
            Player.ClearBuff(BuffID.BeetleMight2);
            Player.ClearBuff(BuffID.BeetleMight3);
        }

        if (Player.HasBuff(BuffID.BeetleMight3))
        {
            // we apply the glowing eye effect from Yoraiz0rsSpell item
            Player.yoraiz0rEye = 33;
        }

        if (Player.hasPaladinShield)
        {
            Player.buffImmune[BuffID.PaladinsShield] = true;
        }

        if (Player.active)
        {
            Player.buffImmune[BuffID.Confused] = true;
            Player.buffImmune[BuffID.BrokenArmor] = true;
            Player.buffImmune[BuffID.Electrified] = true;

        }
    }
    private bool IsSpectreSetEquipped()
    {
        int head = Player.armor[0].type;
        int body = Player.armor[1].type;
        int legs = Player.armor[2].type;

        bool hasSpectreHead = IsSpectreHead(head);
        bool hasSpectreBody = body == ItemID.SpectreRobe;
        bool hasSpectreLegs = legs == ItemID.SpectrePants;

        return hasSpectreHead && hasSpectreBody && hasSpectreLegs;
    }

    private bool IsShinyStoneEquipped()
    {
        for (int i = 3; i < 10; i++) // Check all accessory slots
        {
            if (Player.armor[i].type == ItemID.ShinyStone &&
               (i < 7 || !Player.hideVisibleAccessory[i - 3]))
            {
                return true;
            }
        }
        return false;
    }
    private bool IsPhilostoneEquipped()
    {
        for (int i = 3; i < 10; i++) // Check all accessory slots
        {
            if (Player.armor[i].type == ItemID.PhilosophersStone || (Player.armor[i].type == ItemID.CharmofMyths) &&
               (i < 7 || !Player.hideVisibleAccessory[i - 3]))
            {
                return true;
            }
        }
        return false;
    }
    private bool IsSpectreHead(int headType)
    {
        return headType == ItemID.SpectreHood || headType == ItemID.SpectreMask;
    }
}
