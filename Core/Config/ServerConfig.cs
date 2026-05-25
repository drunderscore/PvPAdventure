using PvPAdventure.Common.Combat;
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

public class ServerConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ServerSide;

    #region Members

    [Header("Points")]
    [HeaderIcon(nameof(Ass.IconPointsSetter))]
    [BackgroundColor(150, 104, 38)]
    [Expand(false, false)]
    public PointsConfig Points = new();

    [BackgroundColor(150, 104, 38)]
    [Expand(false, false)]
    public BountiesConfig Bounties = new();

    [Header("Combat")]
    [HeaderIcon(nameof(Ass.ConfigPvP))]
    [BackgroundColor(142, 54, 50)]
    [Expand(false, false)]
    public WeaponBalanceConfig WeaponBalance = new();

    [BackgroundColor(142, 54, 50)]
    [Expand(false, false)]
    public ImmunityConfig Immunity = new();

    [BackgroundColor(142, 54, 50)]
    [Expand(false, false)]
    public OtherConfig Other = new();

    [Header("Items")]
    [HeaderIcon(ItemID.Chest)]
    [BackgroundColor(54, 86, 132)]
    [ReloadRequired]
    [Expand(false, false)]
    [CustomModConfigItem(typeof(DefinitionDictionaryElement))]
    public Dictionary<ItemDefinition, Statistics> ItemStatistics = [];

    [BackgroundColor(54, 86, 132)]
    [Expand(false, false)]
    [CustomModConfigItem(typeof(DefinitionDictionaryElement))]
    public Dictionary<ItemDefinition, ChestItemReplacement> ChestItemReplacements = [];

    [BackgroundColor(54, 86, 132)]
    public bool RemovePrefixes;

    [Header("Bosses")]
    [HeaderIcon(2112)]

    [BackgroundColor(104, 58, 140)]
    [Expand(false, false)]
    [CustomModConfigItem(typeof(DefinitionDictionaryElement))]
    public Dictionary<NPCDefinition, BossBalanceEntry> BossBalance = [];

    [BackgroundColor(104, 58, 140)]
    [Expand(false, false)]
    public List<NPCDefinition> BossSpawnAnnouncements = [new(NPCID.CultistBoss)];

    [BackgroundColor(104, 58, 140)]
    [Expand(false, false)]
    public List<NPCDefinition> BossOrder = [];

    [BackgroundColor(104, 58, 140)]
    [Expand(false, false)]
    public List<ProjectileDefinition> BossInvulnerableProjectiles = [new(ProjectileID.Dynamite)];

    [BackgroundColor(104, 58, 140)]
    [DefaultValue(true)] public bool NoMechanicalBossSummonDrops = true;

    [BackgroundColor(104, 58, 140)]
    [DefaultValue(true)] public bool OnlyDisplayWorldEvilBoss = true;

    [Header("NPCs")]
    [HeaderIcon(267)]
    [ConfigIcon(nameof(Ass.ConfigBoundNPC))]
    [BackgroundColor(58, 108, 72)]
    [DefaultValue(0.25f)]
    public float BoundSpawnChance = 0.25f;

    [Header("Gameplay")]
    [HeaderIcon(ItemID.GPS)]

    [BackgroundColor(36, 108, 116)]
    [Expand(false, false)]
    public TravelSystemConfig TravelSystem = new();

    [BackgroundColor(36, 108, 116)]
    [Range(0, 30 * 60)]
    [DefaultValue(90)]
    public int SpawnImmuneFrames = 90;

    [BackgroundColor(36, 108, 116)]
    [Range(0, 600)]
    public int MinimumDamageReceivedByPlayers;

    [BackgroundColor(36, 108, 116)]
    [Range(0, 600)]
    public int MinimumDamageReceivedByPlayersFromPlayer;

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
    #endregion

    #region NestedConfigTypes
    public class PointsConfig
    {
        // Points per boss
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

        public class NpcPoints
        {
            public int First;
            public int Additional;
            public bool Repeatable;
        }

        [DefaultValue(5)]
        public int TeamStartingPoints = 5;
    }
    
    public class BountiesConfig
    {
        [Expand(false, false)]
        public List<Bounty> ClaimableItems = [];

        [DefaultValue(false)]
        public bool AwardBountyEveryKill = false;
        public class Bounty
        {
            public List<ConfigItem> Items = [];
            public Condition Conditions = new();
        }
    }

    public class TravelSystemConfig
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
        [DefaultValue(true)]
        public bool ShowPortalCreationProjectile = true;

        [RequiresField(nameof(IsTravelSystemEnabled))]
        [Range(-60, 60)]
        [DefaultValue(30)]
        public int PortalCreationOffset = 30;

        [RequiresField(nameof(IsTravelSystemEnabled))]
        [Range(0, 60)]
        [DefaultValue(5)]
        public int TeleportCooldownSeconds = 5;
    }

    public class WeaponBalanceConfig
    {
        [Expand(false, false)]
        public DamageConfig Damage = new();

        [Expand(false, false)]
        public ArmorPenetrationConfig ArmorPenetration = new();

        [Expand(false, false)]
        public FalloffConfig Falloff = new();

        [Expand(false, false)]
        public KnockbackConfig Knockback = new();

        [Range(0.0f, 1.0f)]
        [DefaultValue(0.0f)]
        public float ProjectileBounceDamageReduction = 0.0f;

        [Expand(false, false)]
        public Dictionary<ProjectileDefinition, float> ProjectileLineOfSightDamageReduction = [];

        public class DamageConfig
        {
            [Expand(false, false)]
            [CustomModConfigItem(typeof(DefinitionDictionaryElement))]
            public Dictionary<ItemDefinition, float> ItemDamage = [];

            [Expand(false, false)]
            [CustomModConfigItem(typeof(DefinitionDictionaryElement))]
            public Dictionary<ProjectileDefinition, float> ProjectileDamage = [];
        }

        public class ArmorPenetrationConfig
        {
            [Increment(0.01f)]
            [Range(0.0f, 1.0f)]
            [Expand(false, false)]
            [CustomModConfigItem(typeof(DefinitionDictionaryElement))]
            public Dictionary<ItemDefinition, float> ItemAP = [];

            [Increment(0.01f)]
            [Range(0.0f, 1.0f)]
            [Expand(false, false)]
            [CustomModConfigItem(typeof(DefinitionDictionaryElement))]
            public Dictionary<ProjectileDefinition, float> ProjectileAP = [];
        }
        public class KnockbackConfig
        {
            [Expand(false, false)]
            [CustomModConfigItem(typeof(DefinitionDictionaryElement))]
            [Range(0f, 2f)]
            [Increment(0.01f)]
            [Slider]
            public Dictionary<ItemDefinition, float> ItemKnockback = [];

            [Expand(false, false)]
            [CustomModConfigItem(typeof(DefinitionDictionaryElement))]
            [Range(0f, 2f)]
            [Increment(0.01f)]
            [Slider]
            public Dictionary<ProjectileDefinition, float> ProjectileKnockback = [];

            [Range(0f, 1f)]
            [DefaultValue(0.5f)]
            [Increment(0.01f)]
            [Slider]
            public float PvPKnockbackMultiplier = 0.8f;
        }

        public class FalloffConfig
        {
            public class Falloff
            {
                [Increment(0.0001f)]
                [Range(0.0f, 5.0f)]
                public float Coefficient;

                [Increment(0.05f)]
                [Range(0.0f, 100.0f)]
                public float Forward;

                public float CalculateMultiplier(float tileDistance) =>
                    (float)Math.Min(Math.Pow(Math.E, -(Coefficient * (tileDistance - Forward) / 100.0)), 1.0);
            }

            [DefaultValue(null)]
            [NullAllowed]
            public Falloff Default;

            [Expand(false, false)]
            [CustomModConfigItem(typeof(DefinitionDictionaryElement))]
            public Dictionary<ItemDefinition, Falloff> PerItem = [];

            [Expand(false, false)]
            [CustomModConfigItem(typeof(DefinitionDictionaryElement))]
            public Dictionary<ProjectileDefinition, Falloff> PerProjectile = [];
        }
    }

    public class OtherConfig
    {
        [Expand(false, false)]
        public SpectreHealingConfig SpectreHealing = new();

        [Expand(false, false)]
        public BeetleScaleMailConfig BeetleScaleMail = new();

        public class BeetleScaleMailConfig
        {
            [Increment(0.01f)]
            [Range(0f, 10f)]
            [DefaultValue(1f)]
            [Slider]
            public float EnergyMultiplier = 1f;

            [Increment(0.5f)]
            [Range(0f, 10f)]
            [DefaultValue(1f)]
            [Slider]
            public float EnergyDecayPerTick = 1f;

            [Increment(100f)]
            [Range(0f, 54000f)]
            [DefaultValue(5400f)]
            [Slider]
            public float EnergyMax = 5400f;

            [Increment(100f)]
            [Range(0f, 54000f)]
            [DefaultValue(900f)]
            [Slider]
            public float Tier1Threshold = 900f;

            [Increment(100f)]
            [Range(0f, 54000f)]
            [DefaultValue(2160f)]
            [Slider]
            public float Tier2Threshold = 2160f;

            [Increment(100f)]
            [Range(0f, 54000f)]
            [DefaultValue(4860f)]
            [Slider]
            public float Tier3Threshold = 4860f;

        }
        public class SpectreHealingConfig
        {
            [DefaultValue(0.2f)]
            public float PvPHealMultiplier = 0.2f;

            [DefaultValue(1.0f)]
            public float PvPSelfHealMultiplier = 1.0f;

            [Range(0.0f, 3000.0f)]
            [DefaultValue(3000.0f)]
            public float PvPHealRange = 3000.0f;

            [Range(0.0f, 3000.0f)]
            [DefaultValue(3000.0f)]
            public float PvEHealRange = 3000.0f;

            [Increment(0.01f)]
            [Range(0.0f, 1.0f)]
            [DefaultValue(0.5f)]
            public float HealerArmorPenetration = 0.5f;
        }
    }

    public class ImmunityConfig
    {
        [Range(0, 5 * 60)]
        [DefaultValue(8)]
        public int TrueMelee = 8;

        [Range(0, 5 * 60)]
        [DefaultValue(8)]
        public int PerPlayerGlobal = 8;

        [Range(0, 60 * 2 * 60)]
        [DefaultValue(15 * 60)]
        public int RecentDamagePreservationFrames = 15 * 60;

        [Expand(false, false)]
        [CustomModConfigItem(typeof(DefinitionDictionaryElement))]
        public Dictionary<ProjectileDefinition, ProjectileImmunityGroup> ProjectileDamageImmunityGroup = [];
    }

    public class ProjectileImmunityGroup
    {
        [Range(0, CombatManager.MaximumNumberOfGroupCooldownId - 1)]
        public int Id;

        [DefaultValue(8)]
        public int Frames = 8;
    }

    public class BossBalanceEntry
    {
        [Range(0f, 5f)]
        [DefaultValue(1f)]
        public float LifeMaxMultiplier = 1f;

        [Range(0f, 5f)]
        [DefaultValue(1f)]
        public float DamageMultiplier = 1f;

        [Range(0f, 1f)]
        [DefaultValue(0.5f)]
        public float TeamLifeShare = 0.5f;
    }

    public class WorldGenerationConfig
    {
        [ConfigIcon(ItemID.LifeFruit)]
        [DefaultValue(2)] public int LifeFruitChanceDenominator = 2;

        [ConfigIcon(ItemID.LifeFruit)]
        [DefaultValue(2)] public int LifeFruitExpertChanceDenominator = 2;

        [ConfigIcon(ItemID.LifeFruit)]
        [DefaultValue(2)] public int LifeFruitMinimumDistanceBetween = 2;

        [ConfigIcon(nameof(Ass.ConfigPlanterasBulb))]
        [DefaultValue(30)] public int PlanteraBulbChanceDenominator = 30;

        [ConfigIcon(ItemID.ChlorophyteOre)]
        [DefaultValue(8)] public int ChlorophyteSpreadChanceModifier = 8;

        [ConfigIcon(ItemID.ChlorophyteOre)]
        [Range(1, 1000)]
        [DefaultValue(300)] public int ChlorophyteGrowChanceModifier = 300;

        [ConfigIcon(ItemID.ChlorophyteOre)]
        [Range(1, 999999)]
        [DefaultValue(300)] public int ChlorophyteGrowLimitModifier = 300;
    }

    #endregion

    #region Small helpers
    public class ChestItemReplacement
    {
        public List<ConfigItem> Items = [];
    }
    public class InvasionSizeValue
    {
        [Range(0, 1000)] public int Value;
    }
    #endregion

    #region Helpers
    public class ConfigItem
    {
        public ItemDefinition Item = new();
        public PrefixDefinition Prefix = new();
        private int _stack = 1;

        // NOTE: Just for QOL. Can be screwed with by changing the above item after setting this.
        public int Stack
        {
            get => _stack;
            set => _stack = Math.Clamp(value, 1, new Item(Item.Type, 1, Prefix.Type).maxStack);
        }
    }

    public class Condition
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

    public class Statistics : IEquatable<Statistics>
    {
        // FIXME: tModLoader does not have struct support, so nullables (System.Nullable) won't work -- and would
        //        require some extra handling to display in the UI properly. (1)
        //        Make an incredibly simplified "optional" type, where it is good enough to indicate an "empty" by
        //        holding a null reference to it.
        //        Can't do any better than this -- even trying to use CustomModConfigItem fails because _someone_ made
        //        most of the UI mod config elements internal, so we can't extend their functionality without uselessly
        //        re-implementing them, which I won't condone. (2)
        //        Can't use a generic class like Optional<T> because UI attributes needs to go onto the property, and
        //        it cannot be repeated, and the Range attribute actually cares about the underlying type you give it
        //        when specifying a limit! (3)
        //        Don't forget that floats ONLY have sliders, which have HUGE inaccuracy problems if you put the range
        //        maximum range higher, which is always the case -- there is genuinely no possible way to specify a
        //        float value that is anywhere near considered "precise" or "precise enough" in the config. (4)
        //
        //        Yes, you heard right, there are 4 issues all right here, stemming from tModLoader's poor code.

        public class OptionalInt : IEquatable<OptionalInt>
        {
            [Range(0, 1000000)] public int Value;

            public bool Equals(OptionalInt other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Value == other.Value;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj.GetType() == GetType() && Equals((OptionalInt)obj);
            }

            public override int GetHashCode() => Value;
        }

        public class OptionalFloat : IEquatable<OptionalFloat>
        {
            [Increment(0.05f)]
            [Range(0.0f, 100.0f)]
            public float Value;

            public bool Equals(OptionalFloat other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Value.Equals(other.Value);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj.GetType() == GetType() && Equals((OptionalFloat)obj);
            }

            public override int GetHashCode() => Value.GetHashCode();
        }

        [DefaultValue(null)][NullAllowed] public OptionalInt Damage;
        [DefaultValue(null)][NullAllowed] public OptionalInt UseTime;
        [DefaultValue(null)][NullAllowed] public OptionalInt UseAnimation;
        [DefaultValue(null)][NullAllowed] public OptionalFloat ShootSpeed;
        [DefaultValue(null)][NullAllowed] public OptionalInt Crit;
        [DefaultValue(null)][NullAllowed] public OptionalInt Mana;
        [DefaultValue(null)][NullAllowed] public OptionalFloat Scale;
        [DefaultValue(null)][NullAllowed] public OptionalFloat Knockback;
        [DefaultValue(null)][NullAllowed] public OptionalInt Value;

        public bool Equals(Statistics other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Damage, other.Damage) && Equals(UseTime, other.UseTime) &&
                   Equals(UseAnimation, other.UseAnimation) && Equals(ShootSpeed, other.ShootSpeed) &&
                   Equals(Crit, other.Crit) && Equals(Mana, other.Mana) && Equals(Scale, other.Scale) &&
                   Equals(Knockback, other.Knockback) && Equals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((Statistics)obj);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(Damage);
            hashCode.Add(UseTime);
            hashCode.Add(UseAnimation);
            hashCode.Add(ShootSpeed);
            hashCode.Add(Crit);
            hashCode.Add(Mana);
            hashCode.Add(Scale);
            hashCode.Add(Knockback);
            hashCode.Add(Value);
            return hashCode.ToHashCode();
        }
    }
    #endregion

    #region Hooks / methods
    public override void OnLoaded()
    {
        base.OnLoaded();

        BossOrder ??= [];
        TravelSystem ??= new();
        if (BossOrder.Count == 0)
            BossOrder = CreateDefaultBossOrder();
    }

    public override bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref NetworkText message)
    {
        // Singleplayer always allowed
        if (Main.netMode == NetmodeID.SinglePlayer)
            return true;

        // If dragonlens isn't loaded, disallow modifying the config.
        if (!ModLoader.HasMod("DragonLens"))
        {
            message = NetworkText.FromLiteral("Server config changes require DragonLens admin (DragonLens not loaded).");
            return false;
        }

        // DragonLens admin check
        return AcceptClientChanges_DragonLens(whoAmI, ref message);
    }

    [JITWhenModsEnabled("DragonLens")]
    private static bool AcceptClientChanges_DragonLens(int whoAmI, ref NetworkText message)
    {
        Player player = Main.player[whoAmI];

        if (!DragonLens.Core.Systems.PermissionHandler.CanUseTools(player))
        {
            message = NetworkText.FromLiteral("You must be a DragonLens admin to modify this config.");
            return false;
        }
        message = NetworkText.FromLiteral("Saved!");

        return true;
    }

    public override void HandleAcceptClientChangesReply(bool success, int player, NetworkText message)
    {
        Log.Chat("Server accepted changes!");
        base.HandleAcceptClientChangesReply(success, player, message);
    }
    public override void OnChanged()
    {
        base.OnChanged();
    }
    #endregion

    #region Default values
    private static List<NPCDefinition> CreateDefaultBossOrder()
    {
        return
        [
            new(NPCID.KingSlime),
            new(NPCID.EyeofCthulhu),
            new(NPCID.EaterofWorldsHead),
            new(NPCID.BrainofCthulhu),
            new(NPCID.QueenBee),
            new(NPCID.SkeletronHead),
            new(NPCID.Deerclops),
            new(NPCID.WallofFlesh),
            new(NPCID.QueenSlimeBoss),
            new(NPCID.Retinazer),
            new(NPCID.TheDestroyer),
            new(NPCID.SkeletronPrime),
            new(NPCID.Plantera),
            new(NPCID.Golem),
            new(NPCID.DukeFishron),
            new(NPCID.HallowBoss),
            new(NPCID.CultistBoss)
        ];
    }
    #endregion

}
