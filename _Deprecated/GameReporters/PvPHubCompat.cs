//using System;
//using Terraria;
//using Terraria.ID;
//using Terraria.ModLoader;

//namespace PvPAdventure.Common.Game.GameReporters;

//[JITWhenModsEnabled("PvPHub")]
//[ExtendsFromMod("PvPHub")]
//public class PvPHubCompat
//{
//    public static bool IsPvPHubLoaded => ModLoader.TryGetMod("PvPHub", out _);

//    public static void LogMatchPostAuthPreflight()
//    {
//        bool isOfficial = IsOfficialServer();

//        Log.Info(
//            $"PvPHub match auth preflight: PvPHubLoaded={IsPvPHubLoaded}, NetMode={Main.netMode}, DedServ={Main.dedServ}, PvPHubOfficial={isOfficial}, ActivePlayers={CountActivePlayers()}");

//        if (Main.netMode != NetmodeID.Server)
//            Log.Warn("PvPHub match posting is disabled because this instance is not a dedicated server.");

//        if (!isOfficial)
//            Log.Warn("PvPHub Steam IDs are unavailable because this server is not official. Start the dedicated server with -official to enable PvPHub server authentication.");
//    }

//    public static bool TryGetSteamId(Player player, out ulong steamId)
//    {
//        steamId = 0;

//        if (player == null || !player.active)
//            return false;

//        if (Main.netMode != NetmodeID.Server)
//        {
//            Log.Debug($"PvPHub Steam ID lookup skipped outside dedicated server. Player={DescribePlayer(player)}");
//            return false;
//        }

//        if (!IsOfficialServer())
//        {
//            Log.Warn($"PvPHub Steam ID lookup failed: server is not official (-official missing). Player={DescribePlayer(player)}");
//            return false;
//        }

//        try
//        {
//            ulong? id = ModContent.GetInstance<PvPHub.Common.Authentication.SteamAuthentication>()
//                .GetAuthenticatedIdentity((byte)player.whoAmI);
//            if (!id.HasValue || id.Value == 0 || id.Value > long.MaxValue)
//            {
//                Log.Warn($"PvPHub Steam ID lookup failed: no accepted Steam auth identity for player. Player={DescribePlayer(player)}, SteamId={id?.ToString() ?? "<null>"}");
//                return false;
//            }

//            steamId = id.Value;
//            Log.Debug($"PvPHub Steam ID lookup succeeded. Player={DescribePlayer(player)}, SteamId={steamId}");
//            return true;
//        }
//        catch (Exception ex)
//        {
//            Log.Warn($"Failed to get PvPHub Steam ID. Player={DescribePlayer(player)}, Error={ex}");
//            return false;
//        }
//    }

//    private static bool IsOfficialServer()
//    {
//        try
//        {
//            return Main.dedServ && global::PvPHub.PvPHub.IsOfficial;
//        }
//        catch (Exception ex)
//        {
//            Log.Warn($"Failed to read PvPHub official-server state: {ex.Message}");
//            return false;
//        }
//    }

//    private static int CountActivePlayers()
//    {
//        int count = 0;
//        foreach (Player player in Main.ActivePlayers)
//        {
//            if (player?.active == true)
//                count++;
//        }

//        return count;
//    }

//    private static string DescribePlayer(Player player)
//    {
//        if (player == null)
//            return "<null>";

//        string clientState = "";
//        if (Main.netMode == NetmodeID.Server && player.whoAmI >= 0 && player.whoAmI < Netplay.Clients.Length)
//        {
//            var client = Netplay.Clients[player.whoAmI];
//            clientState = $", ClientActive={client?.IsActive}, ClientState={client?.State}";
//        }

//        return $"Name={player.name}, WhoAmI={player.whoAmI}, Active={player.active}, Team={player.team}{clientState}";
//    }
//}
