using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Humanizer;
using Humanizer.Localisation;
using Terraria;
using Terraria.Enums;
using Terraria.ID;
using Microsoft.Xna.Framework;
using Terraria.Chat;
using Terraria.GameContent.Creative;
using Terraria.GameContent.Events;
using Terraria.GameContent.NetModules;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Net;

namespace PvPAdventure.System;

[Autoload(Side = ModSide.Both)]
public class GameManager : ModSystem
{
    public int TimeRemaining { get; set; }
    public int? _startGameCountdown = null;
    private Phase _currentPhase;

    private static readonly FieldInfo _bigProgressBarSystemCurrentBarField =
        typeof(BigProgressBarSystem).GetField("_currentBar", BindingFlags.NonPublic | BindingFlags.Instance);

    public Phase CurrentPhase
    {
        get => _currentPhase;
        set
        {
            if (_currentPhase == value)
                return;

            if (Main.netMode != NetmodeID.MultiplayerClient)
                OnPhaseChange(value);

            _currentPhase = value;

            if (Main.dedServ)
                NetMessage.SendData(MessageID.WorldData);
        }
    }

    public enum Phase
    {
        Waiting,
        Playing,
    }

    public override void Load()
    {
        // Prevent the world from entering the lunar apocalypse (killing cultist and spawning pillars)
        On_WorldGen.TriggerLunarApocalypse += _ => { };
        // Prevent tombstones.
        On_Player.DropTombstone += (_, _, _, _, _) => { };

        On_Main.StartInvasion += OnMainStartInvasion;

        if (Main.dedServ)
            // Only send world map pings to teammates.
            On_NetPingModule.Deserialize += OnNetPingModuleDeserialize;

        // Broadcast a message when rain starts.
        On_Main.StartRain += OnMainStartRain;

        // Broadcast a message when a sandstorm starts.
        On_Sandstorm.StartSandstorm += OnSandstormStartSandstorm;
    }

    private void OnMainStartInvasion(On_Main.orig_StartInvasion orig, int type)
    {
        orig(type);

        if (Main.invasionType == InvasionID.None)
            return;

        var adventureConfig = ModContent.GetInstance<AdventureConfig>();
        if (!adventureConfig.InvasionSizes.TryGetValue(type, out var invasionSize))
            return;

        // We shouldn't increase the invasion size, only ever decrease it.
        if (Main.invasionSize > invasionSize.Value)
        {
            Mod.Logger.Info($"Reducing invasion {type} size from {Main.invasionSize} to {invasionSize}");
            Main.invasionSize = Main.invasionSizeStart = Main.invasionProgressMax = invasionSize.Value;
        }
    }

    // NOTE: This should only ever be applied to the server.
    private bool OnNetPingModuleDeserialize(On_NetPingModule.orig_Deserialize orig, NetPingModule self,
        BinaryReader reader, int userid)
    {
        var position = reader.ReadVector2();
        var packet = NetPingModule.Serialize(position);

        var senderTeam = (Team)Main.player[userid].team;

        foreach (var client in Netplay.Clients)
        {
            if (!client.IsActive)
                continue;

            var player = Main.player[client.Id];
            if (!player.active || player.team == (int)Team.None || player.team == (int)senderTeam)
                NetManager.Instance.SendToClient(packet, client.Id);
        }

        return true;
    }

    private void OnSandstormStartSandstorm(On_Sandstorm.orig_StartSandstorm orig)
    {
        orig();
        ChatHelper.BroadcastChatMessage(NetworkText.FromKey("Mods.PvPAdventure.Sandstorm"), Color.White);
    }

