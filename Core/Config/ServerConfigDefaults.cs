using System.Collections.Generic;
using Terraria.ID;
using Terraria.ModLoader.Config;

namespace PvPAdventure.Core.Config;

// Defaults imported from PvPAdventure_ServerConfig.json. Only values represented by this mod's ServerConfig are included.
internal static class ServerConfigDefaults
{
    internal const float BoundSpawnChance = 0.5f;
    internal const int PlayerKillPoints = 1;
    internal const int TeamStartingPoints = 12;
    internal const bool AwardBountyEveryKill = true;

    internal const bool IsRandomTeleportEnabled = false;
    internal const int TravelPortalCreationTimePreHardmodeSeconds = 10;
    internal const int TeleportCooldownSeconds = 0;

    internal const int LifeFruitChanceDenominator = 1;
    internal const int LifeFruitExpertChanceDenominator = 1;
    internal const int LifeFruitMinimumDistanceBetween = 1;
    internal const int PlanteraBulbChanceDenominator = 12;
    internal const int ChlorophyteSpreadChanceModifier = 100;
    internal const int ChlorophyteGrowChanceModifier = 70;
    internal const int ChlorophyteGrowLimitModifier = 60000;

    internal static Dictionary<NPCDefinition, ServerConfig.PointsConfig.NpcPoints> CreateNpcPoints() => new()
    {
        [Npc("Terraria/CultistBoss")] = new() { First = 10, Additional = 10, Repeatable = true },
        [Npc("Terraria/Plantera")] = new() { First = 3, Additional = 3, Repeatable = true },
    };

    internal static List<ServerConfig.BountiesConfig.Bounty> CreateClaimableBounties() =>
    [
        new()
        {
            Items = [ConfigItem("Terraria/GuideVoodooDoll")],
            Conditions = new()
            {
                WorldProgression = ServerConfig.Condition.WorldProgressionState.Hardmode,
                SkeletronPrimeDefeated = true,
                TwinsDefeated = true,
                DestroyerDefeated = true,
                PlanteraDefeated = true,
            }
        },
        new()
        {
            Items =
            [
                ConfigItem("Terraria/VileMushroom"),
                ConfigItem("Terraria/Lens", 2),
                ConfigItem("Terraria/Bone", 10),
                ConfigItem("Terraria/IronBar", 5),
            ],
            Conditions = new()
            {
                WorldProgression = ServerConfig.Condition.WorldProgressionState.Hardmode,
                SkeletronPrimeDefeated = true,
                TwinsDefeated = true,
                DestroyerDefeated = true,
            }
        },
        new()
        {
            Items = [ConfigItem("Terraria/SoulofLight", 2)],
            Conditions = new() { WorldProgression = ServerConfig.Condition.WorldProgressionState.Hardmode }
        },
        new()
        {
            Items = [ConfigItem("Terraria/SoulofNight", 2)],
            Conditions = new() { WorldProgression = ServerConfig.Condition.WorldProgressionState.Hardmode }
        },
    ];

    internal static Dictionary<int, ServerConfig.InvasionSizeValue> CreateInvasionSizes() => new()
    {
        [InvasionID.GoblinArmy] = new() { Value = 400 },
        [InvasionID.PirateInvasion] = new() { Value = 150 },
        [InvasionID.MartianMadness] = new() { Value = 500 },
    };

    private static ServerConfig.ConfigItem ConfigItem(string fullName, int stack = 1) => new()
    {
        Item = Item(fullName),
        Stack = stack
    };

    private static ItemDefinition Item(string fullName)
    {
        int separator = fullName.IndexOf('/');
        return new ItemDefinition(fullName[..separator], fullName[(separator + 1)..]);
    }

    private static NPCDefinition Npc(string fullName)
    {
        int separator = fullName.IndexOf('/');
        return new NPCDefinition(fullName[..separator], fullName[(separator + 1)..]);
    }
}
