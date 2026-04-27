//using Microsoft.Xna.Framework;
//using PvPAdventure.Common.Statistics;
//using PvPAdventure.Common.Teams;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Terraria;
//using Terraria.ModLoader;

//namespace PvPAdventure.Common.Spectator.UI.Tabs.World;

//internal static class WorldMiscInfoHelper
//{
//    #region Misc world information
//    public static int CountActivePlayers()
//    {
//        int count = 0;

//        for (int i = 0; i < Main.maxPlayers; i++)
//        {
//            Player player = Main.player[i];
//            if (player?.active == true)
//                count++;
//        }

//        return count;
//    }

//    public static void GetPlayerSummary(out int totalKills, out int totalDeaths, out string topFragger)
//    {
//        totalKills = 0;
//        totalDeaths = 0;
//        topFragger = "TBD";

//        int bestKills = -1;
//        int bestDeaths = int.MaxValue;
//        float bestKd = float.MinValue;

//        for (int i = 0; i < Main.maxPlayers; i++)
//        {
//            Player player = Main.player[i];
//            if (player == null || !player.active)
//                continue;

//            StatisticsPlayer stats = player.GetModPlayer<StatisticsPlayer>();
//            totalKills += stats.Kills;
//            totalDeaths += stats.Deaths;

//            float kd = stats.Deaths <= 0 ? stats.Kills : stats.Kills / (float)stats.Deaths;
//            bool better = stats.Kills > bestKills || stats.Kills == bestKills && stats.Deaths < bestDeaths || stats.Kills == bestKills && stats.Deaths == bestDeaths && kd > bestKd;
//            if (!better)
//                continue;

//            bestKills = stats.Kills;
//            bestDeaths = stats.Deaths;
//            bestKd = kd;
//            topFragger = $"{player.name} ({stats.Kills}/{stats.Deaths})";
//        }
//    }

//    public static List<NPC> GetTownNpcs(int max = 16)
//    {
//        List<NPC> list = [];

//        for (int i = 0; i < Main.maxNPCs; i++)
//        {
//            NPC npc = Main.npc[i];
//            if (npc == null || !npc.active || !npc.townNPC && !npc.isLikeATownNPC)
//                continue;

//            list.Add(npc);
//        }

//        return list.Take(max).ToList();
//    }

//    public static int CountHousedTownNpcs(List<NPC> townNpcs)
//    {
//        int count = 0;

//        for (int i = 0; i < townNpcs.Count; i++)
//        {
//            if (!townNpcs[i].homeless)
//                count++;
//        }

//        return count;
//    }

//    public static Item GetMostValuableItem()
//    {
//        Item mostValuable = null;

//        for (int i = 0; i < Main.maxItems; i++)
//        {
//            Item item = Main.item[i];
//            if (item == null || !item.active || item.IsAir)
//                continue;

//            if (mostValuable == null || item.value > mostValuable.value)
//                mostValuable = item;
//        }

//        return mostValuable;
//    }

//    public static string GetBedsPerTeamText()
//    {
//        TeamBedSystem bedSystem = ModContent.GetInstance<TeamBedSystem>();
//        var field = typeof(TeamBedSystem).GetField("bedTeams", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

//        if (field?.GetValue(bedSystem) is not Dictionary<Point, TerrariaTeam> beds || beds.Count == 0)
//            return "None";

//        return string.Join(" | ", beds.GroupBy(static x => x.Value).Where(static x => x.Key != TerrariaTeam.None).Select(static x => $"{x.Key}:{x.Count()}"));
//    }
//}
