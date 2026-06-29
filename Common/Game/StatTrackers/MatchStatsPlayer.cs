using PvPAdventure.Common.Game.GameReporters;
using PvPAdventure.Core.Net;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Game.StatTrackers;

internal enum MatchStatKey : byte
{
    DamageDealt,
    DamageTaken,
    ConsumablesUsed,
    TilesPlaced,
    TilesMined,
    MiningToolsUsed,
    LavaDeaths,
    FoodEaten,
    BossDamageDealt,
    PortalKills,
    LostHoney
}

internal sealed class MatchStatsPlayer : ModPlayer
{
    private readonly Dictionary<string, uint> stats = [];
    private readonly Dictionary<string, Dictionary<int, uint>> itemStats = [];

    public readonly record struct StatDelta(MatchStatKey StatKey, int ItemKey, uint Amount) : IPacket<StatDelta>
    {
        public static StatDelta Deserialize(BinaryReader reader)
        {
            var statKey = (MatchStatKey)reader.ReadByte();
            int itemKey = reader.ReadInt32();
            uint amount = reader.ReadUInt32();
            return new StatDelta(statKey, itemKey, amount);
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)StatKey);
            writer.Write(ItemKey);
            writer.Write(Amount);
        }
    }

    public void ResetMatchStats()
    {
        stats.Clear();
        itemStats.Clear();
    }

    public Dictionary<string, uint> CopyStats() => new(stats);

    public Dictionary<string, IDictionary<int, uint>> CopyItemStats()
    {
        Dictionary<string, IDictionary<int, uint>> result = [];

        foreach ((string statKey, Dictionary<int, uint> byItem) in itemStats)
            result[statKey] = new Dictionary<int, uint>(byItem);

        return result;
    }

    public void ApplyDelta(MatchStatKey statKey, int itemKey, uint amount)
    {
        string key = StatsReporter.GetStatKey(statKey);
        if (string.IsNullOrWhiteSpace(key) || amount == 0)
            return;

        AddStat(key, amount);

        if (itemKey >= 0)
            AddItemStat(key, itemKey, amount);
    }

    public static void RecordLocalItemStat(MatchStatKey statKey, int itemKey, uint amount = 1)
    {
        RecordLocalDelta(statKey, itemKey, amount);
    }

    public static void RecordLocalStat(MatchStatKey statKey, uint amount = 1)
    {
        RecordLocalDelta(statKey, -1, amount);
    }

    private static void RecordLocalDelta(MatchStatKey statKey, int itemKey, uint amount)
    {
        if (Main.dedServ || !IsMatchPlaying() || !StatsReporter.IsValidClientDelta(statKey, itemKey, amount))
            return;

        Player player = Main.LocalPlayer;
        if (player == null || !player.active || player.whoAmI != Main.myPlayer)
            return;

        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            player.GetModPlayer<MatchStatsPlayer>().ApplyDelta(statKey, itemKey, amount);
            return;
        }

        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        ModPacket packet = ModContent.GetInstance<PvPAdventure>().GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.MatchStatDelta);
        new StatDelta(statKey, itemKey, amount).Serialize(packet);
        packet.Send();
    }

    public static void RecordServerStat(Player player, MatchStatKey statKey, uint amount = 1, int itemKey = -1)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || !IsMatchPlaying())
            return;

        if (player == null || !player.active || amount == 0)
            return;

        player.GetModPlayer<MatchStatsPlayer>().ApplyDelta(statKey, itemKey, amount);
    }

    public static void RecordServerDamage(Player attacker, Player victim, Player.HurtInfo info)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || !IsMatchPlaying())
            return;

        if (attacker == null || victim == null || !attacker.active || !victim.active || attacker.whoAmI == victim.whoAmI)
            return;

        if (info.Damage <= 0)
            return;

        uint damage = (uint)info.Damage;

        MatchStatsPlayer attackerStats = attacker.GetModPlayer<MatchStatsPlayer>();
        attackerStats.AddStat(StatsReporter.DamageDealt, damage);

        int sourceItem = GetSourceItem(info);
        if (StatsReporter.IsValidItemId(sourceItem))
            attackerStats.AddItemStat(StatsReporter.DamageDealt, sourceItem, damage);

        victim.GetModPlayer<MatchStatsPlayer>().AddStat(StatsReporter.DamageTaken, damage);
    }

    private static int GetSourceItem(Player.HurtInfo info)
    {
        Item item = info.DamageSource.SourceItem;
        if (item != null && !item.IsAir && item.type > ItemID.None)
            return item.type;

        return ItemID.None;
    }

    private static bool IsMatchPlaying()
    {
        GameManager gameManager = ModContent.GetInstance<GameManager>();
        return gameManager != null && gameManager.CurrentPhase == GameManager.Phase.Playing;
    }

    private void AddStat(string statKey, uint amount)
    {
        stats.TryGetValue(statKey, out uint current);
        stats[statKey] = AddClamped(current, amount);
    }

    private void AddItemStat(string statKey, int itemKey, uint amount)
    {
        if (!itemStats.TryGetValue(statKey, out Dictionary<int, uint> byItem))
        {
            byItem = [];
            itemStats[statKey] = byItem;
        }

        byItem.TryGetValue(itemKey, out uint current);
        byItem[itemKey] = AddClamped(current, amount);
    }

    private static uint AddClamped(uint left, uint right)
    {
        if (uint.MaxValue - left < right)
            return uint.MaxValue;

        return left + right;
    }
}
