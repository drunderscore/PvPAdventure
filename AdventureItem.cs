using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.Utilities;

namespace PvPAdventure;

public class AdventureItem : GlobalItem
{
    public static readonly bool[] RecallItems =
        ItemID.Sets.Factory.CreateBoolSet(ItemID.MagicMirror, ItemID.CellPhone, ItemID.IceMirror, ItemID.Shellphone,
            ItemID.ShellphoneSpawn);

    public override void SetDefaults(Item item)
    {
        var adventureConfig = ModContent.GetInstance<AdventureConfig>();

        if (item.type == ItemID.LihzahrdPowerCell)
        {

            item.rare = ItemRarityID.Yellow;
        }

        if (RecallItems[item.type])
        {
            var recallTime = adventureConfig.RecallFrames;
            item.useTime = recallTime * 2;
            item.useAnimation = recallTime * 2;
        }

        // Can't construct an ItemDefinition too early -- it'll call GetName and won't be graceful on failure.
        if (ItemID.Search.TryGetName(item.type, out var name) &&
            adventureConfig.ItemStatistics.TryGetValue(new ItemDefinition(name), out var statistics))
        {
            if (statistics.Damage != null)
                item.damage = statistics.Damage.Value;
            if (statistics.UseTime != null)
                item.useTime = statistics.UseTime.Value;
            if (statistics.UseAnimation != null)
                item.useAnimation = statistics.UseAnimation.Value;
            if (statistics.ShootSpeed != null)
                item.shootSpeed = statistics.ShootSpeed.Value;
            if (statistics.Crit != null)
                item.crit = statistics.Crit.Value;
            if (statistics.Mana != null)
                item.mana = statistics.Mana.Value;
            if (statistics.Scale != null)
                item.scale = statistics.Scale.Value;
            if (statistics.Knockback != null)
                item.knockBack = statistics.Knockback.Value;
            if (statistics.Value != null)
                item.value = statistics.Value.Value;
        }
        if (item.type == ItemID.SpectrePickaxe || item.type == ItemID.ShroomiteDiggingClaw)
            item.pick = 210;
    }

    public override void SetStaticDefaults()
    {
        void AddCircularShimmerTransform(params int[] items)
        {
            for (var i = 1; i < items.Length; i++)
                ItemID.Sets.ShimmerTransformToItem[items[i - 1]] = items[i];

            ItemID.Sets.ShimmerTransformToItem[items[^1]] = items[0];
        }

        AddCircularShimmerTransform(
            ItemID.CrystalNinjaHelmet,
            ItemID.CrystalNinjaChestplate,
            ItemID.CrystalNinjaLeggings
        );
        AddCircularShimmerTransform(
            ItemID.GladiatorHelmet,
            ItemID.GladiatorBreastplate,
            ItemID.GladiatorLeggings
        );
        AddCircularShimmerTransform(ItemID.PaladinsHammer, ItemID.PaladinsShield);
        AddCircularShimmerTransform(ItemID.MaceWhip, ItemID.Keybrand);
        AddCircularShimmerTransform(ItemID.SniperRifle, ItemID.RifleScope);
        AddCircularShimmerTransform(ItemID.Tabi, ItemID.BlackBelt);
        AddCircularShimmerTransform(ItemID.PiggyBank, ItemID.MoneyTrough);
    }

    public override bool CanUseItem(Item item, Player player)
    {
        var isUnderground = player.position.Y > Main.worldSurface * 16;
        var isHallow = player.ZoneHallow;

        if (item.type == ItemID.EmpressButterfly)
        {
            if (isUnderground)
                return false;
        }
        else if (item.type == ItemID.QueenSlimeCrystal)
        {
            if (isUnderground)
                return false;
        }
        else if (item.type == ItemID.MechanicalEye)
        {
            if (isUnderground)
                return false;
        }
        else if (item.type == ItemID.MechanicalSkull)
        {
            if (isUnderground)
                return false;
        }
        else if (item.type == ItemID.MechanicalWorm)
        {
            if (isUnderground)
                return false;
        }

        return !ModContent.GetInstance<AdventureConfig>().PreventUse
            .Any(itemDefinition => item.type == itemDefinition.Type);
    }

    // NOTE: This will not remove already-equipped accessories from players.
    public override bool CanEquipAccessory(Item item, Player player, int slot, bool modded)
    {
        return !ModContent.GetInstance<AdventureConfig>().PreventUse
            .Any(itemDefinition => item.type == itemDefinition.Type);
    }

