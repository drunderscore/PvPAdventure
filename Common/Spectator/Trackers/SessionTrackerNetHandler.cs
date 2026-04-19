using PvPAdventure.Core.Net;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Spectator.Trackers;

internal static class SessionTrackerNetHandler
{
	public static void HandlePacket(BinaryReader reader, int sender)
	{
		string type = reader.ReadString();

		if (type == "FullSync")
			ReceiveFullSync(reader);
	}

	private static ModPacket GetPacket(string type)
	{
		ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
		packet.Write((byte)AdventurePacketIdentifier.SessionTracker);
		packet.Write("SessionTracker");
		packet.Write(type);
		return packet;
	}

	public static void SendFullSync(int toClient = -1)
	{
		if (Main.netMode != NetmodeID.Server)
			return;

		ModPacket packet = GetPacket("FullSync");
		packet.Write(SessionTracker.Sessions.Count);

		DateTime now = DateTime.UtcNow;

		foreach ((int playerIndex, DateTime start) in SessionTracker.Sessions)
		{
			packet.Write(playerIndex);
			packet.Write((now - start).Ticks);
		}

		packet.Send(toClient);
	}

	private static void ReceiveFullSync(BinaryReader reader)
	{
		if (Main.netMode != NetmodeID.MultiplayerClient)
			return;

		SessionTracker.Sessions.Clear();

		int count = reader.ReadInt32();
		DateTime now = DateTime.UtcNow;

		for (int i = 0; i < count; i++)
		{
			int playerIndex = reader.ReadInt32();
			long elapsedTicks = reader.ReadInt64();
			SessionTracker.Sessions[playerIndex] = now - new TimeSpan(elapsedTicks);
		}
	}
}
