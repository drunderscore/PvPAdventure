using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Game.GameReporters;

/// <summary>
/// PvPHub integration through Mod.Call only. This file must not use PvPHub-owned types.
/// </summary>
internal static class PvPHubCompat
{
    private const int RequiredApiVersion = 1;

    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static bool IsPvPHubLoaded => TryGetHub(out _);

    public static void LogMatchPostAuthPreflight()
    {
        bool isOfficial = IsOfficialServer();

        Log.Info(
            $"PvPHub match auth preflight: PvPHubLoaded={IsPvPHubLoaded}, NetMode={Main.netMode}, DedServ={Main.dedServ}, PvPHubOfficial={isOfficial}, ActivePlayers={CountActivePlayers()}");

        if (Main.netMode != NetmodeID.Server)
            Log.Warn("PvPHub match posting is disabled because this instance is not a dedicated server.");

        if (!isOfficial)
            Log.Warn("PvPHub Steam IDs are unavailable because this server is not official. Start the dedicated server with -official to enable PvPHub server authentication.");
    }

    public static bool TryGetSteamId(Player player, out ulong steamId)
    {
        steamId = 0;

        if (player?.active != true || Main.netMode != NetmodeID.Server || !TryGetHub(out Mod hub))
            return false;

        try
        {
            object result = hub.Call("Auth.GetSteamId", player.whoAmI);
            if (result is not ulong id || id == 0 || id > long.MaxValue)
            {
                Log.Warn($"PvPHub Steam ID lookup failed. Player={DescribePlayer(player)}, Result={result ?? "<null>"}");
                return false;
            }

            steamId = id;
            Log.Debug($"PvPHub Steam ID lookup succeeded. Player={DescribePlayer(player)}, SteamId={steamId}");
            return true;
        }
        catch (Exception ex)
        {
            Log.Warn($"PvPHub Steam ID lookup failed. Player={DescribePlayer(player)}, Error={ex}");
            return false;
        }
    }

    public static Task<PvPHubCallResult> PostMatchAsync(
        string payloadJson,
        string replayFilePath = null,
        CancellationToken cancellationToken = default)
    {
        return CallAsync("Match.Post", cancellationToken, payloadJson, replayFilePath);
    }

    public static Task<PvPHubCallResult> ProgressAchievementAsync(
        ulong steamId,
        string achievementName,
        string gameMode,
        uint delta = 1,
        CancellationToken cancellationToken = default)
    {
        return CallAsync("Achievement.Progress", cancellationToken, steamId, achievementName, gameMode, delta);
    }

    private static async Task<PvPHubCallResult> CallAsync(
        string command,
        CancellationToken cancellationToken,
        params object[] commandArguments)
    {
        if (!TryGetHub(out Mod hub))
            return PvPHubCallResult.Failure("PvPHub Mod.Call API v1 is not available.");

        try
        {
            int extraArguments = cancellationToken.CanBeCanceled ? 1 : 0;
            object[] args = new object[commandArguments.Length + 1 + extraArguments];
            args[0] = command;
            Array.Copy(commandArguments, 0, args, 1, commandArguments.Length);
            if (cancellationToken.CanBeCanceled)
                args[^1] = cancellationToken;

            object call = hub.Call(args);
            if (call is not Task<string> responseTask)
                return PvPHubCallResult.Failure($"PvPHub returned an unexpected result for {command}.");

            string responseJson = await responseTask.ConfigureAwait(false);
            return JsonSerializer.Deserialize<PvPHubCallResult>(responseJson, JsonOptions)
                ?? PvPHubCallResult.Failure($"PvPHub returned an empty result for {command}.");
        }
        catch (Exception ex)
        {
            return PvPHubCallResult.Failure(ex.Message);
        }
    }

    private static bool TryGetHub(out Mod hub)
    {
        hub = null;

        if (!ModLoader.TryGetMod("PvPHub", out Mod candidate))
            return false;

        try
        {
            if (candidate.Call("Api.Version") is not int version || version < RequiredApiVersion)
                return false;
        }
        catch
        {
            return false;
        }

        hub = candidate;
        return true;
    }

    private static bool IsOfficialServer()
    {
        if (!TryGetHub(out Mod hub))
            return false;

        try
        {
            return hub.Call("Auth.IsOfficial") is true;
        }
        catch
        {
            return false;
        }
    }

    private static int CountActivePlayers()
    {
        int count = 0;
        foreach (Player player in Main.ActivePlayers)
            if (player?.active == true)
                count++;
        return count;
    }

    private static string DescribePlayer(Player player)
    {
        string clientState = "";
        if (Main.netMode == NetmodeID.Server && player.whoAmI >= 0 && player.whoAmI < Netplay.Clients.Length)
        {
            RemoteClient client = Netplay.Clients[player.whoAmI];
            clientState = $", ClientActive={client?.IsActive}, ClientState={client?.State}";
        }

        return $"Name={player.name}, WhoAmI={player.whoAmI}, Active={player.active}, Team={player.team}{clientState}";
    }
}

internal sealed record PvPHubCallResult(
    bool Success,
    int StatusCode,
    JsonElement Data,
    string Error,
    string RequestSummary)
{
    public static PvPHubCallResult Failure(string error) => new(false, 0, default, error, "");

    public bool TryGetDataInt64(string propertyName, out long value)
    {
        value = 0;
        return Data.ValueKind == JsonValueKind.Object
            && Data.TryGetProperty(propertyName, out JsonElement property)
            && property.TryGetInt64(out value);
    }

    public bool TryGetDataUInt32(string propertyName, out uint value)
    {
        value = 0;
        return Data.ValueKind == JsonValueKind.Object
            && Data.TryGetProperty(propertyName, out JsonElement property)
            && property.TryGetUInt32(out value);
    }
}
