using PvPAdventure.Common.MainMenu.Profile;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Terraria;

namespace PvPAdventure.Common.MainMenu.Shop;

internal enum PurchaseResult
{
    Failed,
    Purchased,
    AlreadyOwned
}

internal static class ShopApi
{
    private const string BaseUrl = "https://jame.xyz:50000/";
    private const string DevThumbprint = "51A6F42F8479EDBB926C9E4385D7B8286A64C418";

    private static readonly HttpClient Client = CreateClient();

    private static HttpClient CreateClient()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, cert, _, _) =>
                cert != null &&
                string.Equals(
                    cert.Thumbprint?.Replace(" ", ""),
                    DevThumbprint,
                    StringComparison.OrdinalIgnoreCase)
        };

        return new HttpClient(handler)
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    private static async Task<HttpResponseMessage> SendAsync(HttpMethod method, string uri, object? body = null)
    {
        var request = new HttpRequestMessage(method, uri);

        if (!string.IsNullOrEmpty(SteamAuthSystem.AuthTicketHex))
            request.Headers.Add("Ticket", SteamAuthSystem.AuthTicketHex);

        if (body != null)
            request.Content = JsonContent.Create(body);

        return await Client.SendAsync(request);
    }

    public static async Task<string> GetShopJsonAsync()
    {
        using HttpResponseMessage response = await SendAsync(HttpMethod.Get, "shop/v1");
        string json = await response.Content.ReadAsStringAsync();

        Log.Info($"[Shop API Response] {json}");
        response.EnsureSuccessStatusCode();

        return json;
    }

    public static async Task<ApiProfileResponse?> GetProfileAsync()
    {
        string steamId = SteamAuthSystem.ClientSteamId.m_SteamID.ToString();

        using HttpResponseMessage response = await SendAsync(HttpMethod.Get, $"profile/v1?id={steamId}");
        string json = await response.Content.ReadAsStringAsync();

        Log.Info($"[Profile API Response] {json}");
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<ApiProfileResponse>(json);
    }

    public static async Task<List<ApiInventoryItem>> GetInventoryAsync()
    {
        using HttpResponseMessage response = await SendAsync(HttpMethod.Get, "profile/inventory/v1");
        string json = await response.Content.ReadAsStringAsync();

        Log.Info($"[Inventory API Response] {json}");
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<List<ApiInventoryItem>>(json) ?? [];
    }

    public static async Task RefreshProfileStateAsync()
    {
        Task<ApiProfileResponse?> profileTask = GetProfileAsync();
        Task<List<ApiInventoryItem>> inventoryTask = GetInventoryAsync();

        await Task.WhenAll(profileTask, inventoryTask);

        ApiProfileResponse? profile = profileTask.Result;
        List<ApiInventoryItem> inventory = inventoryTask.Result;

        Main.QueueMainThreadAction(() =>
        {
            MainMenuProfileState.Instance.SyncWithBackend(profile, inventory);
        });
    }

    public static async Task<PurchaseResult> PurchaseProductAsync(string prototype, string name)
    {
        var payload = new
        {
            prototype,
            name
        };

        try
        {
            using HttpResponseMessage response = await SendAsync(HttpMethod.Post, "shop/purchase/v1", payload);
            string responseText = await response.Content.ReadAsStringAsync();

            Log.Info($"[Purchase API] Status: {response.StatusCode} | Body: {responseText}");

            if (response.IsSuccessStatusCode)
                return PurchaseResult.Purchased;

            if (response.StatusCode == HttpStatusCode.Conflict)
                return PurchaseResult.AlreadyOwned;

            return PurchaseResult.Failed;
        }
        catch (Exception e)
        {
            Log.Error($"[Purchase API Error] Failed to reach server: {e}");
            return PurchaseResult.Failed;
        }
    }

    public static async Task<bool> UpdateEquipmentAsync(string prototype, string? name)
    {
        var payload = new
        {
            prototype,
            name
        };

        try
        {
            using HttpResponseMessage response = await SendAsync(HttpMethod.Post, "profile/equip/v1", payload);
            string responseText = await response.Content.ReadAsStringAsync();

            Log.Info($"[Equip API] Status: {response.StatusCode} | Body: {responseText}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception e)
        {
            Log.Error($"[Equip API Error] Failed to reach server: {e}");
            return false;
        }
    }
}