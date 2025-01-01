using System;
using System.Collections.Generic;
using System.ComponentModel;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader.Config;

namespace PvPAdventure;

public class AdventureConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ServerSide;

    public PointsConfig Points { get; set; } = new();
    public List<Bounty> Bounties { get; set; } = new();
    public Dictionary<ItemDefinition, ItemStatisticModification> ItemStatisticModifications { get; set; } = new();
    public CombatConfig Combat { get; set; } = new();

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

    public class ItemStatisticModification
    {
        public int Damage { get; set; }
        [Range(0.0f, 20.0f)] public float Knockback { get; set; }
        public int Defense { get; set; }
    }

    public class CombatConfig
    {
        public int MeleeInvincibilityFrames { get; set; } = 8;
        public int RecentDamagePreservationFrames { get; set; } = 15 * 60;
    }

    public override bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref NetworkText message)
    {
        if (pendingConfig is AdventureConfig)
        {
            message = NetworkText.FromLiteral("You are not permitted to modify this config.");
            return false;
        }

        return true;
    }
}