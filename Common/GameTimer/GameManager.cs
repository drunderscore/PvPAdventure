using Microsoft.Xna.Framework;
using PvPAdventure.Common.MainMenu.MatchHistory;
using PvPAdventure.Common.Spawnbox;
using PvPAdventure.Common.Statistics;
using PvPAdventure.Core.Net;
using SubworldLibrary;
using System;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Chat;
using Terraria.Enums;
using Terraria.GameContent.Creative;
using Terraria.GameContent.NetModules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Net;

namespace PvPAdventure.Common.GameTimer;

[Autoload(Side = ModSide.Both)]
public class GameManager : ModSystem
{
    public int TimeRemaining { get; set; }
    public int? _startGameCountdown = null;
    private Phase _currentPhase;
    public DateTime? MatchStartTime { get; private set; }

    public Phase CurrentPhase
    {
        get => _currentPhase;
        set
        {
            if (_currentPhase == value)
                return;

            Phase oldPhase = _currentPhase;
            _currentPhase = value;

            if (Main.netMode != NetmodeID.MultiplayerClient)
                OnPhaseChange(oldPhase, value);

            if (Main.dedServ)
                NetMessage.SendData(MessageID.WorldData);
        }
    }

    public enum Phase
    {
        Waiting,
        Playing,
    }

    public override void PostUpdateTime()
    {
        switch (CurrentPhase)
        {
            case Phase.Waiting:
                {
                    if (Main.netMode == NetmodeID.MultiplayerClient)
                        break;

                    if (_startGameCountdown.HasValue)
                    {
                        _startGameCountdown--;

                        if (_startGameCountdown <= 0)
                        {
                            _startGameCountdown = null;
                            MatchStartTime = DateTime.UtcNow;
                            CurrentPhase = Phase.Playing;

                            if (Main.dedServ)
                                NetMessage.SendData(MessageID.WorldData); // sync phase + reset countdown
                        }
                        else
                        {
                            // Every second
                            if (_startGameCountdown % 60 == 0)
                            {
                                if (_startGameCountdown <= 60 * 3)
                                {
                                    ChatHelper.BroadcastChatMessage(
                                        NetworkText.FromLiteral($"{_startGameCountdown / 60}..."),
                                        Color.Crimson);
                                }

                                if (Main.dedServ)
                                    NetMessage.SendData(MessageID.WorldData); // sync current countdown
                            }
                        }
                    }

                    break;
                }

            case Phase.Playing:
                {
                    if (--TimeRemaining <= 0)
                    {
                        CurrentPhase = Phase.Waiting;
                    }

                    break;
                }
        }
    }

    private void ResetMatchState()
    {
        MatchStartTime = null;
    }

    public void StartGame(int time, int countdownTimeInSeconds = 10)
    {
        CurrentPhase = Phase.Waiting;
        TimeRemaining = time;
        _startGameCountdown = 60 * countdownTimeInSeconds;

        if (Main.dedServ)
            NetMessage.SendData(MessageID.WorldData);

        ChatHelper.BroadcastChatMessage(
            NetworkText.FromLiteral($"The game will begin in {_startGameCountdown / 60} seconds."), Color.Green);
    }
    public void EndGame()
    {
        _startGameCountdown = null;
        TimeRemaining = 0;
        CurrentPhase = Phase.Waiting;
    }

