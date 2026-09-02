using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace PvPAdventure.Common.Items;
/// <summary>
/// Allows demon heart to always be used, and makes it function like a non-consumable life crystal.
/// </summary>
public static class DemonHeartState
{
    public static bool RealExpertMode;
}

public class DemonHeartHooks : ILoadable
{
    public void Load(Mod mod)
    {
        On_Player.ItemCheck += ForceDemonHeartUsable;
    }

    public void Unload()
    {
        On_Player.ItemCheck -= ForceDemonHeartUsable;
    }

    private void ForceDemonHeartUsable(On_Player.orig_ItemCheck orig, Player self)
    {
        Item heldItem = self.inventory[self.selectedItem];
        bool isDemonHeart = heldItem.type == ItemID.DemonHeart;
        bool realExpertMode = Main.expertMode;
        int originalGameMode = Main.GameMode;
        var dhPlayer = self.GetModPlayer<DemonHeartPlayer>();

        if (isDemonHeart && !realExpertMode)
        {
            DemonHeartState.RealExpertMode = false;
            Main.GameMode = GameModeID.Expert;
        }
        else
        {
            DemonHeartState.RealExpertMode = realExpertMode;
        }

        dhPlayer.hadExtraAccessoryBeforeUse = self.extraAccessory;

        try
        {
            orig(self);
        }
        finally
        {
            if (isDemonHeart && !realExpertMode)
            {
                Main.GameMode = originalGameMode;

                if (!dhPlayer.hadExtraAccessoryBeforeUse && self.extraAccessory)
                    self.extraAccessory = false;
            }
        }
    }
}

public class DemonHeartPlayer : ModPlayer
{
    public bool hadExtraAccessoryBeforeUse;
    public int demonHeartConsumed;

    public override void ModifyMaxStats(out StatModifier health, out StatModifier mana)
    {
        base.ModifyMaxStats(out health, out mana);
        health.Flat += demonHeartConsumed * 20;
    }

    public override void SaveData(TagCompound tag)
    {
        tag["demonHeartConsumed"] = demonHeartConsumed;
    }

    public override void LoadData(TagCompound tag)
    {
        demonHeartConsumed = tag.GetInt("demonHeartConsumed");
    }
}

public class DemonHeartLifeBoost : GlobalItem
{
    public override bool AppliesToEntity(Item item, bool lateInstantiation)
        => item.type == ItemID.DemonHeart;

    public override bool CanUseItem(Item item, Player player)
    {
        return player.statLifeMax < 400 || (Main.hardMode && player.statLifeMax < 500);
    }

    public override bool? UseItem(Item item, Player player)
    {
        if (player.ConsumedLifeCrystals < 15 && !(Main.hardMode && player.ConsumedLifeCrystals >= 20))
        {
            player.ConsumedLifeCrystals++;
            player.statLife += 20;
            if (player.statLife > player.statLifeMax2)
                player.statLife = player.statLifeMax2;
        }
        return true;
    }

    public override bool ConsumeItem(Item item, Player player) => false;
}