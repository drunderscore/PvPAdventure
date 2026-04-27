using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Spectator.Trackers;

internal sealed class PingTracker : ModSystem
{
	internal static readonly Dictionary<int, int> Pings = [];
	internal static readonly Dictionary<long, long> PendingPings = [];

	private static long nextPingId;
	private static int pingTimer;

	public override void OnWorldLoad()
	{
		Pings.Clear();
		PendingPings.Clear();
		nextPingId = 0;
		pingTimer = 0;
	}

	public override void OnWorldUnload()
	{
		Pings.Clear();
		PendingPings.Clear();
		nextPingId = 0;
		pingTimer = 0;
	}

	public override void PostUpdatePlayers()
	{
        if (!_TrackerStatus.IsEnabled)
		{
			base.PostUpdatePlayers();
            return;
        }

        if (Main.netMode == NetmodeID.Server)
		{
			bool changed = false;

			for (int i = 0; i < Main.maxPlayers; i++)
			{
				if (!Main.player[i].active && Pings.Remove(i))
					changed = true;
			}

			if (changed)
				PingTrackerNetHandler.SendFullSync();

			return;
		}

		if (Main.netMode != NetmodeID.MultiplayerClient)
			return;

		if (++pingTimer < 60)
			return;

		pingTimer = 0;
		SendPing();
	}

	private static void SendPing()
	{
        if (!_TrackerStatus.IsEnabled)
        {
            return;
        }

        long pingId = ++nextPingId;
		long sentTicks = System.DateTime.UtcNow.Ticks;

		PendingPings[pingId] = sentTicks;
		PingTrackerNetHandler.SendPingRequest(pingId, sentTicks);
	}

	internal static void ReceivePingResponse(long pingId, long sentTicks)
	{
        if (!_TrackerStatus.IsEnabled)
        {
            return;
        }

        if (!PendingPings.TryGetValue(pingId, out long pendingTicks) || pendingTicks != sentTicks)
			return;

		PendingPings.Remove(pingId);

		int pingMs = (int)System.TimeSpan.FromTicks(System.DateTime.UtcNow.Ticks - sentTicks).TotalMilliseconds;
		Pings[Main.myPlayer] = pingMs;
		PingTrackerNetHandler.SendPingValue(Main.myPlayer, pingMs);
	}

	internal static void SetPing(int playerIndex, int pingMs)
	{
        Pings[playerIndex] = pingMs;
	}

	public static int GetPing(int playerIndex)
	{
        return Pings.TryGetValue(playerIndex, out int ping) ? ping : -1;
	}
}