using System;
using System.Collections.Generic;
using System.ComponentModel;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace PvPAdventure;

public class AdventureConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ServerSide;

    public PointsConfig Points { get; set; } = new();
    public List<Bounty> Bounties { get; set; } = new();
    public CombatConfig Combat { get; set; } = new();
    public List<ItemDefinition> PreventUse { get; set; } = new();

    public List<NPCDefinition> NpcSpawnAnnouncements { get; set; } = new()
    {
        new(NPCID.CultistBoss)
    };

    public List<NPCDefinition> BossOrder { get; set; } =
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

    [DefaultValue(true)] public bool OnlyDisplayWorldEvilBoss { get; set; }

    public List<ProjectileDefinition> BossInvulnerableProjectiles { get; set; } =
    [
        new(ProjectileID.Dynamite),
        new(ProjectileID.StickyDynamite),
        new(ProjectileID.BouncyDynamite)
    ];

    public class InvasionSizeValue
    {
        [Range(0, 1000)] public int Value { get; set; }
    }

    public Dictionary<int, InvasionSizeValue> InvasionSizes { get; set; } = new();

    [Range(0, 60 * 60)]
    [DefaultValue(4 * 60)]
    public int RecallFrames { get; set; }

    [Range(0, 30 * 60)]
    [DefaultValue(1.5 * 60)]
    public int SpawnImmuneFrames { get; set; }

    public List<string> CrashoutMessages { get; set; } =
    [
        "Is it break yet?",
        "Getting mogged by Matte \"Heat Ray\" Sevai",
        "If you aren't good enough, go play THC",
        "39 buried. 0 Tabis.",
        "That right there is 100% skill issue",
        "Too many surface RTPs"
    ];

    [DefaultValue(true)] public bool ShareWorldMap { get; set; }

    [Description("Discord IDs that are allowed to modify the server configuration")]
    public List<string> AllowConfigModification { get; set; } = new();

    [Description("Percent chance that our bound NPCs spawn")]
    [DefaultValue(0.25f)]
    public float BoundSpawnChance { get; set; }

    public bool RemovePrefixes { get; set; }

    public class PointsConfig
    {
        public Dictionary<NPCDefinition, NpcPoints> Npc { get; set; } = new();

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
    }

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

    public class Bounty
    {
        public List<ConfigItem> Items { get; set; } = [];
        public Condition Conditions { get; set; } = new();
    }

    public class CombatConfig
    {
        public class PlayerDamageBalanceConfig
        {
            public Dictionary<ItemDefinition, float> ItemDamageMultipliers { get; set; } = new();
            public Dictionary<ProjectileDefinition, float> ProjectileDamageMultipliers { get; set; } = new();

            public class Falloff
            {
                [Increment(0.0001f)]
                [Range(0.0f, 1.0f)]
                public float Coefficient { get; set; }

                [Increment(0.05f)]
                [Range(0.0f, 100.0f)]
                public float Forward { get; set; }

                public float CalculateMultiplier(float tileDistance) =>
                    (float)Math.Min(Math.Pow(Math.E, -(Coefficient * (tileDistance - Forward) / 100.0)), 1.0);
            }

            public Dictionary<ItemDefinition, Falloff> ItemFalloff { get; set; } = new();
            public Dictionary<ProjectileDefinition, Falloff> ProjectileFalloff { get; set; } = new();

            [DefaultValue(null)] [NullAllowed] public Falloff DefaultFalloff { get; set; }
        }

        [Range(0, 5 * 60)] [DefaultValue(8)] public int MeleeInvincibilityFrames { get; set; }

        [Range(0, 60 * 2 * 60)]
        [DefaultValue(15 * 60)]
        public int RecentDamagePreservationFrames { get; set; }

        public PlayerDamageBalanceConfig PlayerDamageBalance { get; set; } = new();

        [Range(0, 5 * 60)] [DefaultValue(8)] public int StandardInvincibilityFrames { get; set; }

        [DefaultValue(0.2f)] public float GhostHealMultiplier { get; set; }

        [Description("Additional multiplier for any player that can Ghost Heal")]
        [DefaultValue(1.0f)]
        public float GhostHealMultiplierWearers { get; set; }

        [Range(0.0f, 3000.0f)]
        [DefaultValue(3000.0f)]
        public float GhostHealMaxDistance { get; set; }

        public class TeamLifeConfig
        {
            [DefaultValue(0.5f)] public float Share { get; set; } = 0.5f;
        }

        [Description("Require each team to deal the maximum life in total damage to kill this NPC")]
        public Dictionary<NPCDefinition, TeamLifeConfig> TeamLifeNpcs { get; set; } = new();
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

        [DefaultValue(null)] [NullAllowed] public OptionalInt Damage { get; set; }
        [DefaultValue(null)] [NullAllowed] public OptionalInt UseTime { get; set; }
        [DefaultValue(null)] [NullAllowed] public OptionalInt UseAnimation { get; set; }
        [DefaultValue(null)] [NullAllowed] public OptionalFloat ShootSpeed { get; set; }
        [DefaultValue(null)] [NullAllowed] public OptionalInt Crit { get; set; }
        [DefaultValue(null)] [NullAllowed] public OptionalInt Mana { get; set; }
        [DefaultValue(null)] [NullAllowed] public OptionalFloat Scale { get; set; }
        [DefaultValue(null)] [NullAllowed] public OptionalFloat Knockback { get; set; }
        [DefaultValue(null)] [NullAllowed] public OptionalInt Value { get; set; }

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

    [ReloadRequired] public Dictionary<ItemDefinition, Statistics> ItemStatistics { get; set; } = new();

    public class WorldGenerationConfig
    {
        [DefaultValue(2)] public int LifeFruitChanceDenominator { get; set; } = 2;

        [DefaultValue(2)] public int LifeFruitExpertChanceDenominator { get; set; } = 2;

        [DefaultValue(2)] public int LifeFruitMinimumDistanceBetween { get; set; } = 2;

        [DefaultValue(30)] public int PlanteraBulbChanceDenominator { get; set; } = 30;

        [DefaultValue(8)] public int ChlorophyteSpreadChanceModifier { get; set; } = 8;
    }

    public WorldGenerationConfig WorldGeneration { get; set; } = new();

    public List<ItemDefinition> PreventAutoReuse { get; set; } = new();

    public class NpcBalanceConfig
    {
        public class FloatStatistic
        {
            [Range(0.0f, 5.0f)] public float Value { get; set; }
        }

        public Dictionary<NPCDefinition, FloatStatistic> LifeMaxMultipliers { get; set; } = new();
        public Dictionary<NPCDefinition, FloatStatistic> DamageMultipliers { get; set; } = new();
        public bool NoMechanicalBossSummonDrops { get; set; }
        public bool NoBiomeKeyDrops { get; set; }
    }

    public NpcBalanceConfig NpcBalance { get; set; } = new();

    public class ChestItemReplacement
    {
        public List<ConfigItem> Items { get; set; } = new();
    }

    public Dictionary<ItemDefinition, ChestItemReplacement> ChestItemReplacements { get; set; } = new();

    [Range(0, 600)] public int MinimumDamageReceivedByPlayers { get; set; }
    [Range(0, 600)] public int MinimumDamageReceivedByPlayersFromPlayer { get; set; }

    public override bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref NetworkText message)
    {
        if (pendingConfig is not AdventureConfig pendingAdventureConfig)
            return true;

        var adventureConfig = ModContent.GetInstance<AdventureConfig>();
        var discordId = Main.player[whoAmI].GetModPlayer<AdventurePlayer>().DiscordUser?.Id;
        if (discordId == null)
            return false;

        if (!adventureConfig.AllowConfigModification.Contains(discordId.ToString()))
        {
            message = NetworkText.FromKey("Mods.PvPAdventure.Configs.CannotModify");
            return false;
        }

        // You must have access by this point, but then you removed yourself!
        // Don't do that.
        if (!pendingAdventureConfig.AllowConfigModification.Contains(discordId.ToString()))
        {
            message = NetworkText.FromKey("Mods.PvPAdventure.Configs.CannotModify");
            return false;
        }

        return true;
    }
}