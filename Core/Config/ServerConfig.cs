using PvPAdventure.Core.Config.ConfigElements;
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

    [Header("NPCs")]
    [HeaderIcon(267)]
    [ConfigIcon(nameof(Ass.ConfigBoundNPC))]
    [BackgroundColor(58, 108, 72)]
    [DefaultValue(0.25f)]
    public float BoundSpawnChance = 0.25f;

    [Header("Travel")]
    [HeaderIcon(ItemID.GPS)]
    [ConfigIcon(nameof(Ass.ConfigBed), placement: ConfigIconPlacement.Cut)]
    [BackgroundColor(36, 108, 116)]
    [Expand(false, false)]
    public TravelSystemConfig TravelSystem = new();

    [Header("WorldGen")]
    [HeaderIcon(ItemID.WorldGlobe)]
    [BackgroundColor(114, 90, 46)]
    [Expand(false, false)]
    public WorldGenerationConfig WorldGeneration = new();

    [Header("World")]
    [HeaderIcon(ItemID.WorldGlobe)]
    [BackgroundColor(72, 104, 72)]
    [Expand(false, false)]
    [CustomModConfigItem(typeof(InvasionDictionaryElement))]
    public Dictionary<int, InvasionSizeValue> InvasionSizes = [];

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

    public override void OnLoaded()
    {
        TravelSystem ??= new();
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
        [Expand(false, false)]
        [CustomModConfigItem(typeof(DefinitionDictionaryElement))]
        public Dictionary<NPCDefinition, NpcPoints> Npc = [];

        [Expand(false, false)]
        public NpcPoints Boss = new()
        {
            First = 2,
            Additional = 1
        };

        public int PlayerKill = 1;

        public sealed class NpcPoints
        {
            public int First;
            public int Additional;
            public bool Repeatable;
        }

        [DefaultValue(5)]
        public int TeamStartingPoints = 5;

        [DefaultValue(0)]
        public int BedKill;

        [DefaultValue(0)]
        public int PortalKill;
    }

    public sealed class BountiesConfig
    {
        [Expand(false, false)]
        public List<Bounty> ClaimableItems = [];

        [DefaultValue(false)]
        public bool AwardBountyEveryKill;

        public sealed class Bounty
        {
            public List<ConfigItem> Items = [];
            public Condition Conditions = new();
        }
    }

    public sealed class TravelSystemConfig
    {
        [ConfigIcon(nameof(Ass.IconCheckGreen), nameof(Ass.IconXGray), grayWhenOff: true)]
        [DefaultValue(true)]
        public bool IsTravelSystemEnabled = true;

        [RequiresField(nameof(IsTravelSystemEnabled))]
        [ConfigIcon(nameof(Ass.ConfigMapWorldSpawn))]
        [DefaultValue(true)]
        public bool IsWorldSpawnTeleportEnabled = true;

        [RequiresField(nameof(IsTravelSystemEnabled))]
        [ConfigIcon(nameof(Ass.ConfigPlayerHead))]
        [DefaultValue(true)]
        public bool IsTeammateSpawnTeleportEnabled = true;

        [RequiresField(nameof(IsTravelSystemEnabled))]
        [ConfigIcon(nameof(Ass.IconQuestionMark))]
        [DefaultValue(true)]
        public bool IsRandomTeleportEnabled = true;

        [RequiresField(nameof(IsTravelSystemEnabled))]
        [DefaultValue(true)]
        public bool AllowSpectating = true;

        [RequiresField(nameof(IsTravelSystemEnabled))]
        [Range(0, 60)]
        [DefaultValue(5)]
        public int TravelPortalCreationTimePreHardmodeSeconds = 5;

        [RequiresField(nameof(IsTravelSystemEnabled))]
        [Range(0, 60)]
        [DefaultValue(10)]
        public int TravelPortalCreationTimeHardmodeSeconds = 10;

        [RequiresField(nameof(IsTravelSystemEnabled))]
        [Range(0, 60)]
        [DefaultValue(8)]
        public int TravelRegionRadiusTiles = 8;

        [RequiresField(nameof(IsTravelSystemEnabled))]
        [Range(-60, 60)]
        [DefaultValue(30)]
        public int PortalCreationOffset = 30;

        [RequiresField(nameof(IsTravelSystemEnabled))]
        [Range(0, 60)]
        [DefaultValue(5)]
        public int TeleportCooldownSeconds = 5;
    }

    public sealed class WorldGenerationConfig
    {
        [ConfigIcon(ItemID.LifeFruit)]
        [DefaultValue(2)]
        public int LifeFruitChanceDenominator = 2;

        [ConfigIcon(ItemID.LifeFruit)]
        [DefaultValue(2)]
        public int LifeFruitExpertChanceDenominator = 2;

        [ConfigIcon(ItemID.LifeFruit)]
        [DefaultValue(2)]
        public int LifeFruitMinimumDistanceBetween = 2;

        [ConfigIcon(nameof(Ass.ConfigPlanterasBulb))]
        [DefaultValue(30)]
        public int PlanteraBulbChanceDenominator = 30;

        [ConfigIcon(ItemID.ChlorophyteOre)]
        [DefaultValue(8)]
        public int ChlorophyteSpreadChanceModifier = 8;

        [ConfigIcon(ItemID.ChlorophyteOre)]
        [Range(1, 1000)]
        [DefaultValue(300)]
        public int ChlorophyteGrowChanceModifier = 300;

        [ConfigIcon(ItemID.ChlorophyteOre)]
        [Range(1, 999999)]
        [DefaultValue(300)]
        public int ChlorophyteGrowLimitModifier = 300;
    }

    public sealed class InvasionSizeValue
    {
        [Range(0, 1000)]
        public int Value;
    }

    public sealed class ConfigItem
    {
        public ItemDefinition Item = new();
        public PrefixDefinition Prefix = new();
        private int stack = 1;

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

        public WorldProgressionState WorldProgression;
        public bool SkeletronPrimeDefeated;
        public bool TwinsDefeated;
        public bool DestroyerDefeated;
        public bool PlanteraDefeated;
        public bool GolemDefeated;
        public bool SkeletronDefeated;
        public bool CollectedAllMechanicalBossSouls;
    }
}