    public void AdjustTimeRemaining(int deltaFrames)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            return;
        }

        if (CurrentPhase != Phase.Playing)
        {
            return;
        }

        int oldFrames = TimeRemaining;

        int newValue = oldFrames + deltaFrames;
        if (newValue < 0)
        {
            newValue = 0;
        }

        TimeRemaining = newValue;

        if (TimeRemaining <= 0)
        {
            CurrentPhase = Phase.Waiting;
        }

        int appliedDeltaFrames = TimeRemaining - oldFrames;

        string fromText = FormatHHMMSSFromFrames(oldFrames);
        string toText = FormatHHMMSSFromFrames(TimeRemaining);
        string deltaText = FormatDeltaMMSSFromFrames(appliedDeltaFrames);

        string deltaHex = appliedDeltaFrames < 0 ? "FF0000" : (appliedDeltaFrames > 0 ? "00FF00" : "FFFFFF");
        string deltaTagged = $"[c/{deltaHex}:{deltaText}]";

        string msg = $"Game time adjusted from {fromText} to {toText} ({deltaTagged})";

        if (Main.dedServ)
        {
            NetMessage.SendData(MessageID.WorldData);
            ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(msg), Color.White);
        }
        else if (Main.netMode == NetmodeID.SinglePlayer)
        {
            ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(msg), Color.White);
        }
    }

    private static string FormatHHMMSSFromFrames(int frames)
    {
        if (frames < 0)
        {
            frames = 0;
        }

        const int FramesPerSecond = 60;
        int totalSeconds = frames / FramesPerSecond;

        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        int seconds = totalSeconds % 60;

        return $"{hours:00}:{minutes:00}:{seconds:00}";
    }

    private static string FormatDeltaMMSSFromFrames(int deltaFrames)
    {
        if (deltaFrames == 0)
        {
            return "+0:00";
        }

        string sign = deltaFrames > 0 ? "+" : "-";

        int absFrames = deltaFrames > 0 ? deltaFrames : -deltaFrames;
        const int FramesPerSecond = 60;
        int totalSeconds = absFrames / FramesPerSecond;

        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        return $"{sign}{minutes}:{seconds:00}";
    }

    private static void BroadcastEndGameSummary()
    {
        // If in subworld, dont broadcast.
        if (SubworldSystem.AnyActive())
            return;

        // Only the server (or singleplayer) should broadcast.
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            return;
        }

        var pm = ModContent.GetInstance<PointsManager>();

        for (int i = 0; i < 1; i++)
        {
            ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral("-----------------------------"), Color.White);
        }

        ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral("The game has ended!"), Color.White);

        var scoredTeams = pm.Points
            .Where(kvp => kvp.Key != Team.None)
            .Where(kvp => kvp.Value > 0)
            .OrderByDescending(kvp => kvp.Value)
            .ToList();

        if (scoredTeams.Count == 0)
        {
            ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral("No teams scored any points."), Color.White);
            return;
        }

        var maxPoints = scoredTeams[0].Value;
        var winningTeams = scoredTeams
            .Where(kvp => kvp.Value == maxPoints)
            .Select(kvp => kvp.Key)
            .ToList();

        if (winningTeams.Count == 1)
        {
            var team = winningTeams[0];

            ChatHelper.BroadcastChatMessage(
                NetworkText.FromLiteral($"{team} Team wins with {maxPoints} points!"),
                Main.teamColor[(int)team]);
        }
        else
        {
            // Multiple winners
            var winnersText = string.Join(", ", winningTeams.Select(t => $"{t} Team"));

            ChatHelper.BroadcastChatMessage(
                NetworkText.FromLiteral($"Tie! {winnersText} lead with {maxPoints} points."),
                Color.White);
        }

        int rank = 1;

        // Print team summary row, containing team points and MVP.
        foreach (var (team, points) in scoredTeams)
        {
            Player bestPlayer = null;
            var bestKills = -1;
            var bestDeaths = int.MaxValue;
            var bestKd = -1f;

            foreach (var player in Main.ActivePlayers)
            {
                if ((Team)player.team != team)
                {
                    continue;
                }

                var ap = player.GetModPlayer<StatisticsPlayer>();
                var kills = ap.Kills;
                var deaths = ap.Deaths;

                float kd = deaths <= 0 ? kills : kills / (float)deaths;

                var isBetter = false;

                if (kills > bestKills)
                {
                    isBetter = true;
                }
                else if (kills == bestKills)
                {
                    if (deaths < bestDeaths)
                    {
                        isBetter = true;
                    }
                    else if (deaths == bestDeaths && kd > bestKd)
                    {
                        isBetter = true;
                    }
                }

                if (isBetter)
                {
                    bestPlayer = player;
                    bestKills = kills;
                    bestDeaths = deaths;
                    bestKd = kd;
                }
            }

            string teamSummaryRow = $"{rank}. {team} Team: {points} points.";

            if (bestPlayer != null)
            {
                var bestAp = bestPlayer.GetModPlayer<StatisticsPlayer>();
                teamSummaryRow += $" MVP: {bestPlayer.name} (K/D: {bestAp.Kills}/{bestAp.Deaths})";
            }

            ChatHelper.BroadcastChatMessage(
                text: NetworkText.FromLiteral(teamSummaryRow),
                color: Main.teamColor[(int)team]);

            rank++;
        }
    }

    private static void ReportCompletedMatchToBackend()
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        GameManager gameManager = ModContent.GetInstance<GameManager>();
        if (!gameManager.MatchStartTime.HasValue)
            return;

        DateTime startUtc = DateTime.SpecifyKind(gameManager.MatchStartTime.Value, DateTimeKind.Utc);
        DateTime endUtc = DateTime.UtcNow;

        OfficialMatchReporter.PostCompletedMatch(startUtc, endUtc);

        Log.Chat("Queued completed match for backend reporting");
        Log.Debug("Queued completed match for backend reporting");
    }

    // NOTE: This is not called on multiplayer clients (see CurrentPhase property).
    private void OnPhaseChange(Phase oldPhase, Phase newPhase)
    {
        Log.Chat("New GamePhase: " + newPhase + ", (old: " + oldPhase + ")");

        // Only save when a real match ends (Playing → Waiting transition)
        if (oldPhase == Phase.Playing && newPhase == Phase.Waiting)
        {
            BroadcastEndGameSummary();
            //BroadcastSaveMatchToClients();
            ReportCompletedMatchToBackend();
            ResetMatchState(); // Clear the match start time after broadcasting
        }

        switch (newPhase)
        {
            case Phase.Waiting:
                {
                    var regions = ModContent.GetInstance<RegionManager>().Regions;
                    if (regions.Count >= 1)
                    {
                        // NOTE: We currently have one region, which is the spawn region. We'll use this assumption for now.
                        var spawnRegion = regions[0];
                        spawnRegion.CanRandomTeleport = false;
                        spawnRegion.CanUseWormhole = false;
                        spawnRegion.CanExit = false;
                    }

                    // Remove everything that is hostile
                    foreach (var npc in Main.ActiveNPCs)
                    {
                        if (npc.townNPC || npc.isLikeATownNPC || npc.type == NPCID.TargetDummy)
                            continue;

                        npc.life = 0;
                        npc.netSkip = -1;

                        if (Main.dedServ)
                            NetMessage.SendData(MessageID.SyncNPC, number: npc.whoAmI);
                    }

                    // Teleport all players to spawn
                    var spawnPosition = new Vector2(Main.spawnTileX, Main.spawnTileY - 3).ToWorldCoordinates();
                    foreach (var player in Main.ActivePlayers)
                    {
                        if (Main.dedServ)
                            NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 0, player.whoAmI, spawnPosition.X,
                                spawnPosition.Y, 2);
                    }

                    UpdateFreezeTime(true);

                    break;
                }
            case Phase.Playing:
                {
                    // NOTE: We currently have one region, which is the spawn region. We'll use this assumption for now.
                    var spawnRegion = ModContent.GetInstance<RegionManager>().Regions[0];
                    spawnRegion.CanRandomTeleport = true;
                    spawnRegion.CanUseWormhole = true;
                    spawnRegion.CanExit = true;

                    UpdateFreezeTime(false);

                    break;
                }
        }
    }
    private void UpdateFreezeTime(bool value)
    {
        var freezeTimeModule = CreativePowerManager.Instance.GetPower<CreativePowers.FreezeTime>();
        freezeTimeModule.SetPowerInfo(value);

        if (Main.dedServ)
        {
            var packet = NetCreativePowersModule.PreparePacket(freezeTimeModule.PowerId, 1);
            packet.Writer.Write(freezeTimeModule.Enabled);
            NetManager.Instance.Broadcast(packet);
        }
    }

    public override void OnWorldLoad()
    {
        OnPhaseChange(Phase.Waiting, Phase.Waiting);
    }

    public override void ClearWorld()
    {
        _startGameCountdown = null;
        TimeRemaining = 0;

        // Always run the on-change regardless of if it actually changes.
        if (Main.netMode != NetmodeID.MultiplayerClient && CurrentPhase != Phase.Waiting)
            OnPhaseChange(Phase.Waiting, Phase.Waiting);
        CurrentPhase = Phase.Waiting;

        CurrentPhase = Phase.Waiting;
    }

    public override void NetSend(BinaryWriter writer)
    {
        writer.Write(TimeRemaining);
        writer.Write((int)CurrentPhase);
        writer.Write(_startGameCountdown.HasValue);
        if (_startGameCountdown.HasValue)
            writer.Write(_startGameCountdown.Value);
    }

    public override void NetReceive(BinaryReader reader)
    {
        TimeRemaining = reader.ReadInt32();
        CurrentPhase = (Phase)reader.ReadInt32();
        if (reader.ReadBoolean())
            _startGameCountdown = reader.ReadInt32();
        else
            _startGameCountdown = null;
    }
}