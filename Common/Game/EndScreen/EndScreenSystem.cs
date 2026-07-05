using Microsoft.Xna.Framework;
using PvPAdventure.Common.Game.GameReporters;
using PvPAdventure.Common.Statistics;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace PvPAdventure.Common.Game.EndScreen;

/// <summary>Owns end screen lifetime and snapshot creation.</summary>
[Autoload(Side = ModSide.Both)]
public class EndScreenSystem : ModSystem
{
    private const int FadeInFrames = 24;
    public const int BackButtonDelayFrames = 360; // 6 seconds before manual close appears

    private EndScreenBackdropLayer backdropLayer;
    private EndScreenLayer layer;

    public EndScreenSnapshot CurrentSnapshot;
    public int AgeFrames;
    public int PresentationId;

    // Kept after the screen hides so /gamesummary can bring it back.
    public EndScreenSnapshot LastSnapshot;
    // When re-opened manually (command) we ignore the "match restarted" / team auto-hide.
    private bool forcedView;

    public bool IsVisible => CurrentSnapshot != null;

    public float Opacity
    {
        get
        {
            if (!IsVisible)
                return 0f;

            return MathHelper.Clamp(AgeFrames / (float)FadeInFrames, 0f, 1f);
        }
    }

    public override void Load()
    {
        if (!Main.dedServ)
        {
            backdropLayer = new EndScreenBackdropLayer(this);
            layer = new EndScreenLayer(this);
        }
    }

    public override void ClearWorld()
    {
        Hide();
        LastSnapshot = null;
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        if (backdropLayer == null || layer == null)
            return;

        layers.Remove(backdropLayer);
        layers.Remove(layer);

        int inventoryIndex = layers.FindIndex(l => l.Name == "Vanilla: Inventory");
        if (inventoryIndex >= 0)
        {
            layers.Insert(inventoryIndex, backdropLayer); // blur world, then sharp stars
            layers.Insert(inventoryIndex + 1, layer); // scoreboards and vanilla UI draw above this
        }
    }

    public override void UpdateUI(GameTime gameTime)
    {
        if (!IsVisible)
            return;

        // A manual /gamesummary view persists through an active match and team changes.
        if (!forcedView)
        {
            GameManager gameManager = ModContent.GetInstance<GameManager>();
            bool matchRestarted = gameManager._startGameCountdown.HasValue || (AgeFrames > 60 && gameManager.CurrentPhase == GameManager.Phase.Playing);
            bool wrongTeam = (Team)Main.LocalPlayer.team != CurrentSnapshot.Team;

            if (matchRestarted || wrongTeam)
            {
                Hide();
                return;
            }
        }

        EndScreenStarSystem.UpdateStars();

        AgeFrames++;
    }

    public void ShowSnapshot(EndScreenSnapshot snapshot)
    {
        if (Main.dedServ || snapshot == null || Main.LocalPlayer == null)
            return;

        if ((Team)Main.LocalPlayer.team != snapshot.Team)
            return; // only show local player's team

        CurrentSnapshot = snapshot;
        LastSnapshot = snapshot;
        AgeFrames = 0;
        PresentationId++;
        forcedView = false;
        PlayOpenSound(snapshot);
    }

    /// <summary>Re-opens the most recent summary on demand (used by /gamesummary).</summary>
    public bool ReshowLastSummary()
    {
        if (Main.dedServ || LastSnapshot == null)
            return false;

        CurrentSnapshot = LastSnapshot;
        AgeFrames = 0;
        PresentationId++;
        forcedView = true;
        PlayOpenSound(LastSnapshot);
        return true;
    }

    public void Hide()
    {
        CurrentSnapshot = null;
        AgeFrames = 0;
        forcedView = false;
    }

    private static void PlayOpenSound(EndScreenSnapshot snapshot)
    {
        switch (snapshot.Result)
        {
            case EndScreenResult.Victory:
                SoundEngine.PlaySound(SoundID.DD2_WinScene.WithVolume(0.45f));
                break;
            case EndScreenResult.Defeat:
                SoundEngine.PlaySound(SoundID.DD2_DefeatScene.WithVolume(0.35f));
                break;
            default:
                SoundEngine.PlaySound(SoundID.MenuOpen with { Volume = 0.65f });
                break;
        }
    }

    public static void SendMatchEndSnapshots()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        Team[] teamsWithPlayers = Main.player
            .Where(p => p?.active == true && (Team)p.team != Team.None)
            .Select(p => (Team)p.team)
            .Distinct()
            .ToArray();

