//using System;
//using System.IO;
//using System.Net;
//using MonoMod.Cil;
//using Terraria;
//using Terraria.GameContent.Creative;
//using Terraria.GameContent.Events;
//using Terraria.ID;
//using Terraria.Localization;
//using Terraria.ModLoader;
//using Terraria.Net;
//using Terraria.Net.Sockets;

//namespace PvPAdventure.Common.ReeseRecorder;

//[Autoload(Side = ModSide.Server)]
//public sealed class Recorder : ModSystem
//{
//    public enum RecordingMode
//    {
//        Idle,
//        Recording
//    }

//    public static RecordingMode State { get; private set; } = RecordingMode.Idle;

//    private const int RecordClientId = 254;

//    private static ReplayFile file;
//    private static RecordSocket socket;
//    private static uint ticks;

//    public override void Load()
//    {
//        IL_NetMessage.SendData += ForceSendToAllClients;
//    }

//    public override void PostUpdateEverything()
//    {
//        if (State == RecordingMode.Recording)
//            ticks++;

//        if (State == RecordingMode.Recording && ticks % 60 == 0)
//            Log.Debug($"Server recording tick: {ticks}");
//    }

//    public static void StartRecording()
//    {
//        if (State == RecordingMode.Recording)
//            StopRecording();

//        ticks = 0;
//        Log.Debug("Server starts recording");

//        var dir = Path.Combine(Main.SavePath, "PvPAdventureReplays");
//        Directory.CreateDirectory(dir);

//        var safeWorld = SanitizeFileName(Main.worldName);
//        var path = Path.Combine(dir, $"{safeWorld}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}{ReplayFile.Extension}");

//        file = ReplayFile.OpenWrite(path);
//        Log.Debug($"Server starts recording file: {path}");

//        var client = Netplay.Clients[RecordClientId];
//        client.Reset();
//        client.Name = "Recording";
//        client.Socket = socket = new RecordSocket(client, file);
//        client.IsActive = true;

//        Log.Debug("Server preparing baseline");

//        client.State = 1;
//        ModNet.SyncMods(client.Id);
//        ModNet.SendNetIDs(client.Id);
//        NetMessage.SendData(MessageID.PlayerInfo, client.Id);

//        client.State = 2;
//        NetMessage.SendData(MessageID.WorldData, client.Id);
//        Main.SyncAnInvasion(client.Id);

//        NetMessage.SendData(MessageID.WorldData, client.Id);
//        client.State = 3;

//        for (var x = 0; x < Main.maxSectionsX; x++)
//            for (var y = 0; y < Main.maxSectionsY; y++)
//                NetMessage.SendSection(client.Id, x, y);

//        for (var i = 0; i < Main.maxItems; i++)
//            if (Main.item[i].active)
//            {
//                NetMessage.SendData(MessageID.SyncItem, client.Id, number: i);
//                NetMessage.SendData(MessageID.ItemOwner, client.Id, number: i);
//            }

//        for (var i = 0; i < Main.maxNPCs; i++)
//            if (Main.npc[i].active)
//                NetMessage.SendData(MessageID.SyncNPC, client.Id, number: i);

//        for (var i = 0; i < 1000; i++)
//            if (Main.projectile[i].active)
//                NetMessage.SendData(MessageID.SyncProjectile, client.Id, number: i);

//        for (var i = 0; i < NPCLoader.NPCCount; i++)
//            NetMessage.SendData(MessageID.NPCKillCountDeathTally, client.Id, number: i);

//        NetMessage.SendData(MessageID.TileCounts, client.Id);
//        NetMessage.SendData(MessageID.MoonlordHorror);
//        NetMessage.SendData(MessageID.UpdateTowerShieldStrengths, client.Id);
//        NetMessage.SendData(MessageID.SyncCavernMonsterType, client.Id);
//        NetMessage.SendData(MessageID.InitialSpawn, client.Id);

//        Main.BestiaryTracker.OnPlayerJoining(client.Id);
//        CreativePowerManager.Instance.SyncThingsToJoiningPlayer(client.Id);
//        Main.PylonSystem.OnPlayerJoining(client.Id);

