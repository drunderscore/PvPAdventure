using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Common.Statistics;
using PvPAdventure.Common.Teams;
using PvPAdventure.Core.Utilities;
using PvPAdventure.UI;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaTeam = Terraria.Enums.Team;

namespace PvPAdventure.Common.Spectator.UI.World;

internal static class WorldStatsHelper
{
    internal readonly record struct BossEntry(int NpcId, string Name, bool Downed);
    internal readonly record struct EventEntry(int ItemId, string Name, bool Downed);

    public static string GetTimeText()
    {
        double time = Main.time;
        if (!Main.dayTime)
            time += 54000.0;

        time = time / 86400.0 * 24.0;
        time = (time + 7.5) % 24.0;

        int hours = (int)time;
        int minutes = (int)((time - hours) * 60.0);
        return $"{hours:00}:{minutes:00} {(Main.dayTime ? "Day" : "Night")}";
    }

    public static string GetWeatherText()
    {
        if (Sandstorm.Happening)
            return "Sandstorm";
        if (Main.raining)
            return $"Rain {Main.maxRaining:P0}";
        if (Main.eclipse)
            return "Solar Eclipse";
        if (Main.bloodMoon)
            return "Blood Moon";

        return "Clear";
    }

    public static string GetWorldSizeText()
    {
        if (Main.maxTilesX <= 4200)
            return "Small";
        if (Main.maxTilesX <= 6400)
            return "Medium";
        if (Main.maxTilesX <= 8400)
            return "Large";

        return "Custom";
    }

    public static string GetSeedText()
    {
        return Main.ActiveWorldFileData?.SeedText ?? "TBD";
    }

    public static string GetEvilText()
    {
        return WorldGen.crimson ? "Crimson" : "Corruption";
    }

    public static Color GetEvilColor()
    {
        return WorldGen.crimson ? new Color(255, 120, 120) : new Color(170, 120, 255);
    }

    public static string GetMoonText()
    {
        return $"{Main.moonPhase + 1}/8";
    }

    public static string GetWindText()
    {
        if (Math.Abs(Main.windSpeedTarget) < 0.01f)
            return "Calm";

        string direction = Main.windSpeedTarget > 0f ? "East" : "West";
        return $"{direction} {Math.Abs(Main.windSpeedTarget):0.00}";
    }

    public static string GetSurfaceText()
    {
        return FormatFeet(Main.worldSurface);
    }

    public static string GetCavernText()
    {
        return FormatFeet(Main.rockLayer);
    }

    public static int GetEffectiveDifficultyMode()
    {
        int modeNumber = Main.GameMode;
        if (Main.getGoodWorld && modeNumber > 0)
            modeNumber++;

        return modeNumber;
    }

    public static string GetDifficultyText()
    {
        int mode = GetEffectiveDifficultyMode();

        if (mode == 0)
            return "Classic";
        if (mode == 1)
            return "Expert";
        if (mode == 2)
            return "Master";
        if (mode == 3)
            return "Journey";
        if (mode == 4)
            return "Legendary";

        return $"Mode {mode}";
    }

    public static Color GetDifficultyColor()
    {
        int mode = GetEffectiveDifficultyMode();

        if (mode == 1)
            return Main.mcColor;
        if (mode == 2)
            return Main.hcColor;
        if (mode == 3)
            return Main.creativeModeColor;
        if (mode == 4 && typeof(Main).GetField("legendaryModeColor")?.GetValue(null) is Color legendaryColor)
            return legendaryColor;
        if (mode == 4)
            return Main.hcColor;

        return Color.White;
    }

    public static Asset<Texture2D> TryGetWorldIconAsset()
    {
        try
        {
            string path = "Images/UI/Icon" + (Main.hardMode ? "Hallow" : string.Empty) + (WorldGen.crimson ? "Crimson" : "Corruption");
            return Main.Assets.Request<Texture2D>(path, AssetRequestMode.ImmediateLoad);
        }
        catch
        {
            return null;
        }
    }

    public static string GetWorldInfectionText()
    {
        return $"{WorldGen.tEvil + WorldGen.tBlood}%";
    }

    public static string FormatFeet(double value)
    {
        return $"{value * 16f:0}'";
    }