    public override bool? CanBeChosenAsAmmo(Item ammo, Item weapon, Player player)
    {
        if (ModContent.GetInstance<AdventureConfig>().PreventUse
            .Any(itemDefinition => ammo.type == itemDefinition.Type))
            return false;

        return null;
    }

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
    {
        var adventureConfig = ModContent.GetInstance<AdventureConfig>();

        var itemDefinition = new ItemDefinition(item.type);
        if (adventureConfig.Combat.PlayerDamageBalance.ItemDamageMultipliers.TryGetValue(itemDefinition,
                out var multiplier))
        {
            // FIXME: The mod config is very imprecise with floating points. Do some rounding to make the UI cleaner.
            tooltips.Add(new TooltipLine(Mod, "CombatPlayerDamageBalance",
                $"-{(int)((1.0f - multiplier) * 100)}% PvP damage")
            {
                IsModifier = true,
                IsModifierBad = true
            });
        }

        if (adventureConfig.PreventUse.Contains(itemDefinition))
        {
            tooltips.Add(new TooltipLine(Mod, "Disabled", Language.GetTextValue("Mods.PvPAdventure.Item.Disabled"))
            {
                OverrideColor = Color.Red
            });
        }
        else
        {
            var isUnderground = Main.LocalPlayer.position.Y > Main.worldSurface * 16;
            var isHallow = Main.LocalPlayer.ZoneHallow;
            if (item.type == ItemID.EmpressButterfly)
            {
                if (isUnderground)
                {
                    tooltips.Add(new TooltipLine(Mod, "NoUndergroundEmpressButterfly",
                        Language.GetTextValue("Mods.PvPAdventure.Item.NoUndergroundEmpressButterfly"))
                    {
                        OverrideColor = Color.Red
                    });
                }
            }
            else if (item.type == ItemID.QueenSlimeCrystal)
            {
                if (isHallow && isUnderground)
                {
                    tooltips.Add(new TooltipLine(Mod, "NoUndergroundQueenSlimeCrystal",
                        Language.GetTextValue("Mods.PvPAdventure.Item.NoUndergroundQueenSlimeCrystal"))
                    {
                        OverrideColor = Color.Red
                    });
                }
            }
        }
    }

    public override bool? PrefixChance(Item item, int pre, UnifiedRandom rand)
    {
        // Prevent the item from spawning with a prefix, being placed into a reforge window, and loading with a prefix.
        if ((pre == -1 || pre == -3 || pre > 0) && ModContent.GetInstance<AdventureConfig>().RemovePrefixes)
            return false;

        return null;
    }

    // This is likely unnecessary if we are overriding PrefixChance, but might as well.
    public override bool CanReforge(Item item) => !ModContent.GetInstance<AdventureConfig>().RemovePrefixes;


