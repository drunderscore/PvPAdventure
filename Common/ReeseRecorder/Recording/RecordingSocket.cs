using PvPAdventure.Common.ReeseRecorder.Replay;
using System;
using Terraria;
using Terraria.Localization;
using Terraria.Net;
using Terraria.Net.Sockets;

namespace PvPAdventure.Common.ReeseRecorder.Recording;

public class RecordingSocket(ITicker ticker, RemoteClient remoteClient, ReplayFile replayFile) : ISocket
{
    private readonly RecordingRemoteAddress _remoteAddress = new();

    public void Close()
    {
        Netplay.KickClient(this, NetworkText.FromLiteral("Recording closed"));
        // This is essentially a flush operation
        SendQueuedPackets();
        Log.Info("Closing record socket");
        try { replayFile.FlushTick(); } catch { }
        replayFile.Dispose();
    }

    public bool IsConnected() => true;

    public void Connect(RemoteAddress address) =>
        throw new InvalidOperationException("The recording socket cannot connect");

    public void AsyncSend(byte[] data, int offset, int size, SocketSendCallback callback, object state = null)
    {
        // FIXME: Actually do this async
        // But the main thread definitely leads to packet arriving out of order or something, so dont do that!
        //Main.QueueMainThreadAction(() =>
        //{
        replayFile.WritePacketData(data[offset..(offset + size)], ticker.Ticks);
        callback(state);
        //});
    }

    public void AsyncReceive(byte[] data, int offset, int size, SocketReceiveCallback callback,
        object state = null) => throw new InvalidOperationException("The recording socket cannot receive");

    public bool IsDataAvailable() => false;

    public void SendQueuedPackets()
    {
        // TODO: Verify this is actually preventing us from timing out
        //       (and that this is an issue at all which i think it is)
        remoteClient.TimeOutTimer = 0;
    }

    public bool StartListening(SocketConnectionAccepted callback) =>
        throw new InvalidOperationException("The recording socket cannot listen");

    public void StopListening() => throw new InvalidOperationException("The recording socket cannot listen");

    public RemoteAddress GetRemoteAddress() => _remoteAddress;
}
