using System;
using Terraria.Net;
using Terraria.Net.Sockets;

namespace PvPAdventure.Common.ReeseRecorder.Replay;

public class ReplaySocket(ITicker ticker, ReplayFile replayFile) : ISocket
{
    private readonly ReplayRemoteAddress _remoteAddress = new();

    public void Close()
    {
        Log.Info("Closing replay socket");
        replayFile.Dispose();
    }

    public bool IsConnected() => true;

    public void Connect(RemoteAddress address)
    {
        Log.Info($"Replay connect to {address}");
    }

    public void AsyncSend(byte[] data, int offset, int size, SocketSendCallback callback, object state)
    {
    }

    public void AsyncReceive(byte[] data, int offset, int size, SocketReceiveCallback callback, object state)
    {
        // TODO: Is this called even if IsDataAvailable returns false?
        if (!IsDataAvailable())
            throw new InvalidOperationException("I SAID NOTHING WAS AVAILABLE YOU BITCH.");

        var numberOfBytesRead = replayFile.ReadPacketData(data.AsSpan()[offset..(offset + size)]);

        callback(state, numberOfBytesRead);
    }

    public bool IsDataAvailable()
    {
        return ticker.Ticks >= replayFile.Tick && replayFile.NumberOfPacketDataBytesRemaining > 0;
    }

    public void SendQueuedPackets()
    {
    }

    public bool StartListening(SocketConnectionAccepted callback) =>
        throw new InvalidOperationException("The replaying socket cannot listen");

    public void StopListening() => throw new InvalidOperationException("The replaying socket cannot listen");

    public RemoteAddress GetRemoteAddress() => _remoteAddress;
}
