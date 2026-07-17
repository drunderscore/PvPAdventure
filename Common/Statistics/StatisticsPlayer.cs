using Microsoft.Xna.Framework.Input;
using PvPAdventure.Common.Game;
using PvPAdventure.Common.Game.GameReporters;
using PvPAdventure.Common.Game.StatTrackers;
using PvPAdventure.Common.Teams;
using PvPAdventure.Core.Config;
using PvPAdventure.Core.Net;
using PvPAdventure.Core.Utilities;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.UI.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace PvPAdventure.Common.Statistics;

internal class StatisticsPlayer : ModPlayer
{
    private const string ErkySscTag = "ErkySSC";
    private const string StatsTag = "PvPAdventure";

    public DamageInfo RecentDamageFromPlayer { get; private set; }
    public int Kills { get; private set; }
    public int Deaths { get; private set; }
    public HashSet<int> ItemPickups { get; private set; } = new();

    public sealed class DamageInfo(byte who, int ticksRemaining)
    {
        public byte Who { get; } = who;
        public int TicksRemaining { get; set; } = ticksRemaining;
    }

    public sealed class Statistics(byte player, int kills, int deaths) : IPacket<Statistics>
    {
        public byte Player { get; } = player;
        public int Kills { get; } = kills;
        public int Deaths { get; } = deaths;

        public static Statistics Deserialize(BinaryReader reader)
        {
            var player = reader.ReadByte();
            var kills = reader.ReadInt32();
            var deaths = reader.ReadInt32();
            return new(player, kills, deaths);
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Player);
            writer.Write(Kills);
            writer.Write(Deaths);
        }

        public void Apply(StatisticsPlayer statisticsPlayer)
        {
            if (Kills < 0 || Kills > 1_000_000 || Deaths < 0 || Deaths > 1_000_000)
            {
                Log.Warn($"Discarding bogus stats: kills={Kills}, deaths={Deaths}");
                Log.Chat($"Discarding bogus stats: kills={Kills}, deaths={Deaths}");
                return;
            }

            statisticsPlayer.Kills = Kills;
            statisticsPlayer.Deaths = Deaths;
        }

    }

    public sealed class ItemPickup : IPacket<ItemPickup>
    {
        public int[] Items { get; }

        public ItemPickup(int[] items)
        {
            // Defensive copy to avoid CS9124: do not capture parameter directly
            Items = items is not null ? (int[])items.Clone() : [];
        }

        public static ItemPickup Deserialize(BinaryReader reader)
        {
            var length = reader.ReadInt32();
            var items = new int[length];
            for (var i = 0; i < items.Length; i++)
                items[i] = reader.ReadInt32();

            return new(items);
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Items.Length);

            foreach (var item in Items)
                writer.Write(item);
        }

        public void Apply(StatisticsPlayer statisticsPlayer)
        {
            statisticsPlayer.ItemPickups.UnionWith(Items);
        }
    }

    public void ResetMatchStatistics(bool sync = false)
    {
        RecentDamageFromPlayer = null;
        Kills = 0;
        Deaths = 0;

        if (sync)
            SyncStatistics();
    }
    

    #region Hooks
    public override void PreUpdate()
    {
        if (RecentDamageFromPlayer != null && --RecentDamageFromPlayer.TicksRemaining <= 0)
        {
            Mod.Logger.Info($"Recent damage for {this} expired (was from {RecentDamageFromPlayer.Who})");
            RecentDamageFromPlayer = null;
        }

#if DEBUG
        if (Player.whoAmI == Main.myPlayer &&
            !Main.dedServ &&
            Main.keyState.IsKeyDown(Keys.NumPad7) &&
            Main.oldKeyState.IsKeyUp(Keys.NumPad7))
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                var packet = Mod.GetPacket();
                packet.Write((byte)AdventurePacketIdentifier.PlayerStatistics);
                new Statistics((byte)Main.myPlayer, Kills + 1, Deaths).Serialize(packet);
                packet.Send();

                Log.Chat($"Debug requested +1 kill for {Player.name}. Kills: {Kills + 1}");
            }
        }
