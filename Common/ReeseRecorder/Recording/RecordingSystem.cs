using MonoMod.Cil;
using PvPAdventure.Common.ReeseRecorder.Replay;
using System;
using System.IO;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PvPAdventure.Common.ReeseRecorder.Recording;

[Autoload(Side = ModSide.Server)]
public class RecordingSystem : ModSystem, ITicker
{
    // FIXME: Become delegate
    public uint Ticks { get; private set; }
    private static RecordingMode _recordingMode = RecordingMode.NotRecording;
    public static RecordingMode GetRecordingMode() => _recordingMode;
    private static string _lastReplayPath;

    public override void Load()
    {
        // FIXME: This should only be done for the replay client, not ALL clients!
        // Always broadcast DamageNPC regardless of distance to the client's player.
        IL_NetMessage.SendData += EditNetMessageSendData;

        On_Netplay.InitializeServer += OnNetplayInitializeServer;
    }

    private void OnNetplayInitializeServer(On_Netplay.orig_InitializeServer orig)
    {
        orig();
        //StartRecording();
    }

    public void StartRecording()
    {
        _recordingMode = RecordingMode.Recording;

        // FIXME: Will this break an existing recording that we try to end? prob need to do it later.
        Ticks = 0;
        const int RecordClientIndex = 254;
        const string RecordClientName = "Recording";

        // Create file at: PvPAdventureReplays/{worldName}_{timestamp}.reese
        var dir = Path.Combine(Main.SavePath, "PvPAdventureReplays");
        Directory.CreateDirectory(dir);

        var fileName = $"{Main.worldName}_{DateTime.Now:yyyy-MM-dd_HH-mm}.reese";
        var fullPath = Path.Combine(dir, fileName);
        var replayFile = ReplayFile.Write(File.Open(fullPath, FileMode.Create));

        _lastReplayPath = fullPath;
        Log.Info($"Recording path: {_lastReplayPath}");

        var recordClient = Netplay.Clients[RecordClientIndex];
        // Not really needed, because we probably just did it above, but why not.
        recordClient.Reset();
        recordClient.Name = RecordClientName;
        // FIXME: File name too long? file path too long? do we care is that our problem??
        recordClient.Socket = new RecordingSocket(this, recordClient, replayFile);

        // RemoteClient.Update would set this because Socket.IsConnected() returned true, but we need this now, so
        // fast-track it.
        recordClient.IsActive = true;

        // Client says hello
        // Server sets State to 1 and syncs mods
        recordClient.State = 1;
        ModNet.SyncMods(recordClient.Id);
        // Client syncs mods to indicate it's done and ready
        // Server sends net IDs and PlayerInfo
        ModNet.SendNetIDs(recordClient.Id);
        NetMessage.SendData(MessageID.PlayerInfo, recordClient.Id);
        // Client eventually sends RequestWorldData
        // Server sets State to 2 and sends WorldData and syncs invasion
        recordClient.State = 2;
        NetMessage.SendData(MessageID.WorldData, recordClient.Id);
        Main.SyncAnInvasion(recordClient.Id);
        // Client waits for world clear and state bullshit, eventually sends SpawnTileData
        // Server sends WorldData (again yes), calculates portal bullshit(???), StatusTextSize (who cares),
        // set State to 3, TileSection for world spawn and maybe player spawn and maybe portal sections, SyncItem and
        // ItemOwner for all active items, SyncNPC for all active NPCs, SyncProjectile for all applicable projectiles,
        // NPCKillCountDeathTally for all NPC types, TileCounts, MoonlordHorror (as a broadcast, probably a bug lol),
        // UpdateTowerShieldStrengths, SyncCavernMonsterType, InitialSpawn.
        // Main.BestiaryTracker.OnPlayerJoining(whoAmI);
        // CreativePowerManager.Instance.SyncThingsToJoiningPlayer(whoAmI);
        // Main.PylonSystem.OnPlayerJoining(whoAmI);

        NetMessage.SendData(MessageID.WorldData, recordClient.Id);
        // NOTE: Not sending status text, who cares
        recordClient.State = 3;
        // Let's send ALL tile sections for the ENTIRE world
        // Likely handles world spawn, player spawn, and portal bullshit
        for (var x = 0; x < Main.maxSectionsX; x++)
        {
            for (var y = 0; y < Main.maxSectionsY; y++)
                NetMessage.SendSection(recordClient.Id, x, y);
        }

        for (var i = 0; i < Main.maxItems; i++)
        {
            var item = Main.item[i];
            if (item.active)
            {
                NetMessage.SendData(MessageID.SyncItem, recordClient.Id, number: i);
                NetMessage.SendData(MessageID.ItemOwner, recordClient.Id, number: i);
            }
        }

        for (var i = 0; i < Main.maxNPCs; i++)
        {
            var npc = Main.npc[i];
            if (npc.active)
                NetMessage.SendData(MessageID.SyncNPC, recordClient.Id, number: i);
        }

        // NOTE: Applicable projectiles is a subset of all projectiles, but in our mod, that subset is ALWAYS equal to
        // all projectiles anyhow.
        for (var i = 0; i < 1000; i++)
        {
            if (Main.projectile[i].active)
                NetMessage.SendData(MessageID.SyncProjectile, recordClient.Id, number: i);
        }

        for (var i = 0; i < NPCLoader.NPCCount; i++)
            NetMessage.SendData(MessageID.NPCKillCountDeathTally, recordClient.Id, number: i);

        NetMessage.SendData(MessageID.TileCounts, recordClient.Id);
        // NOTE: Yes, we broadcast this for whatever reason, to match vanilla.
        NetMessage.SendData(MessageID.MoonlordHorror);
        NetMessage.SendData(MessageID.UpdateTowerShieldStrengths, recordClient.Id);
        NetMessage.SendData(MessageID.SyncCavernMonsterType, recordClient.Id);
        NetMessage.SendData(MessageID.InitialSpawn, recordClient.Id);

        Main.BestiaryTracker.OnPlayerJoining(recordClient.Id);
        CreativePowerManager.Instance.SyncThingsToJoiningPlayer(recordClient.Id);
        Main.PylonSystem.OnPlayerJoining(recordClient.Id);

        // Client sends a PlayerSpawn from Player.Spawn
        // Server calls Player.Spawn, sets State to 10, enables broadcast, NetMessage.SyncConnectedPlayer, maybe
        // SetCountsAsHostForGameplay, AnglerQuest, FinishedConnectingToServer, NetMessage.greetPlayer(whoAmI),
        // NOTE: We really don't want a player for this client, so we start to deviate here.

        recordClient.State = 10;
        NetMessage.buffer[recordClient.Id].broadcast = true;

        // Can't call NetMessage.SyncConnectedPlayer because we don't want to sync ourselves to everyone else by
        // calling SyncOnePlayer with the record client (we aren't a player!)
        for (var i = 0; i < Main.maxPlayers; i++)
        {
            if (Main.player[i].active)
                NetMessage.SyncOnePlayer(i, recordClient.Id, -1);
        }

        NetMessage.SendNPCHousesAndTravelShop(recordClient.Id);
        NetMessage.SendAnglerQuest(recordClient.Id);
        CreditsRollEvent.SendCreditsRollRemainingTimeToPlayer(recordClient.Id);
        NPC.RevengeManager.SendAllMarkersToPlayer(recordClient.Id);

        NetMessage.SendData(MessageID.AnglerQuest, recordClient.Id,
            text: NetworkText.FromLiteral(Main.player[recordClient.Id].name), number: Main.anglerQuest);
        NetMessage.SendData(MessageID.FinishedConnectingToServer, recordClient.Id);

        // Flush now, so that it comes at update delta 0
        replayFile.FlushTick();
    }