    private void OnMainStartRain(On_Main.orig_StartRain orig)
    {
        orig();
        ChatHelper.BroadcastChatMessage(NetworkText.FromKey("Mods.PvPAdventure.Rain"), Color.White);
    }
    public override void PostUpdateTime()
    {
        // The Nurse is never allowed to spawn.
        Main.townNPCCanSpawn[NPCID.Nurse] = false;

        switch (CurrentPhase)
        {
            case Phase.Waiting:
            {
                if (Main.netMode == NetmodeID.MultiplayerClient)
                    break;

                if (_startGameCountdown.HasValue)
                {
                    if (--_startGameCountdown <= 0)
                    {
                        _startGameCountdown = null;
                        CurrentPhase = Phase.Playing;
                    }
                    else if (_startGameCountdown <= (60 * 3) && _startGameCountdown % 60 == 0)
                    {
                        ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral($"{_startGameCountdown / 60}..."),
                            Color.Crimson);
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
    public void StartGame(int time, int countdownTimeInSeconds = 10)
    {
        CurrentPhase = Phase.Waiting;
        TimeRemaining = time;
        _startGameCountdown = 60 * countdownTimeInSeconds;

        ChatHelper.BroadcastChatMessage(
            NetworkText.FromLiteral($"The game will begin in {_startGameCountdown / 60} seconds."), Color.Green);
    }

    // NOTE: This is not called on multiplayer clients (see CurrentPhase property).
    private void OnPhaseChange(Phase newPhase)
    {
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

                var spawnPosition = new Vector2(Main.spawnTileX, Main.spawnTileY - 3).ToWorldCoordinates();
                foreach (var player in Main.ActivePlayers)
                {
                    player.Teleport(spawnPosition, TeleportationStyleID.RecallPotion);
                    // FIXME: I think this is right-ish?

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
        OnPhaseChange(Phase.Waiting);
    }

    public override void ClearWorld()
    {
        _startGameCountdown = null;
        TimeRemaining = 0;

        // Always run the on-change regardless of if it actually changes.
        if (Main.netMode != NetmodeID.MultiplayerClient && CurrentPhase != Phase.Waiting)
            OnPhaseChange(Phase.Waiting);
        CurrentPhase = Phase.Waiting;

        // Terraria/TML bug: Remove boss bar when clearing the world
        // FIXME: Don't put this here!
        _bigProgressBarSystemCurrentBarField.SetValue(Main.BigBossProgressBar, null);

        CurrentPhase = Phase.Waiting;
    }

    public override void NetSend(BinaryWriter writer)
    {
        writer.Write(TimeRemaining);
        writer.Write((int)CurrentPhase);
    }

    public override void NetReceive(BinaryReader reader)
    {
        TimeRemaining = reader.ReadInt32();
        CurrentPhase = (Phase)reader.ReadInt32();
    }

    public class TeamCommand : ModCommand
    {
        public override void Action(CommandCaller caller, string input, string[] args)
        {
            // You can only use this command from chat in singleplayer.
            if (caller.CommandType == CommandType.Chat && Main.netMode != NetmodeID.SinglePlayer)
                return;

            if (args.Length < 2)
                return;

            var player = Main.player
                .Where(player => player.active)
                .Where(player => player.name.Contains(args[0], StringComparison.CurrentCultureIgnoreCase))
                .FirstOrDefault();

            if (player == null)
                return;

            if (!Enum.TryParse(args[1], true, out Team team) || (int)team >= Enum.GetValues<Team>().Length)
                return;

            player.team = (int)team;
            NetMessage.SendData(MessageID.PlayerTeam, number: player.whoAmI);
        }

        public override string Command => "team";
        public override CommandType Type => CommandType.Chat | CommandType.Console;
    }

    public class StartGameCommand : ModCommand
    {
        public override void Action(CommandCaller caller, string input, string[] args)
        {
            // You can only use this command from chat in singleplayer.
            if (caller.CommandType == CommandType.Chat && Main.netMode != NetmodeID.SinglePlayer)
                return;

            if (args.Length == 0 || !int.TryParse(args[0], out var time))
            {
                caller.Reply("Invalid time.", Color.Red);
                return;
            }

            var gameManager = ModContent.GetInstance<GameManager>();
            if (gameManager.CurrentPhase == Phase.Playing)
            {
                caller.Reply("The game is already being played.", Color.Red);
                return;
            }

            if (gameManager._startGameCountdown.HasValue)
            {
                caller.Reply("The game is already being started.", Color.Red);
                return;
            }

            gameManager.StartGame(time);
        }

        public override string Command => "startgame";
        public override CommandType Type => CommandType.Chat | CommandType.Console;
    }

    public class TimeLeftCommand : ModCommand
    {
        public override void Action(CommandCaller caller, string input, string[] args)
        {
            var gameManager = ModContent.GetInstance<GameManager>();

            if (gameManager == null)
                return;

            if (gameManager.CurrentPhase != Phase.Playing)
                return;

            caller.Reply(
                $"{TimeSpan.FromSeconds(gameManager.TimeRemaining / 60.0).Humanize(2, minUnit: TimeUnit.Second)} remain",
                Color.Green);
        }

        public override string Command => "timeleft";
        public override CommandType Type => CommandType.World;
    }

    public class CrashoutCommand : ModCommand
    {
        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
                return;

            var crashoutMessages = ModContent.GetInstance<AdventureConfig>().CrashoutMessages;
            var message = crashoutMessages[Main.rand.Next(0, crashoutMessages.Count)];

            ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral($"{caller.Player.name} crashed out: {message}"),
                Color.Red, caller.Player.whoAmI);
            NetMessage.SendData(MessageID.Kick, caller.Player.whoAmI, text: NetworkText.FromLiteral(message));
        }

        public override string Command => "crashout";
        public override CommandType Type => CommandType.World;
    }
}