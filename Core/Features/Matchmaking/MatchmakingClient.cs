using PvPAdventure.Core.Helpers;
using System;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Core.Features.Matchmaking
{
    public static class MatchmakingClient
    {
        public static Action<int, int> OnCounts; // (online, queuing)

        public static void SendToggle(bool isQueuing)
        {
            if (Main.netMode != Terraria.ID.NetmodeID.MultiplayerClient) { Log.Error("not MP"); return; }
            if (!ModLoader.TryGetMod("PvPAdventure", out var mod)) { Log.Error("mod not found!!"); return; }

            var p = mod.GetPacket();
            p.Write((byte)AdventurePacketIdentifier.QueueToggle);
            p.Write(isQueuing);
            p.Send();
        }

        public static void RequestCounts()
        {
            if (Main.netMode != Terraria.ID.NetmodeID.MultiplayerClient) return;
            if (!ModLoader.TryGetMod("PvPAdventure", out var mod)) return;

            var p = mod.GetPacket();
            p.Write((byte)AdventurePacketIdentifier.QueueCountsRequest);
            p.Send();
        }
    }
}