    public static int CountActiveBosses()
    {
        int count = 0;

        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC npc = Main.npc[i];
            if (npc?.active == true && npc.boss)
                count++;
        }

        return count;
    }

    public static BossEntry[] GetBossEntries()
    {
        int evilBossId;
        string evilBossName;

        if (WorldGen.crimson)
        {
            evilBossId = NPCID.BrainofCthulhu;
            evilBossName = "Brain of Cthulhu";
        }
        else
        {
            evilBossId = NPCID.EaterofWorldsHead;
            evilBossName = "Eater of Worlds";
        }

        return
        [
            new BossEntry(NPCID.KingSlime, "King Slime", NPC.downedSlimeKing),
            new BossEntry(NPCID.EyeofCthulhu, "Eye of Cthulhu", NPC.downedBoss1),
            new BossEntry(evilBossId, evilBossName, NPC.downedBoss2),
            new BossEntry(NPCID.QueenBee, "Queen Bee", NPC.downedQueenBee),
            new BossEntry(NPCID.SkeletronHead, "Skeletron", NPC.downedBoss3),
            new BossEntry(NPCID.WallofFlesh, "Wall of Flesh", Main.hardMode),
            new BossEntry(NPCID.QueenSlimeBoss, "Queen Slime", NPC.downedQueenSlime),
            new BossEntry(NPCID.TheDestroyer, "The Destroyer", NPC.downedMechBoss1),
            new BossEntry(NPCID.Retinazer, "The Twins", NPC.downedMechBoss2),
            new BossEntry(NPCID.SkeletronPrime, "Skeletron Prime", NPC.downedMechBoss3),
            new BossEntry(NPCID.Plantera, "Plantera", NPC.downedPlantBoss),
            new BossEntry(NPCID.Golem, "Golem", NPC.downedGolemBoss),
            new BossEntry(NPCID.DukeFishron, "Duke Fishron", NPC.downedFishron),
            new BossEntry(NPCID.CultistBoss, "Lunatic Cultist", NPC.downedAncientCultist),
            new BossEntry(NPCID.MoonLordCore, "Moon Lord", NPC.downedMoonlord)
        ];
    }

    public static int GetBossHeadNpcId(int npcId)
    {
        return npcId == NPCID.Golem ? NPCID.GolemHead : npcId;
    }

    public static string GetActiveInvasionText()
    {
        if (DD2Event.Ongoing)
            return "Old One's Army";
        if (Main.invasionType == InvasionID.GoblinArmy)
            return "Goblin Army";
        if (Main.invasionType == InvasionID.SnowLegion)
            return "Frost Legion";
        if (Main.invasionType == InvasionID.PirateInvasion)
            return "Pirate Invasion";
        if (Main.invasionType == InvasionID.MartianMadness)
            return "Martian Madness";

        return "None";
    }

    public static EventEntry[] GetEventEntries()
    {
        return
        [
            new EventEntry(ItemID.GoblinBattleStandard, "Goblin Army", NPC.downedGoblins),
            new EventEntry(ItemID.SnowGlobe, "Frost Legion", NPC.downedFrost),
            new EventEntry(ItemID.PirateMap, "Pirate Invasion", NPC.downedPirates),
            new EventEntry(ItemID.MartianConduitWall, "Martian Madness", NPC.downedMartians),
            new EventEntry(ItemID.PumpkinMoonMedallion, "Pumpkin Moon", NPC.downedHalloweenKing || NPC.downedHalloweenTree),
            new EventEntry(ItemID.NaughtyPresent, "Frost Moon", NPC.downedChristmasIceQueen || NPC.downedChristmasSantank || NPC.downedChristmasTree),
            new EventEntry(ItemID.DD2ElderCrystal, "Old One's Army", DD2Event.DownedInvasionT1 || DD2Event.DownedInvasionT2 || DD2Event.DownedInvasionT3),
            new EventEntry(ItemID.BloodMoonStarter, "Blood Moon", Main.bloodMoon),
            new EventEntry(ItemID.SolarTablet, "Solar Eclipse", Main.eclipse)
        ];
    }

    public static int CountActivePlayers()
    {
        int count = 0;

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player player = Main.player[i];
            if (player?.active == true)
                count++;
        }

        return count;
    }

    public static void GetPlayerSummary(out int totalKills, out int totalDeaths, out string topFragger)
    {
        totalKills = 0;
        totalDeaths = 0;
        topFragger = "TBD";

        int bestKills = -1;
        int bestDeaths = int.MaxValue;
        float bestKd = float.MinValue;

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            Player player = Main.player[i];
            if (player == null || !player.active)
                continue;

            StatisticsPlayer stats = player.GetModPlayer<StatisticsPlayer>();
            totalKills += stats.Kills;
            totalDeaths += stats.Deaths;

            float kd = stats.Deaths <= 0 ? stats.Kills : stats.Kills / (float)stats.Deaths;
            bool better = stats.Kills > bestKills || stats.Kills == bestKills && stats.Deaths < bestDeaths || stats.Kills == bestKills && stats.Deaths == bestDeaths && kd > bestKd;
            if (!better)
                continue;

            bestKills = stats.Kills;
            bestDeaths = stats.Deaths;
            bestKd = kd;
            topFragger = $"{player.name} ({stats.Kills}/{stats.Deaths})";
        }
    }

    public static List<NPC> GetTownNpcs(int max = 16)
    {
        List<NPC> list = [];

        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC npc = Main.npc[i];
            if (npc == null || !npc.active || !npc.townNPC && !npc.isLikeATownNPC)
                continue;

            list.Add(npc);
        }

        return list.Take(max).ToList();
    }

    public static int CountHousedTownNpcs(List<NPC> townNpcs)
    {
        int count = 0;

        for (int i = 0; i < townNpcs.Count; i++)
        {
            if (!townNpcs[i].homeless)
                count++;
        }

        return count;
    }

    public static Item GetMostValuableItem()
    {
        Item mostValuable = null;

        for (int i = 0; i < Main.maxItems; i++)
        {
            Item item = Main.item[i];
            if (item == null || !item.active || item.IsAir)
                continue;

            if (mostValuable == null || item.value > mostValuable.value)
                mostValuable = item;
        }

        return mostValuable;
    }

    public static string GetBedsPerTeamText()
    {
        TeamBedSystem bedSystem = ModContent.GetInstance<TeamBedSystem>();
        var field = typeof(TeamBedSystem).GetField("bedTeams", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (field?.GetValue(bedSystem) is not Dictionary<Point, TerrariaTeam> beds || beds.Count == 0)
            return "None";

        return string.Join(" | ", beds.GroupBy(static x => x.Value).Where(static x => x.Key != TerrariaTeam.None).Select(static x => $"{x.Key}:{x.Count()}"));
    }

    public static void DrawColumnSeparator(SpriteBatch sb, Rectangle inner, int x, int topOffset = 0, int bottomOffset = 0)
    {
        sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(x, inner.Y + topOffset, 2, inner.Height - topOffset - bottomOffset), Color.White * 0.08f);
    }

    public static void DrawRowSeparator(SpriteBatch sb, int x, int y, int width)
    {
        sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(x, y, width, 2), Color.White * 0.08f);
    }

    public static void DrawWorldInfoBackground(SpriteBatch sb, Rectangle area)
    {
        if (Ass.BG_WorldInfo == null || area.Width <= 0 || area.Height <= 0)
            return;

        Texture2D texture = Ass.BG_WorldInfo.Value;
        if (texture == null)
            return;

        int sourceWidth = Math.Min(texture.Width, area.Width);
        int sourceHeight = Math.Min(texture.Height, area.Height);
        Rectangle source = new((texture.Width - sourceWidth) / 2, (texture.Height - sourceHeight) / 2, sourceWidth, sourceHeight);
        Rectangle destination = new(area.Right - sourceWidth, area.Y, sourceWidth, sourceHeight);

        sb.Draw(texture, destination, source, Color.White * 0.22f);

        int fadeWidth = Math.Min(140, destination.Width);
        for (int i = 0; i < fadeWidth; i++)
        {
            float progress = i / (float)Math.Max(1, fadeWidth - 1);
            Color fadeColor = Color.Black * (1f - progress) * 0.85f;
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(destination.X + i, destination.Y, 1, destination.Height), fadeColor);
        }
    }
}