    public class AdventureBag : ModItem
    {
        public override string Texture => $"PvPAdventure/Assets/Item/AdventureBag";
        public override void SetStaticDefaults()
        {
            ItemID.Sets.OpenableBag[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.rare = ItemRarityID.Orange;
            Item.maxStack = 1;
            Item.consumable = true;
        }

        public override bool CanRightClick()
        {
            return true;
        }

        public override void RightClick(Player player)
        {
            player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.SilverPickaxe, 1);    
            player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.SilverAxe, 1);        
            player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.Ambrosia, 1);         
            player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.SlimeBed, 1);        
            player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.Torch, 15);          
            player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.Wood, 20);             
            player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.LifeCrystal, 5);       
            player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.ManaCrystal, 4);        
            player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.MiningPotion, 2);       
            player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.WormholePotion, 4);
            player.QuickSpawnItem(player.GetSource_OpenItem(Type), ItemID.MagicMirror, 1);
        }

        public override bool ConsumeItem(Player player)
        {
            return true;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // Add custom tooltip line
            tooltips.Add(new TooltipLine(Mod, "PvPBagInfo", "Contains essential items for your PvP Adventure"));
        }
    }
    public class BlightFruit : ModItem
    {
        public override string Texture => $"PvPAdventure/Assets/Item/BlightFruit";

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.maxStack = 9999;
            Item.rare = ItemRarityID.Gray;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useAnimation = 1;
            Item.useTime = 1;
            Item.consumable = true;
            Item.UseSound = SoundID.Item27;
        }
        public override bool CanUseItem(Player player)
        {
            return player.ConsumedLifeFruit > 0;
        }

        public override bool? UseItem(Player player)
        {
            if (player.ConsumedLifeFruit > 0)
            {
                player.ConsumedLifeFruit--;

                player.statLifeMax = 100 + (player.ConsumedLifeCrystals * 20) + (player.ConsumedLifeFruit * 5);

                if (player.statLife > player.statLifeMax)
                {
                    player.statLife = player.statLifeMax;
                }

                if (Main.myPlayer == player.whoAmI)
                {
                    player.HealEffect(-5, true);
                }
            }

            return true;
        }
    }
    public class QuiverNerf : GlobalItem
    {
        public override void ModifyWeaponDamage(Item item, Player player, ref StatModifier damage)
        {
            QuiverNerfPlayer modPlayer = player.GetModPlayer<QuiverNerfPlayer>();

            if (modPlayer.hasQuiver && item.useAmmo == AmmoID.Arrow)
            {
                damage += -0.1f;
            }
        }
    }
    public class ItemTextChanges : GlobalItem
    {
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.type == ItemID.BeetleScaleMail || item.type == ItemID.BeetleShell)
            {
                TooltipLine setBonusLine = tooltips.FirstOrDefault(x => x.Name == "SetBonus" && x.Mod == "Terraria");
                if (setBonusLine != null)
                {
                    setBonusLine.Text = "Set bonus:\nGain Beetles from player kills\nBeetles increase your melee damage and attack speed";
                }
            }

            if (item.type == ItemID.TikiMask || item.type == ItemID.TikiShirt || item.type == ItemID.TikiPants)
            {
                TooltipLine setBonusLine = tooltips.FirstOrDefault(x => x.Name == "SetBonus" && x.Mod == "Terraria");
                if (setBonusLine != null)
                {
                    setBonusLine.Text = "Set bonus:\nIncreases your max number of minions\nIncreases whip range by 20%\nIncreases whip debuff duration against players by 150%\nPrevents whip range penalty";
                }
            }
            if (item.type == ItemID.ObsidianHelm || item.type == ItemID.ObsidianShirt || item.type == ItemID.ObsidianPants)
            {
                TooltipLine setBonusLine = tooltips.FirstOrDefault(x => x.Name == "SetBonus" && x.Mod == "Terraria");
                if (setBonusLine != null)
                {
                    setBonusLine.Text = "Set bonus:\n\tIncreases whip range by 30% and speed by 15%\nIncreases summon damage by 15%\nPrevents whip range penalty";
                }
            }
            if (item.type == ItemID.BlandWhip || item.type == ItemID.ThornWhip ||
            item.type == ItemID.BoneWhip || item.type == ItemID.FireWhip ||
            item.type == ItemID.CoolWhip || item.type == ItemID.SwordWhip ||
            item.type == ItemID.ScytheWhip || item.type == ItemID.MaceWhip ||
            item.type == ItemID.RainbowWhip)
            {
                tooltips.Add(new TooltipLine(Mod, "WhipRangeWarning", "Range greatly reduced without Pygmy Necklace, Hercules Beetle, or certain Set Bonuses")
                {
                    OverrideColor = new Color(255, 100, 100)
                });
                tooltips.Add(new TooltipLine(Mod, "SummonsArePlayers", "All whip debuffs apply to players, and effect all non-summon damage")
                {
                    OverrideColor = new Color(100, 255, 100)
                });
            }
            

            if (item.type == ItemID.PygmyNecklace || item.type == ItemID.HerculesBeetle)
            {
                tooltips.Add(new TooltipLine(Mod, "WhipRangeFix", "Prevents whip range penalty"));
            }
            if (item.type == ItemID.SpectrePickaxe || item.type == ItemID.ShroomiteDiggingClaw)
            {
                tooltips.Add(new TooltipLine(Mod, "MiningPowerChange", "Capable of mining Lihzahrd Bricks"));
            }
            if (item.type == ItemID.PhilosophersStone || item.type == ItemID.CharmofMyths)
            {
                tooltips.Add(new TooltipLine(Mod, "FullHPRespawn", "Gain full health upon returning to the land of the living"));
            }
            if (item.type == ItemID.MagicQuiver || item.type == ItemID.MoltenQuiver)
            {
                for (int i = 0; i < tooltips.Count; i++)
                {
                    if (tooltips[i].Text.Contains("Increases arrow damage by 10%") ||
                        tooltips[i].Text.Contains("arrow damage"))
                    {
                        tooltips[i].Text = "Greatly increases arrow speed";
                        break;
                    }
                }
                tooltips.Add(new TooltipLine(Mod, "QuiverNerf", "No longer grants arrow damage.\nPerhaps if it was a little more sneaky?")
                {
                    OverrideColor = new Color(255, 100, 100)
                });
            }
            if (item.type == ItemID.ArcheryPotion)
            {
                for (int i = 0; i < tooltips.Count; i++)
                {
                    if (tooltips[i].Text.Contains("10% increased bow damage") ||
                        tooltips[i].Text.Contains("bow damage and 20%"))
                    {
                        tooltips[i].Text = "20% increased arrow speed";
                        break;
                    }
                }
                tooltips.Add(new TooltipLine(Mod, "ArcheryNerf", "No longer grants bow damage")
                {
                    OverrideColor = new Color(255, 100, 100)
                });
            }
            if (item.type == ItemID.TempleKey)
            {
                for (int i = 0; i < tooltips.Count; i++)
                {
                    if (tooltips[i].Text.Contains("Opens the jungle temple door") ||
                        tooltips[i].Text.Contains("temple door"))
                    {
                        tooltips[i].Text = "Opens the jungle temple";
                        break;
                    }
                }
                tooltips.Add(new TooltipLine(Mod, "LihzahrdKey", "Opens the jungle temple's chests")
                {
                    OverrideColor = new Color(255, 100, 100)
                });
            }
            if (item.type == ItemID.LunarCraftingStation)
            {
                tooltips.Add(new TooltipLine(Mod, "AllCraftTiles", "Counts as all crafting stations"));
            }
        }
    }
}