//        client.State = 10;
//        NetMessage.buffer[client.Id].broadcast = true;

//        for (var i = 0; i < Main.maxPlayers; i++)
//            if (Main.player[i].active)
//                NetMessage.SyncOnePlayer(i, client.Id, -1);

//        NetMessage.SendNPCHousesAndTravelShop(client.Id);
//        NetMessage.SendAnglerQuest(client.Id);
//        CreditsRollEvent.SendCreditsRollRemainingTimeToPlayer(client.Id);
//        NPC.RevengeManager.SendAllMarkersToPlayer(client.Id);

//        NetMessage.SendData(MessageID.AnglerQuest, client.Id, text: NetworkText.FromLiteral(Main.player[client.Id].name), number: Main.anglerQuest);
//        NetMessage.SendData(MessageID.FinishedConnectingToServer, client.Id);

//        State = RecordingMode.Recording;
//        Log.Debug("Server recording ready");
//    }

//    public static void StopRecording()
//    {
//        if (State != RecordingMode.Recording)
//            return;

//        Log.Debug("Server stops recording");

//        try { socket?.Close(); } catch { }
//        try { file?.Dispose(); } catch { }

//        socket = null;
//        file = null;

//        try { Netplay.Clients[RecordClientId].Reset(); } catch { }

//        State = RecordingMode.Idle;
//        Log.Debug("Server stopped recording");
//        Log.Chat("Server stopped recording");
//    }

//    private static void ForceSendToAllClients(ILContext il)
//    {
//        var c = new ILCursor(il);
//        if (!c.TryGotoNext(i => i.MatchStloc(115)))
//            return;

//        c.Index -= 1;
//        c.Remove();
//        c.EmitLdcI4(1);
//    }

//    private sealed class RecordSocket : ISocket
//    {
//        private readonly RemoteClient client;
//        private readonly ReplayFile file;
//        private bool closed;

//        public RecordSocket(RemoteClient client, ReplayFile file)
//        {
//            this.client = client;
//            this.file = file;
//        }

//        public void Close()
//        {
//            if (closed)
//                return;

//            closed = true;
//            Log.Debug("RecordSocket closing...");

//            try { file.Dispose(); } catch { }

//            client.IsActive = false;
//            client.Socket = null;
//            client.State = 0;
//            NetMessage.buffer[client.Id].broadcast = false;
//        }

//        public bool IsConnected() => true;
//        public void Connect(RemoteAddress address) => throw new InvalidOperationException();
//        public void AsyncReceive(byte[] data, int offset, int size, SocketReceiveCallback callback, object state = null) => throw new InvalidOperationException();
//        public bool IsDataAvailable() => false;

//        public void AsyncSend(byte[] data, int offset, int size, SocketSendCallback callback, object state = null)
//        {
//            file.WritePacket(data.AsSpan(offset, size), ticks);
//            callback(state);
//        }

//        public void SendQueuedPackets()
//        {
//            client.TimeOutTimer = 0;
//        }

//        public bool StartListening(SocketConnectionAccepted callback) => throw new InvalidOperationException();
//        public void StopListening() => throw new InvalidOperationException();
//        public RemoteAddress GetRemoteAddress() => new TcpAddress(IPAddress.Loopback, 0);
//    }

//    private static string SanitizeFileName(string s)
//    {
//        foreach (var c in Path.GetInvalidFileNameChars())
//            s = s.Replace(c, '_');

//        return s;
//    }
//}

//public sealed class ReeseNpc : GlobalNPC
//{
//    public override void PostAI(NPC npc)
//    {
//        if (Main.netMode == NetmodeID.Server && Recorder.State == Recorder.RecordingMode.Recording)
//            npc.netAlways = true;
//    }
//}

//public sealed class ReeseProjectile : GlobalProjectile
//{
//    public override void PostAI(Projectile projectile)
//    {
//        if (Main.netMode == NetmodeID.Server && Recorder.State == Recorder.RecordingMode.Recording)
//            projectile.netImportant = true;
//    }
//}