//using System;
//using System.IO;
//using System.Net;
//using MonoMod.Cil;
//using Terraria;
//using Terraria.ModLoader;
//using Terraria.Net;
//using Terraria.Net.Sockets;

//namespace PvPAdventure.Common.ReeseRecorder.Deprecated;

//[Autoload(Side = ModSide.Client)]
//public sealed class Replayer_CleanNotWorking : ModSystem
//{
//    private enum StateKind
//    {
//        Idle,
//        Replaying
//    }

//    private static StateKind state = StateKind.Idle;
//    private static string pendingPath;
//    private static ReplayFile_CleanNotWorking file;
//    private static ReplaySocket socket;
//    private static uint ticks;

//    public override void Load()
//    {
//        On_Netplay.ClientLoopSetup += OnClientLoopSetup;
//        //IL_Main.DoUpdate += TickHook;
//    }

//    public override void PostUpdateEverything()
//    {
//        if (state == StateKind.Replaying)
//            ticks++;

//        if (state == StateKind.Replaying && ticks % 60 == 0 && file != null)
//            Log.Debug($"[Replayer] tick={ticks} nextTick={file.NextTick} bytesRemaining={file.BytesRemaining}");
//    }

//    public static uint Ticks => ticks;

//    public static void StartReplaying(string path)
//    {
//        if (!Main.gameMenu || Main.menuMode != 0)
//            return;

//        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
//        {
//            Log.Warn($"Replay not found: {path}");
//            return;
//        }

//        Log.Debug($"Client starts replaying: {path}");

//        pendingPath = path;
//        Netplay.ClientLoopSetup(new TcpAddress(IPAddress.Parse("192.0.2.1"), 7777));
//    }

//    private void OnClientLoopSetup(On_Netplay.orig_ClientLoopSetup orig, RemoteAddress address)
//    {
//        orig(address);

//        if (string.IsNullOrWhiteSpace(pendingPath))
//            return;

//        var path = pendingPath;
//        pendingPath = null;

//        try
//        {
//            file = ReplayFile_CleanNotWorking.OpenRead(path);
//        }
//        catch (Exception e)
//        {
//            Log.Error($"Replay open failed: {path}, {e}");
//            return;
//        }

//        ticks = 0;
//        state = StateKind.Replaying;

//        Netplay.Connection = new RemoteServer();
//        Netplay.Connection.ReadBuffer = new byte[ushort.MaxValue];
//        Netplay.Connection.Socket = socket = new ReplaySocket(file);
//        Netplay.Connection.Socket.Connect(address);
//        Log.Debug($"Replay first packet: nextTick={file.NextTick}, bytesRemaining={file.BytesRemaining}");
//    }

//    private static void Stop()
//    {
//        if (state != StateKind.Replaying)
//            return;

//        Log.Debug("Client stops replaying");

//        try { socket?.Close(); } catch { }
//        try { file?.Dispose(); } catch { }

//        socket = null;
//        file = null;
//        state = StateKind.Idle;

//        Log.Debug("Client replay stopped");
//    }

//    private static void TickHook(ILContext il)
//    {
//        var c = new ILCursor(il);
//        if (!c.TryGotoNext(i => i.MatchStsfld<Main>("drawSkip")))
//            return;

//        c.EmitDelegate(() =>
//        {
//            if (state == StateKind.Replaying)
//                ticks++;
//        });
//    }

//    private sealed class ReplaySocket : ISocket
//    {
//        private readonly ReplayFile_CleanNotWorking file;
//        private bool connected;

//        public ReplaySocket(ReplayFile_CleanNotWorking file) => this.file = file;

//        public bool IsConnected() => connected;

//        public void Connect(RemoteAddress address)
//        {
//            connected = true;
//            Log.Debug("[Replayer] ReplaySocket.Connect()");
//        }

//        public void Close()
//        {
//            file.Dispose();
//        }

//        public void AsyncSend(byte[] data, int offset, int size, SocketSendCallback callback, object state) => callback(state);

//        public void AsyncReceive(byte[] data, int offset, int size, SocketReceiveCallback callback, object stateObj)
//        {
//            Log.Debug("AsyncReceive received: " + data.Length);
//            if (!IsDataAvailable())
//            {
//                callback(stateObj, 0);
//                return;
//            }

//            var read = file.ReadPacketData(data.AsSpan(offset, size));
//            callback(stateObj, read);

//            if (read == 0 && file.NextTick == uint.MaxValue)
//            {
//                Log.Debug("Replay EOF");
//                Stop();
//            }
//        }

//        public bool IsDataAvailable()
//        {
//            return ticks >= file.NextTick && file.BytesRemaining > 0;
//        }

//        public void SendQueuedPackets() { }
//        public bool StartListening(SocketConnectionAccepted callback) => throw new InvalidOperationException();
//        public void StopListening() => throw new InvalidOperationException();
//        public RemoteAddress GetRemoteAddress() => new TcpAddress(IPAddress.Loopback, 0);
//    }
//}