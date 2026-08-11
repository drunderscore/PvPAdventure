using PvPAdventure.Core.Config.ConfigElements;
using PvPOnline.Core.Configs.ConfigElements;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace PvPAdventure.Core.Config;

public sealed class ServerConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ServerSide;

    [Header("Points")]
    [HeaderIcon(nameof(Ass.IconPointsSetter))]
    [BackgroundColor(150, 104, 38)]
    [Expand(false, false)]
    public PointsConfig Points = new();

    [BackgroundColor(150, 104, 38)]
    [Expand(false, false)]
    public BountiesConfig Bounties = new();

    [Header("Gameplay")]
    [BackgroundColor(205, 110, 60)]
    [Expand(false, false)]
    public ShakingChestConfig ShakingChest = new();

    [ConfigIcon(nameof(Ass.ConfigBed), placement: ConfigIconPlacement.Cut)]
    [BackgroundColor(205, 110, 60)]
    [Expand(false, false)]
    public TravelSystemConfig TravelSystem = new();

    [Header("NPCs")]
    [HeaderIcon(267)]
    [ConfigIcon(nameof(Ass.ConfigBoundNPC))]
    [BackgroundColor(58, 108, 72)]
    [DefaultValue(ServerConfigDefaults.BoundSpawnChance)]
    public float BoundSpawnChance = ServerConfigDefaults.BoundSpawnChance;

    [Header("World")]
    [HeaderIcon(ItemID.WorldGlobe)]
    [BackgroundColor(72, 104, 72)]
    [Expand(false, false)]
    [CustomModConfigItem(typeof(InvasionDictionaryElement))]
    public Dictionary<int, InvasionSizeValue> InvasionSizes = ServerConfigDefaults.CreateInvasionSizes();

    [BackgroundColor(72, 104, 72)]
    [DefaultValue(true)]
    public bool DisableTombstones = true;

    [BackgroundColor(72, 104, 72)]
    [DefaultValue(true)]
    public bool IncreaseRainFrequency = true;

    [BackgroundColor(72, 104, 72)]
    [DefaultValue(true)]
    public bool DisableLunarApocalypse = true;

    [BackgroundColor(72, 104, 72)]
    [DefaultValue(true)]
    public bool StartHardmodeGoblinInvasion = true;

    [BackgroundColor(72, 104, 72)]
    [DefaultValue(true)]
    public bool BroadcastWeatherMessages = true;

    [Header("WorldGen")]
    [HeaderIcon(ItemID.WorldGlobe)]
    [BackgroundColor(114, 90, 46)]
    [Expand(false, false)]
    public WorldGenerationConfig WorldGeneration = new();

    public override void OnLoaded()
    {
        Points ??= new();
        Points.Npc ??= ServerConfigDefaults.CreateNpcPoints();
        Bounties ??= new();
        Bounties.ClaimableItems ??= ServerConfigDefaults.CreateClaimableBounties();
        ShakingChest ??= new();
        ShakingChest.ShopItems ??= ServerConfigDefaults.CreateShopItems();
        TravelSystem ??= new();
        WorldGeneration ??= new();
        InvasionSizes ??= ServerConfigDefaults.CreateInvasionSizes();
    }

    public sealed class ShakingChestConfig
    {
        [BackgroundColor(205, 110, 60)]
        [Range(0, 9999)]
        [DefaultValue(ServerConfigDefaults.StartingCoins)]
        public int StartingCoins = ServerConfigDefaults.StartingCoins;

        [BackgroundColor(205, 110, 60)]
        [Expand(false, false)]
        [CustomModConfigItem(typeof(ShopItemListElement))]
        public List<ShopItem> ShopItems = ServerConfigDefaults.CreateShopItems();
    }

    public sealed class ShopItem
    {
        [BackgroundColor(205, 110, 60)]
        public ItemDefinition Item = new();

        [BackgroundColor(205, 110, 60)]
        [Range(0, 100000000)]
        public int Price;
    }

    public override bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref NetworkText message)
    {
        if (Main.netMode == NetmodeID.SinglePlayer)
            return true;

        if (!ModLoader.TryGetMod("ErkySSC", out Mod erkySsc))
        {
            message = NetworkText.FromLiteral("Server config changes require ErkySSC admin permissions.");
            return false;
        }

        bool isAdmin = false;

        try
        {
            if (erkySsc.Call("IsAdmin", whoAmI) is bool value)
                isAdmin = value;
        }
        catch (Exception exception)
        {
            Log.Chat($"Failed to check ErkySSC admin permission for config change. whoAmI={whoAmI}, error={exception.Message}");
        }

        if (!isAdmin)
        {
            message = NetworkText.FromLiteral("You must be an ErkySSC admin to modify this config.");
            return false;
        }

        message = NetworkText.FromLiteral("Saved!");
        return true;
    }

    public sealed class PointsConfig
    {
        [BackgroundColor(150, 104, 38)]
        [Expand(false, false)]
        [CustomModConfigItem(typeof(DefinitionDictionaryElement))]
        public Dictionary<NPCDefinition, NpcPoints> Npc = ServerConfigDefaults.CreateNpcPoints();

        [BackgroundColor(150, 104, 38)]
        [Expand(false, false)]
        public NpcPoints Boss = new()
        {
            First = 2,
            Additional = 1
        };

        [BackgroundColor(150, 104, 38)]
        [DefaultValue(ServerConfigDefaults.PlayerKillPoints)]
        public int PlayerKill = ServerConfigDefaults.PlayerKillPoints;

        public sealed class NpcPoints
        {
            [BackgroundColor(150, 104, 38)]
            public int First;
            [BackgroundColor(150, 104, 38)]
            public int Additional;
            [BackgroundColor(150, 104, 38)]
            public bool Repeatable;
        }

        [BackgroundColor(150, 104, 38)]
        [DefaultValue(ServerConfigDefaults.TeamStartingPoints)]
        public int TeamStartingPoints = ServerConfigDefaults.TeamStartingPoints;

        [BackgroundColor(150, 104, 38)]
        [DefaultValue(0)]
        public int BedKill;

        [BackgroundColor(150, 104, 38)]
        [DefaultValue(0)]
        public int PortalKill;
    }

    public sealed class BountiesConfig
    {
        [BackgroundColor(150, 104, 38)]
        [Expand(false, false)]
        public List<Bounty> ClaimableItems = ServerConfigDefaults.CreateClaimableBounties();

        [BackgroundColor(150, 104, 38)]
        [DefaultValue(ServerConfigDefaults.AwardBountyEveryKill)]
        public bool AwardBountyEveryKill = ServerConfigDefaults.AwardBountyEveryKill;

        public sealed class Bounty
        {
            [BackgroundColor(150, 104, 38)]
            public List<ConfigItem> Items = [];
            [BackgroundColor(150, 104, 38)]
            public Condition Conditions = new();
        }
    }

    public sealed class TravelSystemConfig
    {
        [BackgroundColor(205, 110, 60)]
        [ConfigIcon(nameof(PvPOnline.Core.Utilities.Ass.CheckGreen), nameof(PvPOnline.Core.Utilities.Ass.XGray), grayWhenOff: true)]
        [DefaultValue(true)]
        public bool IsTravelSystemEnabled = true;

        [BackgroundColor(205, 110, 60)]
        [RequiresField(nameof(IsTravelSystemEnabled))]
        [ConfigIcon(nameof(Ass.ConfigMapWorldSpawn))]
        [DefaultValue(true)]
        public bool IsWorldSpawnTeleportEnabled = true;

        [BackgroundColor(205, 110, 60)]
        [RequiresField(nameof(IsTravelSystemEnabled))]
        [ConfigIcon(nameof(Ass.ConfigPlayerHead))]
        [DefaultValue(true)]
        public bool IsTeammateSpawnTeleportEnabled = true;

        [BackgroundColor(205, 110, 60)]
        [RequiresField(nameof(IsTravelSystemEnabled))]
        [ConfigIcon(nameof(Ass.IconQuestionMark))]
        [DefaultValue(ServerConfigDefaults.IsRandomTeleportEnabled)]
        public bool IsRandomTeleportEnabled = ServerConfigDefaults.IsRandomTeleportEnabled;

        [BackgroundColor(205, 110, 60)]
        [RequiresField(nameof(IsTravelSystemEnabled))]
        [DefaultValue(true)]
        public bool AllowSpectating = true;

        [BackgroundColor(205, 110, 60)]
        [RequiresField(nameof(IsTravelSystemEnabled))]
        [Range(0, 60)]
        [DefaultValue(ServerConfigDefaults.TravelPortalCreationTimePreHardmodeSeconds)]
        public int TravelPortalCreationTimePreHardmodeSeconds = ServerConfigDefaults.TravelPortalCreationTimePreHardmodeSeconds;

        [BackgroundColor(205, 110, 60)]
        [RequiresField(nameof(IsTravelSystemEnabled))]
        [Range(0, 60)]
        [DefaultValue(10)]
        public int TravelPortalCreationTimeHardmodeSeconds = 10;

        [BackgroundColor(205, 110, 60)]
        [RequiresField(nameof(IsTravelSystemEnabled))]
        [Range(0, 60)]
        [DefaultValue(8)]
        public int TravelRegionRadiusTiles = 8;

        [BackgroundColor(205, 110, 60)]
        [RequiresField(nameof(IsTravelSystemEnabled))]
        [Range(-60, 60)]
        [DefaultValue(30)]
        public int PortalCreationOffset = 30;

        [BackgroundColor(205, 110, 60)]
        [RequiresField(nameof(IsTravelSystemEnabled))]
        [Range(0, 60)]
        [DefaultValue(ServerConfigDefaults.TeleportCooldownSeconds)]
        public int TeleportCooldownSeconds = ServerConfigDefaults.TeleportCooldownSeconds;
    }

    public sealed class WorldGenerationConfig
    {
        [BackgroundColor(114, 90, 46)]
        [ConfigIcon(ItemID.LifeFruit)]
        [DefaultValue(ServerConfigDefaults.LifeFruitChanceDenominator)]
        public int LifeFruitChanceDenominator = ServerConfigDefaults.LifeFruitChanceDenominator;

        [BackgroundColor(114, 90, 46)]
        [ConfigIcon(ItemID.LifeFruit)]
        [DefaultValue(ServerConfigDefaults.LifeFruitExpertChanceDenominator)]
        public int LifeFruitExpertChanceDenominator = ServerConfigDefaults.LifeFruitExpertChanceDenominator;

        [BackgroundColor(114, 90, 46)]
        [ConfigIcon(ItemID.LifeFruit)]
        [DefaultValue(ServerConfigDefaults.LifeFruitMinimumDistanceBetween)]
        public int LifeFruitMinimumDistanceBetween = ServerConfigDefaults.LifeFruitMinimumDistanceBetween;

        [BackgroundColor(114, 90, 46)]
        [ConfigIcon(nameof(Ass.ConfigPlanterasBulb))]
        [DefaultValue(ServerConfigDefaults.PlanteraBulbChanceDenominator)]
        public int PlanteraBulbChanceDenominator = ServerConfigDefaults.PlanteraBulbChanceDenominator;

        [BackgroundColor(114, 90, 46)]
        [ConfigIcon(ItemID.ChlorophyteOre)]
        [DefaultValue(ServerConfigDefaults.ChlorophyteSpreadChanceModifier)]
        public int ChlorophyteSpreadChanceModifier = ServerConfigDefaults.ChlorophyteSpreadChanceModifier;

        [BackgroundColor(114, 90, 46)]
        [ConfigIcon(ItemID.ChlorophyteOre)]
        [Range(1, 1000)]
        [DefaultValue(ServerConfigDefaults.ChlorophyteGrowChanceModifier)]
        public int ChlorophyteGrowChanceModifier = ServerConfigDefaults.ChlorophyteGrowChanceModifier;

        [BackgroundColor(114, 90, 46)]
        [ConfigIcon(ItemID.ChlorophyteOre)]
        [Range(1, 999999)]
        [DefaultValue(ServerConfigDefaults.ChlorophyteGrowLimitModifier)]
        public int ChlorophyteGrowLimitModifier = ServerConfigDefaults.ChlorophyteGrowLimitModifier;
    }

    public sealed class InvasionSizeValue
    {
        [BackgroundColor(72, 104, 72)]
        [Range(0, 1000)]
        public int Value;
    }

    public sealed class ConfigItem
    {
        [BackgroundColor(150, 104, 38)]
        public ItemDefinition Item = new();
        [BackgroundColor(150, 104, 38)]
        public PrefixDefinition Prefix = new();
        private int stack = 1;

        [BackgroundColor(150, 104, 38)]
        public int Stack
        {
            get => stack;
            set => stack = Math.Clamp(value, 1, new Item(Item.Type, 1, Prefix.Type).maxStack);
        }
    }

    public sealed class Condition
    {
        public enum WorldProgressionState
        {
            Any,
            PreHardmode,
            Hardmode
        }

        [BackgroundColor(150, 104, 38)] public WorldProgressionState WorldProgression;
        [BackgroundColor(150, 104, 38)] public bool SkeletronPrimeDefeated;
        [BackgroundColor(150, 104, 38)] public bool TwinsDefeated;
        [BackgroundColor(150, 104, 38)] public bool DestroyerDefeated;
        [BackgroundColor(150, 104, 38)] public bool PlanteraDefeated;
        [BackgroundColor(150, 104, 38)] public bool GolemDefeated;
        [BackgroundColor(150, 104, 38)] public bool SkeletronDefeated;
        [BackgroundColor(150, 104, 38)] public bool CollectedAllMechanicalBossSouls;
    }
}
