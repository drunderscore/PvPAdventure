using Terraria.Net;

namespace PvPAdventure.Common.ReeseRecorder.Recording;

public class RecordingRemoteAddress : RemoteAddress
{
    public override string GetIdentifier() => "Recording";
    public override string GetFriendlyName() => "Recording";
    public override bool IsLocalHost() => true;

    public override string ToString() => GetFriendlyName();
}
