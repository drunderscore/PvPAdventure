using Terraria.Net;

namespace PvPAdventure.Common.ReeseRecorder.Replay;

public class ReplayRemoteAddress : RemoteAddress
{
    public override string GetIdentifier() => "Replaying";
    public override string GetFriendlyName() => "Replaying";
    public override bool IsLocalHost() => true;

    public override string ToString() => GetFriendlyName();
}
