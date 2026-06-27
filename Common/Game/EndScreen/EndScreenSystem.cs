using Microsoft.Xna.Framework;
using PvPAdventure.Common.Game.GameReporters;
using PvPAdventure.Common.Statistics;
using System.Collections.Generic;
using System.Linq;
using Terraria;
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
            bool matchRestarted = gameManager.CurrentPhase == GameManager.Phase.Playing || gameManager._startGameCountdown.HasValue;
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
        forcedView = false;
    }

    /// <summary>Re-opens the most recent summary on demand (used by /gamesummary).</summary>
    public bool ReshowLastSummary()
    {
        if (Main.dedServ || LastSnapshot == null)
            return false;

        CurrentSnapshot = LastSnapshot;
        AgeFrames = 0;
        forcedView = true;
        return true;
    }

    public void Hide()
    {
        CurrentSnapshot = null;
        AgeFrames = 0;
        forcedView = false;
    }

    public static void SendMatchEndSnapshots()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        Team[] teams = Main.player
            .Where(p => p?.active == true && (Team)p.team != Team.None)
            .Select(p => (Team)p.team)
            .Distinct()
            .ToArray();

        foreach (Team team in teams)
            SendTeamSnapshot(team, teams);
    }

    private static void SendTeamSnapshot(Team team, Team[] teams)
    {
        EndScreenSnapshot snapshot = BuildSnapshot(team, teams);
        if (snapshot.Players.Count == 0)
            return;

        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            ModContent.GetInstance<EndScreenSystem>().ShowSnapshot(snapshot);
            return;
        }

        foreach (Player player in Main.player)
        {
            if (player?.active == true && (Team)player.team == team)
                EndScreenNetHandler.SendSnapshot(snapshot, player.whoAmI);
        }
    }

    private static EndScreenSnapshot BuildSnapshot(Team team, Team[] teams)
    {
        PointsManager pointsManager = ModContent.GetInstance<PointsManager>();
        int TeamScore(Team t) => pointsManager.Points.TryGetValue(t, out int value) ? value : 0;

        int teamScore = TeamScore(team);
        int bestScore = teams.DefaultIfEmpty(team).Max(TeamScore);
        int opponentScore = teams.Where(t => t != team).DefaultIfEmpty(Team.None).Max(TeamScore);

        EndScreenSnapshot snapshot = new()
        {
            Team = team,
            TeamScore = teamScore,
            OpponentScore = opponentScore,
            Result = GetResult(teamScore, bestScore, teams.Count(t => TeamScore(t) == bestScore))
        };

        // Every team's points, even teams with no players, in team order (e.g. 7-5-5-5).
        foreach (Team t in System.Enum.GetValues<Team>())
            if (t != Team.None)
                snapshot.AllScores.Add(new TeamScoreEntry(t, TeamScore(t)));

        snapshot.Players.AddRange(GetTeamPlayers(team, pointsManager));
        return snapshot;
    }

    private static IEnumerable<EndScreenPlayerStats> GetTeamPlayers(Team team, PointsManager pointsManager)
    {
        return Main.player
            .Where(p => p?.active == true && (Team)p.team == team)
            .Select(p => CreatePlayerStats(p, pointsManager))
            .OrderByDescending(p => p.Reward) // reward decides MVP first
            .ThenByDescending(p => p.Kills)
            .ThenBy(p => p.Deaths)
            .ThenByDescending(p => p.DamageDealt)
            .Take(5);
    }

    private static EndScreenResult GetResult(int teamScore, int bestScore, int bestTeamCount)
    {
        if (teamScore != bestScore)
            return EndScreenResult.Defeat;

        return bestTeamCount > 1 ? EndScreenResult.Tie : EndScreenResult.Victory;
    }

    private static EndScreenPlayerStats CreatePlayerStats(Player player, PointsManager pointsManager)
    {
        StatisticsPlayer statistics = player.GetModPlayer<StatisticsPlayer>();
        Dictionary<string, uint> matchStats = StatsReporter.CopyStats(player);
        uint Stat(string key) => matchStats.TryGetValue(key, out uint value) ? value : 0u;

        MatchRewardContext rewardContext = MatchRewardCalculator.CreateContext(player, pointsManager);
        uint reward = MatchRewardCalculator.Calculate(rewardContext);

        return new EndScreenPlayerStats(
            (byte)player.whoAmI,
            player.name,
            statistics.Kills,
            statistics.Deaths,
            Stat(StatsReporter.DamageDealt),
            Stat(StatsReporter.DamageTaken),
            Stat(StatsReporter.TilesMined),
            Stat(StatsReporter.TilesPlaced),
            Stat(StatsReporter.ConsumablesUsed),
            reward);
    }
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