#endif
    }
    public override void PostHurt(Player.HurtInfo info)
    {
        // Don't need the client to have this information right now, and I can't be sure it's accurate.
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        if (!info.PvP)
            return;

        if (info.DamageSource.SourcePlayerIndex == -1)
        {
            Mod.Logger.Warn($"PostHurt for {this} indicated PvP, but source player was -1");
            return;
        }

        var damagerPlayer = Main.player[info.DamageSource.SourcePlayerIndex];
        if (!damagerPlayer.active)
        {
            Mod.Logger.Warn($"PostHurt for {this} sourced from inactive player");
            return;
        }

        // Hurting ourselves doesn't change our recent damage
        if (info.DamageSource.SourcePlayerIndex == Player.whoAmI)
            return;

        DamageTracker.RecordPostHurt(Player, info);

        RecentDamageFromPlayer = new((byte)damagerPlayer.whoAmI,
            ModContent.GetInstance<ServerConfig>().Immunity.RecentDamagePreservationFrames);
    }
    public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;

        try
        {
            Player killer = null;

            // If you killed yourself, we should delegate to the recent damage.
            if (pvp && damageSource.SourcePlayerIndex != -1 && damageSource.SourcePlayerIndex != Player.whoAmI)
            {
                killer = Main.player[damageSource.SourcePlayerIndex];
            }
            else
            {
                // We checked this earlier, but let's check again for logging purposes.
                if (pvp && damageSource.SourcePlayerIndex == -1)
                    Mod.Logger.Warn($"PvP kill without a valid SourcePlayerIndex ({this} killed)");

                if (RecentDamageFromPlayer != null)
                    killer = Main.player[RecentDamageFromPlayer.Who];
            }

            // Nothing should happen for suicide
            if (killer == null || !killer.active || killer.whoAmI == Player.whoAmI)
                return;

            // Award player kill to team
            ModContent.GetInstance<PointsManager>().AwardPlayerKillToTeam(killer, Player);

            // Increment killer's kill count
            StatisticsPlayer killerStats = killer.GetModPlayer<StatisticsPlayer>();
            killerStats.Kills += 1;
            killerStats.SyncStatistics();

            Deaths += 1;
            SyncStatistics();

            NetworkText customReasonText = NetworkText.FromLiteral
                ($"[c/{Main.teamColor[killer.team].Hex3()}:{killer.name}] {ItemTagHandler.GenerateTag(damageSource.SourceItem ?? new Item(ItemID.Skull))} [c/{Main.teamColor[Player.team].Hex3()}:{Player.name}]");

            damageSource.CustomReason = customReasonText;
        }
        finally
        {
            // PvP or not, reset whom we last took damage from.
            RecentDamageFromPlayer = null;

            // Remove recent damage for ALL players we've attacked after we die.
            // These are indirect post-mortem kills, which we don't want.
            // FIXME: We would still like to attribute this to the next recent damager, which would require a stack of
            //        recent damage.
            foreach (var player in Main.ActivePlayers)
            {
                var adventurePlayer = player.GetModPlayer<StatisticsPlayer>();
                if (adventurePlayer.RecentDamageFromPlayer?.Who == Player.whoAmI)
                    adventurePlayer.RecentDamageFromPlayer = null;
            }
        }
    }
    private void SyncStatistics(int to = -1, int ignore = -1)
    {
        var packet = Mod.GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.PlayerStatistics);
        new Statistics((byte)Player.whoAmI, Kills, Deaths).Serialize(packet);
        packet.Send(to, ignore);
    }
    private void SyncSingleItemPickup(int item, int to = -1, int ignore = -1)
    {
        var packet = Mod.GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.PlayerItemPickup);
        new ItemPickup([item]).Serialize(packet);
        packet.Send(to, ignore);
    }

    private void SyncItemPickups(int to = -1, int ignore = -1)
    {
        var packet = Mod.GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.PlayerItemPickup);
        new ItemPickup(ItemPickups.ToArray()).Serialize(packet);
        packet.Send(to, ignore);
    }

    internal bool ExportSscStats(string characterKey, TagCompound root)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || root == null)
            return false;

        TagCompound ssc = root.ContainsKey(ErkySscTag) ? root.GetCompound(ErkySscTag) : [];
        ssc[StatsTag] = new TagCompound
        {
            ["version"] = 1,
            ["characterKey"] = characterKey ?? "",
            ["matchToken"] = CurrentMatchToken(),
            ["kills"] = Kills,
            ["deaths"] = Deaths,
            ["itemPickups"] = ItemPickups.ToArray(),
            ["team"] = Player.team
        };

        root[ErkySscTag] = ssc;
        return true;
    }

    internal bool ImportSscStats(string characterKey, TagCompound root)
    {
        if (root == null)
            return true;

        TagCompound ssc = root.ContainsKey(ErkySscTag) ? root.GetCompound(ErkySscTag) : [];
        TagCompound saved = null;
        bool legacy = false;

        if (ssc.ContainsKey(StatsTag))
        {
            saved = ssc.GetCompound(StatsTag);
        }
        else if (ssc.ContainsKey("kills") || ssc.ContainsKey("deaths"))
        {
            // One-time migration of the previous flat ErkySSC statistics format.
            saved = ssc;
            legacy = true;
        }
        else
        {
            // Older files only contain StatisticsPlayer's normal tModLoader data.
            saved = FindLegacyModPlayerStats(root);
            legacy = saved != null;
        }

        bool restore = saved != null;

        if (restore && legacy && Main.netMode != NetmodeID.MultiplayerClient)
            restore = !string.IsNullOrEmpty(CurrentMatchToken());

        if (restore && !legacy)
        {
            string savedCharacter = saved.ContainsKey("characterKey") ? saved.GetString("characterKey") : "";
            string savedMatch = saved.ContainsKey("matchToken") ? saved.GetString("matchToken") : "";
            string currentMatch = CurrentMatchToken();

            restore = !string.IsNullOrEmpty(currentMatch) &&
                (string.IsNullOrEmpty(savedCharacter) || savedCharacter == characterKey) &&
                savedMatch == currentMatch;
        }

        // A client receives this root from ErkySSC after the server has normalized
        // it, so the nested values are safe to apply locally after PlayerIO.LoadData.
        if (Main.netMode == NetmodeID.MultiplayerClient)
            restore = saved != null && !legacy;

        if (restore)
            ApplySavedStats(saved);

        if (Main.netMode == NetmodeID.Server)
        {
            // Normalize every loaded file to the explicit, tokened format. When a
            // saved token is stale, this writes the server's current values instead.
            ExportSscStats(characterKey, root);
            SyncStatistics();
            SyncItemPickups();
        }
        else
        {
            ModContent.GetInstance<PointsManager>().UiScoreboard.Invalidate();
        }

        return true;
    }

    private void ApplySavedStats(TagCompound saved)
    {
        Kills = System.Math.Clamp(saved.GetInt("kills"), 0, 1_000_000);
        Deaths = System.Math.Clamp(saved.GetInt("deaths"), 0, 1_000_000);
        ItemPickups = saved.ContainsKey("itemPickups") ? saved.Get<int[]>("itemPickups").ToHashSet() : [];

        if (saved.ContainsKey("team"))
            Player.team = saved.GetInt("team");
    }

    private static TagCompound FindLegacyModPlayerStats(TagCompound root)
    {
        if (!root.ContainsKey("modData"))
            return null;

        foreach (TagCompound entry in root.GetList<TagCompound>("modData"))
        {
            if (entry.GetString("mod") == "PvPAdventure" &&
                entry.GetString("name") == nameof(StatisticsPlayer) &&
                entry.ContainsKey("data"))
                return entry.GetCompound("data");
        }

        return null;
    }

    private static string CurrentMatchToken()
    {
        GameManager game = ModContent.GetInstance<GameManager>();
        return game.CurrentPhase == GameManager.Phase.Playing && game.MatchStartTime.HasValue
            ? game.MatchStartTime.Value.ToUniversalTime().Ticks.ToString()
            : "";
    }

    public override void SaveData(TagCompound tag)
    {
        tag["kills"] = Kills;
        tag["deaths"] = Deaths;
        tag["itemPickups"] = ItemPickups.ToArray();
        tag["team"] = Player.team;
    }

    public override void LoadData(TagCompound tag)
    {
        Kills = tag.Get<int>("kills");
        Deaths = tag.Get<int>("deaths");
        ItemPickups = tag.Get<int[]>("itemPickups").ToHashSet();
        Player.team = tag.Get<int>("team");
    }
    public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
    {
        SyncStatistics(toWho, fromWho);

        if (newPlayer)
        {
            // Sync all of our pickups at once when we join
            if (!Main.dedServ)
                SyncItemPickups(toWho, fromWho);

            var packet = Mod.GetPacket();
            packet.Write((byte)AdventurePacketIdentifier.PlayerTeam);
            new Team((byte)Player.whoAmI, (Terraria.Enums.Team)Player.team).Serialize(packet);
            packet.Send(toWho, fromWho);
        }
    }
    public override bool OnPickup(Item item)
    {
        // FIXME: This could work for non-modded items, but I'm not so sure the item type ordinals are determinant.
        //         We _can_ work under the assumption this one player will be played within one world with the same mods
        //         always, but I'm not sure even that is good enough -- so let's just ignore them for now.
        if (item.ModItem == null)
        {
            if (ItemPickups.Add(item.type) && Main.netMode == NetmodeID.MultiplayerClient)
                SyncSingleItemPickup(item.type);
        }

        return true;
    }
    #endregion
}
