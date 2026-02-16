using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Discord.Systems;

[Autoload(Side = ModSide.Both)]
public sealed class DiscordIdentity : ModSystem
{
    public readonly struct Identity(string displayName, ulong userId)
    {
        public string DisplayName { get; } = displayName;
        public ulong UserId { get; } = userId;
    }

    private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
    private static Identity? cachedIdentity;

    public static bool TryGetIdentity(out Identity identity)
    {
        if (cachedIdentity.HasValue)
        {
            identity = cachedIdentity.Value;
            return true;
        }

        identity = default;
        return false;
    }

    public override void OnWorldUnload() => cachedIdentity = null;

    public override void OnWorldLoad()
    {
        if (Main.netMode != NetmodeID.MultiplayerClient)
            return;

        // We deliberately do NOT force a token request here; SSC/Join flow will call CacheAsync(token).
    }

    public static async Task CacheAsync(string token)
    {
        if (cachedIdentity.HasValue)
            return;

        var identity = await FetchAsync(token).ConfigureAwait(false);
        if (!identity.HasValue)
            return;

        cachedIdentity = identity.Value;
        Log.Debug("Discord identity cached: " + cachedIdentity.Value.DisplayName);
    }

    public static async Task<Identity?> FetchAsync(string token)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, "https://discord.com/api/v10/users/@me");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var res = await Http.SendAsync(req).ConfigureAwait(false);
            if (!res.IsSuccessStatusCode)
            {
                Log.Debug("Discord /users/@me HTTP " + (int)res.StatusCode);
                return null;
            }

            using var json = JsonDocument.Parse(await res.Content.ReadAsStringAsync().ConfigureAwait(false));
            var root = json.RootElement;

            if (!ulong.TryParse(root.GetProperty("id").GetString(), out var id) || id == 0)
                return null;

            string displayName = null;

            if (root.TryGetProperty("global_name", out var gn) && gn.ValueKind == JsonValueKind.String)
                displayName = gn.GetString();

            if (string.IsNullOrWhiteSpace(displayName))
                displayName = root.GetProperty("username").GetString();

            if (string.IsNullOrWhiteSpace(displayName))
                return null;

            return new Identity(displayName, id);
        }
        catch (Exception e)
        {
            Log.Debug("Discord identity fetch failed: " + e.GetType().Name);
            return null;
        }
    }
}