        Team[] resultTeams = System.Enum.GetValues<Team>().Where(t => t != Team.None).ToArray();

        foreach (Team team in teamsWithPlayers)
            SendTeamSnapshot(team, teamsWithPlayers, resultTeams);
    }

    private static void SendTeamSnapshot(Team team, Team[] scoreTeams, Team[] resultTeams)
    {
        EndScreenSnapshot snapshot = BuildSnapshot(team, scoreTeams, resultTeams);
        if (snapshot.Players.Count == 0)
            return;

        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            snapshot.LocalPlayerReward = GetPlayerReward(Main.LocalPlayer, ModContent.GetInstance<PointsManager>());
            ModContent.GetInstance<EndScreenSystem>().ShowSnapshot(snapshot);
            return;
        }

        foreach (Player player in Main.player)
        {
            if (player?.active == true && (Team)player.team == team)
            {
                snapshot.LocalPlayerReward = GetPlayerReward(player, ModContent.GetInstance<PointsManager>());
                EndScreenNetHandler.SendSnapshot(snapshot, player.whoAmI);
            }
        }
    }

    private static EndScreenSnapshot BuildSnapshot(Team team, Team[] scoreTeams, Team[] resultTeams)
    {
        PointsManager pointsManager = ModContent.GetInstance<PointsManager>();
        int TeamScore(Team t) => pointsManager.Points.TryGetValue(t, out int value) ? value : 0;

        int teamScore = TeamScore(team);
        int bestScore = resultTeams.DefaultIfEmpty(team).Max(TeamScore);
        int opponentScore = resultTeams.Where(t => t != team).DefaultIfEmpty(Team.None).Max(TeamScore);

        EndScreenSnapshot snapshot = new()
        {
            Team = team,
            TeamScore = teamScore,
            OpponentScore = opponentScore,
            Result = GetResult(teamScore, bestScore, resultTeams.Count(t => TeamScore(t) == bestScore))
        };

        // Same team filter/order as the small Scoreline: only teams with active players.
        foreach (Team t in System.Enum.GetValues<Team>())
            if (scoreTeams.Contains(t))
                snapshot.AllScores.Add(new TeamScoreEntry(t, TeamScore(t)));

        foreach (Team t in scoreTeams)
            snapshot.Players.AddRange(AssignRoles(GetTeamPlayers(t).ToList()));

        return snapshot;
    }

    private static uint GetPlayerReward(Player player, PointsManager pointsManager)
    {
        if (player == null || !player.active)
            return 0u;

        MatchRewardContext rewardContext = MatchRewardCalculator.CreateContext(player, pointsManager);
        return MatchRewardCalculator.Calculate(rewardContext);
    }

    private static IEnumerable<EndScreenPlayerStats> GetTeamPlayers(Team team)
    {
        return Main.player
            .Where(p => p?.active == true && (Team)p.team == team)
            .Select(CreatePlayerStats)
            .OrderByDescending(p => p.Kills)
            .ThenBy(p => p.Deaths)
            .ThenByDescending(p => p.DamageDealt);
    }

    private static EndScreenResult GetResult(int teamScore, int bestScore, int bestTeamCount)
    {
        if (teamScore != bestScore)
            return EndScreenResult.Defeat;

        return bestTeamCount > 1 ? EndScreenResult.Tie : EndScreenResult.Victory;
    }

    private static EndScreenPlayerStats CreatePlayerStats(Player player)
    {
        StatisticsPlayer statistics = player.GetModPlayer<StatisticsPlayer>();
        Dictionary<string, uint> matchStats = StatsReporter.CopyStats(player);
        Dictionary<string, IDictionary<int, uint>> itemStats = StatsReporter.CopyItemStats(player);
        uint Stat(string key) => matchStats.TryGetValue(key, out uint value) ? value : 0u;

        return new EndScreenPlayerStats(
            (byte)player.whoAmI,
            (Team)player.team,
            player.name,
            statistics.Kills,
            statistics.Deaths,
            Stat(StatsReporter.DamageDealt),
            Stat(StatsReporter.DamageTaken),
            Stat(StatsReporter.TilesMined),
            Stat(StatsReporter.TilesPlaced),
            Stat(StatsReporter.ConsumablesUsed),
            Stat(StatsReporter.LavaDeaths),
            Stat(StatsReporter.FoodEaten),
            Stat(StatsReporter.BossDamageDealt),
            Stat(StatsReporter.PortalKills),
            CountDifferentWeapons(itemStats),
            Stat(StatsReporter.LostHoney));
    }

    private static List<EndScreenPlayerStats> AssignRoles(List<EndScreenPlayerStats> players)
    {
        Dictionary<byte, (string Title, string Value)> roles = [];

        AwardHighest(players, roles, p => p.BossDamageDealt, "Boss Breaker", p => $"{Short(p.BossDamageDealt)} boss dmg");
        AwardHighest(players, roles, p => p.PortalKills, "Portal Breaker", p => $"{p.PortalKills} {Plural(p.PortalKills, "portal")}");
        AwardHighest(players, roles, p => p.DifferentWeaponsUsed, "The Arsenal", p => $"{p.DifferentWeaponsUsed} {Plural(p.DifferentWeaponsUsed, "weapon")}");
        AwardHighest(players, roles, p => p.LavaDeaths, "Lava Magnet", p => $"{p.LavaDeaths} lava {Plural(p.LavaDeaths, "death")}");
        AwardHighest(players, roles, p => p.FoodEaten, "Feastmaster", p => $"{p.FoodEaten} food eaten");
        AwardHighest(players, roles, p => p.LostHoney, "Honey Spiller", p => $"{p.LostHoney} honey lost");
        AwardFewestDeaths(players, roles);

        return players
            .Select(p => roles.TryGetValue(p.PlayerIndex, out var role) ? p with { RoleTitle = role.Title, RoleValue = role.Value } : p with { RoleTitle = "Adventurer", RoleValue = "Ready for more" })
            .ToList();
    }

    private static void AwardHighest(List<EndScreenPlayerStats> players, Dictionary<byte, (string Title, string Value)> roles, System.Func<EndScreenPlayerStats, uint> value, string title, System.Func<EndScreenPlayerStats, string> text)
    {
        EndScreenPlayerStats winner = players.Where(p => !roles.ContainsKey(p.PlayerIndex) && value(p) > 0).OrderByDescending(value).ThenByDescending(p => p.Kills).ThenBy(p => p.Deaths).FirstOrDefault();
        if (winner != null)
            roles[winner.PlayerIndex] = (title, text(winner));
    }

    private static void AwardFewestDeaths(List<EndScreenPlayerStats> players, Dictionary<byte, (string Title, string Value)> roles)
    {
        EndScreenPlayerStats winner = players.Where(p => !roles.ContainsKey(p.PlayerIndex)).OrderBy(p => p.Deaths).ThenByDescending(p => p.Kills).ThenByDescending(p => p.DamageDealt).FirstOrDefault();
        if (winner != null)
            roles[winner.PlayerIndex] = ("Survivor", $"{winner.Deaths} {Plural((uint)winner.Deaths, "death")}");
    }

    private static uint CountDifferentWeapons(Dictionary<string, IDictionary<int, uint>> itemStats)
    {
        HashSet<int> weapons = [];
        AddWeapons(itemStats, StatsReporter.DamageDealt, weapons);
        AddWeapons(itemStats, StatsReporter.BossDamageDealt, weapons);
        return (uint)weapons.Count;
    }

    private static void AddWeapons(Dictionary<string, IDictionary<int, uint>> itemStats, string statKey, HashSet<int> weapons)
    {
        if (!itemStats.TryGetValue(statKey, out IDictionary<int, uint> byItem))
            return;

        foreach ((int itemId, uint amount) in byItem)
            if (amount > 0 && itemId > ItemID.None && itemId < ItemLoader.ItemCount)
                weapons.Add(itemId);
    }

    private static string Short(uint value) => value >= 1000 ? $"{value / 1000f:0.0}k" : value.ToString();

    private static string Plural(uint value, string word) => value == 1 ? word : word + "s";
}

/// <summary>Freezes local player movement/actions while the end screen is open.</summary>
public class EndScreenInputBlocker : ModPlayer
{
    public override void SetControls()
    {
        if (!ModContent.GetInstance<EndScreenSystem>().IsVisible)
            return;

        Player.controlLeft = false;
        Player.controlRight = false;
        Player.controlUp = false;
        Player.controlDown = false;
        Player.controlJump = false;
        Player.controlMount = false;
        Player.controlHook = false;
        Player.controlUseItem = false;
        Player.controlUseTile = false;
        Player.controlThrow = false;
    }
}
