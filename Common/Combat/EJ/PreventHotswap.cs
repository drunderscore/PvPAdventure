using PvPAdventure.Content.Buffs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Combat;

public class PermanentInfoDisplays : GlobalInfoDisplay
{
    public override bool? Active(InfoDisplay currentDisplay)
    {
        if (currentDisplay == InfoDisplay.Sextant || currentDisplay == InfoDisplay.DPSMeter)
            return true;

        return null;
    }
}

public class PermanentInfoAccessoriesItem : GlobalItem
{
    public override void UpdateInventory(Item item, Player player)
    {
        player.accWatch = ItemID.GoldWatch;
        player.accCalendar = true;
        player.accDreamCatcher = true;
    }
}
internal class PreventHotswapPlayer : ModPlayer
{
    private bool hadPhilostoneLastFrame;
    private bool hadShinyStoneLastFrame;

    public override void PostUpdateEquips()
    {
        if (Player.HasBuff(ModContent.BuffType<ShinyStoneHotswap>()))
        {
            Player.shinyStone = false;
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
            Player.buffImmune[BuffID.Horrified] = true;
            Player.buffImmune[BuffID.TheTongue] = true;
        }
    }

    public override void PostUpdate()
    {
        bool hasShinyStone = IsShinyStoneEquipped();
        if (hasShinyStone && !hadShinyStoneLastFrame)
        {
            Player.AddBuff(ModContent.BuffType<ShinyStoneHotswap>(), 3600); // 60 seconds
        }
        hadShinyStoneLastFrame = hasShinyStone;

        bool hasPhilostone = IsPhilostoneEquipped();
        if (hasPhilostone && !hadPhilostoneLastFrame)
        {
            Player.AddBuff(ModContent.BuffType<UncoutHandboring>(), 3600); // 60 seconds
        }
        hadPhilostoneLastFrame = hasPhilostone;
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
