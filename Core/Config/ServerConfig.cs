using Newtonsoft.Json.Converters;
using PvPAdventure.Common.Combat;
using PvPAdventure.Core.Config.ConfigElements;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;
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
    [BackgroundColor(140, 100, 20)]
    [Expand(false, false)]
    public PointsConfig Points { get; set; } = new();

    [BackgroundColor(140, 100, 20)]
    [Expand(false, false)]
    public BountiesConfig Bounties { get; set; } = new();

    [BackgroundColor(140, 100, 20)]
    [Expand(false, false)]
    [CustomModConfigItem(typeof(InvasionDictionaryElement))]
    public Dictionary<int, InvasionSizeValue> InvasionSizes { get; set; } = [];

    [Header("Combat")]
    [BackgroundColor(40, 90, 40)]
    [Expand(false, false)]
    public WeaponBalanceConfig WeaponBalance { get; set; } = new();

    [BackgroundColor(40, 90, 40)]
    [Expand(false, false)]
    public ImmunityConfig Immunity { get; set; } = new();

    [BackgroundColor(40, 90, 40)]
    [Expand(false, false)]
    public OtherConfig Other { get; set; } = new();

    [Header("Items")]
    [BackgroundColor(40, 60, 110)]
    [Expand(false, false)]
    public List<ItemDefinition> PreventUse { get; set; } = [];

    [BackgroundColor(40, 60, 110)]
    [ReloadRequired]
    [Expand(false, false)]
    [CustomModConfigItem(typeof(DefinitionDictionaryElement))]
    public Dictionary<ItemDefinition, Statistics> ItemStatistics { get; set; } = [];

    [BackgroundColor(40, 60, 110)]
    [Expand(false, false)]
    public List<ItemDefinition> PreventAutoReuse { get; set; } = [];

    [BackgroundColor(40, 60, 110)]
    [Expand(false, false)]
    [CustomModConfigItem(typeof(DefinitionDictionaryElement))]
    public Dictionary<ItemDefinition, ChestItemReplacement> ChestItemReplacements { get; set; } = [];

    [BackgroundColor(40, 60, 110)]
    public bool RemovePrefixes { get; set; }

    [Header("Bosses")]

    [BackgroundColor(90, 40, 110)]
    [Expand(false, false)]
    [CustomModConfigItem(typeof(DefinitionDictionaryElement))]
    public Dictionary<NPCDefinition, BossBalanceEntry> BossBalance { get; set; } = [];

    [BackgroundColor(90, 40, 110)]
    [Expand(false, false)]
    public List<NPCDefinition> BossSpawnAnnouncements { get; set; } = [new(NPCID.CultistBoss)];

    [BackgroundColor(90, 40, 110)]
    [Expand(false, false)]
    public List<NPCDefinition> BossOrder { get; set; } = [];

    [BackgroundColor(90, 40, 110)]
    [Expand(false, false)]
    public List<ProjectileDefinition> BossInvulnerableProjectiles { get; set; } = [new(ProjectileID.Dynamite)];

    [BackgroundColor(90, 40, 110)]
    [DefaultValue(true)] public bool NoMechanicalBossSummonDrops { get; set; }

    [BackgroundColor(90, 40, 110)]
    [DefaultValue(true)] public bool OnlyDisplayWorldEvilBoss { get; set; }

    [Header("NPCs")]
    [BackgroundColor(90, 40, 110)]
    [DefaultValue(0.25f)]
    public float BoundSpawnChance { get; set; }

    [Header("TravelSystem")]
    [BackgroundColor(30, 150, 150)]
    [Range(0, 60)]
    [DefaultValue(true)]
    public bool IsTravelSystemEnabled { get; set; }

    [BackgroundColor(30, 150, 150)]
    [Range(0, 60)]
    [DefaultValue(8)]
    public int TravelRegionRadiusTiles { get; set; }

    [BackgroundColor(30, 150, 150)]
    [Range(0, 60)]
    [DefaultValue(4)]
    public int TravelPortalCreationTimeSeconds { get; set; }

    [Range(0, 60)]
    [DefaultValue(true)]
    [BackgroundColor(30, 150, 150)]
    public bool ShowPortalCreationProjectile { get; set; }

    [BackgroundColor(30, 150, 150)]
    [Range(-60, 60)]
    [DefaultValue(30)]
    public int PortalCreationOffset { get; set; }

    [BackgroundColor(30, 150, 150)]
    [Range(0, 60)]
    [DefaultValue(5)]
    public int TeleportCooldownSeconds { get; set; }

    [Header("Gameplay")]

    [BackgroundColor(40, 90, 40)]
    [Range(0, 30 * 60)]
    [DefaultValue(1.5 * 60)]
    public int SpawnImmuneFrames { get; set; }

    [BackgroundColor(40, 90, 40)]
    [Range(0, 600)]
    public int MinimumDamageReceivedByPlayers { get; set; }

    [BackgroundColor(40, 90, 40)]
    [Range(0, 600)]
    public int MinimumDamageReceivedByPlayersFromPlayer { get; set; }

    [BackgroundColor(40, 90, 40)]
    [DefaultValue(AllowMode.BeforeGameStart)]
    [JsonConverter(typeof(StringEnumConverter))]
    public AllowMode AllowPlayersToChangeTeam { get; set; } = AllowMode.BeforeGameStart;

    [Header("Security")]
    [BackgroundColor(150, 70, 20)]
    [Expand(false, false)]
    public ClientModsConfig ClientMods { get; set; } = new();

    [BackgroundColor(150, 70, 20)]
    [Expand(false, false)]
    public WhitelistPlayersConfig WhitelistPlayers { get; set; } = new();

    [BackgroundColor(150, 70, 20)]
    [Expand(false, false)]
    public AutoAdminsConfig AutoAdmins { get; set; } = new();

    [Header("WorldGen")]
    [BackgroundColor(90, 70, 40)]
    [Expand(false, false)]
    public WorldGenerationConfig WorldGeneration { get; set; } = new();
    #endregion

    #region NestedConfigTypes
    public class PointsConfig
    {
        // Points per boss
        [Expand(false, false)]
        [CustomModConfigItem(typeof(DefinitionDictionaryElement))]
        public Dictionary<NPCDefinition, NpcPoints> Npc { get; set; } = [];

        [Expand(false, false)]
        public NpcPoints Boss { get; set; } = new()
        {
            First = 2,
            Additional = 1
        };

        public int PlayerKill { get; set; } = 1;

        public class NpcPoints
        {
            public int First { get; set; }
            public int Additional { get; set; }
            public bool Repeatable { get; set; }
        }

        [DefaultValue(5)]
        public int TeamStartingPoints { get; set; } = 5;
    }
    
    public class BountiesConfig
    {
        [Expand(false, false)]
        public List<Bounty> ClaimableItems { get; set; } = [];

        [DefaultValue(false)]
        public bool AwardBountyEveryKill { get; set; }
        public class Bounty
        {
            public List<ConfigItem> Items { get; set; } = [];
            public Condition Conditions { get; set; } = new();
        }
    }

    public class WeaponBalanceConfig
    {
        [Expand(false, false)]
        public DamageConfig Damage { get; set; } = new();

        [Expand(false, false)]
        public ArmorPenetrationConfig ArmorPenetration { get; set; } = new();

        [Expand(false, false)]
        public FalloffConfig Falloff { get; set; } = new();

        [Expand(false, false)]
        public KnockbackConfig Knockback { get; set; } = new();

        [Range(0.0f, 1.0f)]
        [DefaultValue(0.0f)]
        public float ProjectileBounceDamageReduction { get; set; } = 0.0f;

        [Expand(false, false)]
        public Dictionary<ProjectileDefinition, float> ProjectileLineOfSightDamageReduction { get; set; } = [];

        public class DamageConfig
        {
            [Expand(false, false)]
            [CustomModConfigItem(typeof(DefinitionDictionaryElement))]
            public Dictionary<ItemDefinition, float> ItemDamage { get; set; } = [];

            [Expand(false, false)]
            [CustomModConfigItem(typeof(DefinitionDictionaryElement))]
            public Dictionary<ProjectileDefinition, float> ProjectileDamage { get; set; } = [];
        }

        public class ArmorPenetrationConfig
        {
            [Increment(0.01f)]
            [Range(0.0f, 1.0f)]
            [Expand(false, false)]
            [CustomModConfigItem(typeof(DefinitionDictionaryElement))]
            public Dictionary<ItemDefinition, float> ItemAP { get; set; } = [];

            [Increment(0.01f)]
            [Range(0.0f, 1.0f)]
            [Expand(false, false)]
            [CustomModConfigItem(typeof(DefinitionDictionaryElement))]
            public Dictionary<ProjectileDefinition, float> ProjectileAP { get; set; } = [];
        }
        public class KnockbackConfig
        {
            [Expand(false, false)]
            [CustomModConfigItem(typeof(DefinitionDictionaryElement))]
            [Range(0f, 2f)]
            [Increment(0.01f)]
            [Slider]
            public Dictionary<ItemDefinition, float> ItemKnockback { get; set; } = [];

            [Expand(false, false)]
            [CustomModConfigItem(typeof(DefinitionDictionaryElement))]
            [Range(0f, 2f)]
            [Increment(0.01f)]
            [Slider]
            public Dictionary<ProjectileDefinition, float> ProjectileKnockback { get; set; } = [];

            [Range(0f, 1f)]
            [DefaultValue(0.5f)]
            [Increment(0.01f)]
            [Slider]
            public float PvPKnockbackMultiplier { get; set; } = 0.8f;
        }

        public class FalloffConfig
        {
            public class Falloff
            {
                [Increment(0.0001f)]
                [Range(0.0f, 5.0f)]
                public float Coefficient { get; set; }

                [Increment(0.05f)]
                [Range(0.0f, 100.0f)]
                public float Forward { get; set; }

                public float CalculateMultiplier(float tileDistance) =>
                    (float)Math.Min(Math.Pow(Math.E, -(Coefficient * (tileDistance - Forward) / 100.0)), 1.0);
            }

            [DefaultValue(null)]
            [NullAllowed]
            public Falloff Default { get; set; }

            [Expand(false, false)]
            [CustomModConfigItem(typeof(DefinitionDictionaryElement))]
            public Dictionary<ItemDefinition, Falloff> PerItem { get; set; } = [];

            [Expand(false, false)]
            [CustomModConfigItem(typeof(DefinitionDictionaryElement))]
            public Dictionary<ProjectileDefinition, Falloff> PerProjectile { get; set; } = [];
        }
    }

    public class OtherConfig
    {
        [Expand(false, false)]
        public SpectreHealingConfig SpectreHealing { get; set; } = new();

        [Expand(false, false)]
        public BeetleScaleMailConfig BeetleScaleMail { get; set; } = new();

        public class BeetleScaleMailConfig
        {
            [Increment(0.01f)]
            [Range(0f, 10f)]
            [DefaultValue(1f)]
            [Slider]
            public float EnergyMultiplier { get; set; } = 1f;

            [Increment(0.5f)]
            [Range(0f, 10f)]
            [DefaultValue(1f)]
            [Slider]
            public float EnergyDecayPerTick { get; set; } = 1f;

            [Increment(100f)]
            [Range(0f, 54000f)]
            [DefaultValue(5400f)]
            [Slider]
            public float EnergyMax { get; set; } = 5400f;

            [Increment(100f)]
            [Range(0f, 54000f)]
            [DefaultValue(900f)]
            [Slider]
            public float Tier1Threshold { get; set; } = 900f;

            [Increment(100f)]
            [Range(0f, 54000f)]
            [DefaultValue(2160f)]
            [Slider]
            public float Tier2Threshold { get; set; } = 2160f;

            [Increment(100f)]
            [Range(0f, 54000f)]
            [DefaultValue(4860f)]
            [Slider]
            public float Tier3Threshold { get; set; } = 4860f;

        }
        public class SpectreHealingConfig
        {
            [DefaultValue(0.2f)]
            public float PvPHealMultiplier { get; set; }

            [DefaultValue(1.0f)]
            public float PvPSelfHealMultiplier { get; set; }

            [Range(0.0f, 3000.0f)]
            [DefaultValue(3000.0f)]
            public float PvPHealRange { get; set; }

            [Range(0.0f, 3000.0f)]
            [DefaultValue(3000.0f)]
            public float PvEHealRange { get; set; }

            [Increment(0.01f)]
            [Range(0.0f, 1.0f)]
            [DefaultValue(0.5f)]
            public float HealerArmorPenetration { get; set; }
        }
    }

    public class ImmunityConfig
    {
        [Range(0, 5 * 60)]
        [DefaultValue(8)]
        public int TrueMelee { get; set; } = 8;

        [Range(0, 5 * 60)]
        [DefaultValue(8)]
        public int PerPlayerGlobal { get; set; } = 8;

        [Range(0, 60 * 2 * 60)]
        [DefaultValue(15 * 60)]
        public int RecentDamagePreservationFrames { get; set; } = 15 * 60;

        [Expand(false, false)]
        [CustomModConfigItem(typeof(DefinitionDictionaryElement))]
        public Dictionary<ProjectileDefinition, ProjectileImmunityGroup> ProjectileDamageImmunityGroup { get; set; } = [];
    }

    public class ProjectileImmunityGroup
    {
        [Range(0, CombatManager.MaximumNumberOfGroupCooldownId - 1)]
        public int Id { get; set; }

        [DefaultValue(8)]
        public int Frames { get; set; } = 8;
    }

    public class BossBalanceEntry
    {
        [Range(0f, 5f)]
        [DefaultValue(1f)]
        public float LifeMaxMultiplier { get; set; } = 1f;

        [Range(0f, 5f)]
        [DefaultValue(1f)]
        public float DamageMultiplier { get; set; } = 1f;

        [Range(0f, 1f)]
        [DefaultValue(0.5f)]
        public float TeamLifeShare { get; set; } = 0.5f;
    }

    public class ClientModsConfig
    {
        [DefaultValue(false)]
        public bool AllowAnyClientMods { get; set; }

        [Expand(false, false)]
        public List<string> AllowedClientMods { get; set; } = [];
    }

    public class WhitelistPlayersConfig
    {
        [DefaultValue(true)]
        public bool AllowAnyPlayerToJoin { get; set; }

        [Expand(false, false)]
        public List<string> AllowedPlayerSteamIds { get; set; } = [];
    }

    public class AutoAdminsConfig
    {
        [DefaultValue(false)]
        public bool Enabled { get; set; }

        [Expand(false, false)]
        public List<string> SteamIds { get; set; } = [];
    }

    public class WorldGenerationConfig
    {
        [DefaultValue(2)] public int LifeFruitChanceDenominator { get; set; } = 2;

        [DefaultValue(2)] public int LifeFruitExpertChanceDenominator { get; set; } = 2;

        [DefaultValue(2)] public int LifeFruitMinimumDistanceBetween { get; set; } = 2;

        [DefaultValue(30)] public int PlanteraBulbChanceDenominator { get; set; } = 30;

        [DefaultValue(8)] public int ChlorophyteSpreadChanceModifier { get; set; } = 8;

        [Range(1, 1000)]
        [DefaultValue(300)] public int ChlorophyteGrowChanceModifier { get; set; } = 300;

        [Range(1, 999999)]
        [DefaultValue(300)] public int ChlorophyteGrowLimitModifier { get; set; } = 300;
    }

    #endregion

    #region Small helpers
    public class ChestItemReplacement
    {
        public List<ConfigItem> Items { get; set; } = [];
    }
    public class InvasionSizeValue
    {
        [Range(0, 1000)] public int Value { get; set; }
    }
    public enum AllowMode
    {
        Always,
        BeforeGameStart,
        Never
    }
    #endregion

    #region Helpers
    public class ConfigItem
    {
        public ItemDefinition Item { get; set; } = new();
        public PrefixDefinition Prefix { get; set; } = new();
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

        public WorldProgressionState WorldProgression { get; set; }
        public bool SkeletronPrimeDefeated { get; set; }
        public bool TwinsDefeated { get; set; }
        public bool DestroyerDefeated { get; set; }
        public bool PlanteraDefeated { get; set; }
        public bool GolemDefeated { get; set; }
        public bool SkeletronDefeated { get; set; }
        public bool CollectedAllMechanicalBossSouls { get; set; }
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
            [Range(0, 1000000)] public int Value { get; set; }

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
            public float Value { get; set; }

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

        [DefaultValue(null)][NullAllowed] public OptionalInt Damage { get; set; }
        [DefaultValue(null)][NullAllowed] public OptionalInt UseTime { get; set; }
        [DefaultValue(null)][NullAllowed] public OptionalInt UseAnimation { get; set; }
        [DefaultValue(null)][NullAllowed] public OptionalFloat ShootSpeed { get; set; }
        [DefaultValue(null)][NullAllowed] public OptionalInt Crit { get; set; }
        [DefaultValue(null)][NullAllowed] public OptionalInt Mana { get; set; }
        [DefaultValue(null)][NullAllowed] public OptionalFloat Scale { get; set; }
        [DefaultValue(null)][NullAllowed] public OptionalFloat Knockback { get; set; }
        [DefaultValue(null)][NullAllowed] public OptionalInt Value { get; set; }

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