    public void StopRecording()
    {
        _recordingMode = RecordingMode.NotRecording;

        if (Main.dedServ)
        {
            foreach (var remoteClient in Netplay.Clients)
            {
                if (remoteClient.Socket is RecordingSocket recordSocket)
                    recordSocket.Close();
            }
        }

        try
        {
            var dir = Path.Combine(Main.SavePath, "PvPAdventureReplays");
            var recordBinPath = Path.Combine(dir, "record.bin");

            if (!string.IsNullOrWhiteSpace(_lastReplayPath) && File.Exists(_lastReplayPath))
            {
                File.Copy(_lastReplayPath, recordBinPath, true);
                Log.Info($"Wrote record.bin: {recordBinPath} (source: {Path.GetFileName(_lastReplayPath)})");
            }
            else
            {
                Log.Warn("record.bin not written (no last replay path / file missing)");
            }
        }
        catch (Exception e)
        {
            Log.Warn("Failed to write record.bin: " + e);
        }
    }

    // FIXME: This is a shitty edit I think?
    private void EditNetMessageSendData(ILContext il)
    {
        var cursor = new ILCursor(il);

        // Find the first store to local 115...
        // (determines whether this player should receive the packet -- this is its initialization)
        cursor.GotoNext(i => i.MatchStloc(115));
        // ...and go back one instruction, to the load of the initial value...
        cursor.Index -= 1;
        // ...to remove it...
        cursor.Remove();
        // ...and replace it with 1/true.
        cursor.EmitLdcI4(1);
    }

    public override void PostUpdateEverything()
    {
        if (_recordingMode != RecordingMode.Recording)
            return;

        Ticks++;
        if (Ticks % 60 == 0)
            Log.Info("Server record tick:" + Ticks);
    }
}
