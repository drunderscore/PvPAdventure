using PvPAdventure.Common.Game.GameReporters;
using PvPAdventure.Core.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Game.StatTrackers;

internal static class MatchStatsNetHandler
{
    public static void HandleDeltaPacket(BinaryReader reader, int whoAmI)
    {
        MatchStatsPlayer.StatDelta delta = MatchStatsPlayer.StatDelta.Deserialize(reader);

        if (Main.netMode != NetmodeID.Server)
            return;

        if (!StatsReporter.IsValidClientDelta(delta.StatKey, delta.ItemKey, delta.Amount))
            return;

        GameManager gameManager = Terraria.ModLoader.ModContent.GetInstance<GameManager>();
        if (gameManager.CurrentPhase != GameManager.Phase.Playing)
            return;

        if (whoAmI < 0 || whoAmI >= Main.maxPlayers)
            return;

        Player player = Main.player[whoAmI];
        if (player == null || !player.active)
            return;

        if ((Team)player.team == Team.None)
            return;

        player.GetModPlayer<MatchStatsPlayer>().ApplyDelta(delta.StatKey, delta.ItemKey, delta.Amount);
    }

    public static void HandleSnapshotPacket(BinaryReader reader)
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        foreach (Player player in Main.ActivePlayers)
            player.GetModPlayer<MatchStatsPlayer>().ResetMatchStats();

        int playerCount = reader.ReadByte();

        for (int i = 0; i < playerCount; i++)
        {
            int playerId = reader.ReadByte();
            int statCount = reader.ReadByte();
            Dictionary<MatchStatKey, uint> values = new(statCount);

            for (int j = 0; j < statCount; j++)
            {
                MatchStatKey key = (MatchStatKey)reader.ReadByte();
                uint value = reader.ReadUInt32();

                if (Enum.IsDefined(key) && !string.IsNullOrEmpty(StatsReporter.GetStatKey(key)))
                    values[key] = value;
            }

            if (playerId >= 0 && playerId < Main.maxPlayers && Main.player[playerId].active)
                Main.player[playerId].GetModPlayer<MatchStatsPlayer>().ReplaceNetworkStats(values);
        }
    }

    public static void SendSnapshot()
    {
        if (Main.netMode != NetmodeID.Server)
            return;

        List<Player> players = [];

        foreach (Player player in Main.ActivePlayers)
        {
            if ((Team)player.team != Team.None)
                players.Add(player);

            if (players.Count == byte.MaxValue)
                break;
        }

        ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.MatchStatsSnapshot);
        packet.Write((byte)players.Count);

        foreach (Player player in players)
        {
            MatchStatsPlayer matchStats = player.GetModPlayer<MatchStatsPlayer>();
            List<(MatchStatKey Key, uint Value)> values = Enum.GetValues<MatchStatKey>()
                .Select(key => (Key: key, Value: matchStats.GetStat(key)))
                .Where(pair => pair.Value > 0)
                .Take(byte.MaxValue)
                .ToList();

            packet.Write((byte)player.whoAmI);
            packet.Write((byte)values.Count);

            foreach ((MatchStatKey key, uint value) in values)
            {
                packet.Write((byte)key);
                packet.Write(value);
            }
        }

        packet.Send();
    }
}

internal sealed class MatchStatsSyncSystem : ModSystem
{
    public override void PostUpdatePlayers()
    {
        if (Main.netMode == NetmodeID.Server && Main.GameUpdateCount % 30 == 0)
            MatchStatsNetHandler.SendSnapshot();
    }
}
