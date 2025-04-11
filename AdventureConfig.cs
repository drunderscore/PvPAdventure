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

    public Dictionary<int, int> InvasionSizes { get; set; } = new();

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
                [Range(0.0f, 3.0f)]
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
    }

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