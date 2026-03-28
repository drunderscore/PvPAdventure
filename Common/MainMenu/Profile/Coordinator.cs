using PvPAdventure.Common.MainMenu.Shop;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.MainMenu.Profile;

public class Coordinator : ModSystem
{
    private HttpClientHandler _httpClientHandler;
    private HAuthTicket _waitingForAuthTicket = HAuthTicket.Invalid;
    private Callback<GetTicketForWebApiResponse_t> _authTicketCallback;
    private TaskCompletionSource<HttpClient> _idk;

    // TODO: how to gather this? config?
    public const string OfficialDedicatedServerCertificatePath =
        "/home/amber/Documents/tavernkeep/pki/private/resin.p12";

    // Only auth tickets with this identity are valid for tavernkeep.
    public const string AuthTicketIdentity = "tpvpa.terraria.sh";

    private Task<HttpClient> Create(bool dedServ)
    {
        // TODO: wtf?
        _httpClientHandler = new HttpClientHandler();

        // TODO: this is just dev self-signed cert! DO NOT INCLUDE!
        _httpClientHandler.ServerCertificateCustomValidationCallback = (_, certificate, _, _) =>
            certificate?.Thumbprint == "37A797AFE27D3B2DFCF6A1458C19601290B721DE";

        if (dedServ)
        {
            if (OfficialDedicatedServerCertificatePath != null)
            {
                // TODO: How should I gather this path? config?
                // TODO: There should be ANNOYING WARNING when this certificate is close to expiry (2 weeks or less)
                _httpClientHandler.ClientCertificates.Add(
                    new X509Certificate2(OfficialDedicatedServerCertificatePath));
            }
        }
        else
        {
            _waitingForAuthTicket = SteamUser.GetAuthTicketForWebApi(AuthTicketIdentity);
            _idk = new();
            return _idk.Task;

            // _ticketHandle = SteamUser.GetAuthTicketForWebApi("tpvpa.terraria.sh");
            // Logger.Info($"Created ticket, handle {_ticketHandle}");
        }

        return Task.FromResult(new HttpClient(_httpClientHandler));
    }

    public override async void Load()
    {
        try
        {
            _authTicketCallback = Callback<GetTicketForWebApiResponse_t>.Create(OnGetTicketForWebApiResponse);

            var client = await Create(Main.dedServ);
            var res = await client.GetAsync($"https://tpvpa.terraria.sh/v1/profile/{SteamUser.GetSteamID()}");
            Log.Info(await res.Content.ReadAsStringAsync());
        }
        catch (Exception e)
        {
            Log.Warn("couldn't create client", e.Message);
        }
    }

    public override void Unload()
    {
        // TODO: Check all this!
        if (_waitingForAuthTicket != HAuthTicket.Invalid)
        {
            Log.Info($"canceling auth ticket handle {_waitingForAuthTicket}");
            SteamUser.CancelAuthTicket(_waitingForAuthTicket);
        }

        _authTicketCallback.Dispose();
    }

    private void OnGetTicketForWebApiResponse(GetTicketForWebApiResponse_t param)
    {
        // TODO: Check this also!!! null and reset and cancel and such!
        Log.Info($"Got GetTicketForWebApiResponse_t, handle {param.m_hAuthTicket}, result {param.m_eResult}");

        if (param.m_hAuthTicket == _waitingForAuthTicket)
        {
            if (param.m_eResult == EResult.k_EResultOK)
            {
                var ticket = new byte[param.m_cubTicket];
                Array.Copy(param.m_rgubTicket, ticket, ticket.Length);
                Log.Info($"Ticket handle {param.m_hAuthTicket} now has ticket: {Convert.ToHexString(ticket)}");

                var ticketHex = Convert.ToHexString(ticket);
                _idk.SetResult(new HttpClient(_httpClientHandler)
                {
                    DefaultRequestHeaders =
                    {
                        { "Ticket", ticketHex }
                    }
                });
            }
            else
            {
                Log.Error("WHAT THE ??");
                _idk.SetException(new Exception("somehow we got bad result for auth ticket response"));
            }
        }
    }

    public async Task SyncProfileDataFromServerAsync()
    {
        try
        {
            // 1. Get the authenticated client
            HttpClient client = await Create(Main.dedServ);
            var state = MainMenuProfileState.Instance;

            // 2. Fetch Profile (Gems & Equipment)
            var profileRes = await client.GetAsync("https://api.tpvpa.terraria.sh/profile/v1");
            if (profileRes.IsSuccessStatusCode)
            {
                string profileJson = await profileRes.Content.ReadAsStringAsync();
                var profileData = JsonSerializer.Deserialize<ApiProfileResponse>(profileJson);

                if (profileData != null)
                {
                    // Set Gems
                    state.SetGems(profileData.Gems);

                    // Set Equipped Items
                    if (profileData.Equipment != null)
                    {
                        foreach (var kvp in profileData.Equipment)
                        {
                            string prototype = kvp.Key;
                            string name = kvp.Value;

                            // Find the associated ItemType for this prototype by checking our catalog
                            var matchingProduct = Shop.Products.All.FirstOrDefault(p => p.Prototype == prototype);

                            if (matchingProduct.ItemType != 0) // If we found a valid matching product
                            {
                                state.SetEquippedSkin(matchingProduct.ItemType, new SkinIdentity(prototype, name));
                            }
                        }
                    }
                }
            }

            // 3. Fetch Inventory (Owned Skins)
            var inventoryRes = await client.GetAsync("https://api.tpvpa.terraria.sh/profile/inventory/v1");
            if (inventoryRes.IsSuccessStatusCode)
            {
                string inventoryJson = await inventoryRes.Content.ReadAsStringAsync();
                var inventoryData = JsonSerializer.Deserialize<List<ApiInventoryItem>>(inventoryJson);

                if (inventoryData != null)
                {
                    List<SkinIdentity> ownedSkins = new();
                    foreach (var item in inventoryData)
                    {
                        // Create the new SkinIdentity struct directly from the API response
                        ownedSkins.Add(new SkinIdentity(item.Prototype, item.Name));
                    }

                    state.SetOwnedSkins(ownedSkins);
                }
            }
            else
            {
                Log.Error($"Failed to fetch inventory: {inventoryRes.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Exception during profile sync: {ex.Message}");
        }
    }

    public async Task<bool> PurchaseProductAsync(SkinIdentity identity)
    {
        try
        {
            HttpClient client = await Create(Main.dedServ);

            var requestData = new ApiPurchaseRequest
            {
                Prototype = identity.Prototype,
                Name = identity.Name
            };

            string json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://api.tpvpa.terraria.sh/shop/v1", content);

            if (response.IsSuccessStatusCode)
            {
                Log.Info($"Successfully purchased {identity.Prototype}:{identity.Name}");
                return true;
            }
            else
            {
                string errorBody = await response.Content.ReadAsStringAsync();
                Log.Error($"Failed to purchase {identity.Prototype}:{identity.Name}. Status: {response.StatusCode}. Body: {errorBody}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Exception during purchase: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> UpdateEquipmentAsync(string prototype, string? name)
    {
        try
        {
            HttpClient client = await Create(Main.dedServ);

            var requestData = new ApiEquipmentUpdateRequest
            {
                Prototype = prototype,
                Name = name
            };

            string json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://api.tpvpa.terraria.sh/profile/equipment/v1", content);

            if (response.IsSuccessStatusCode)
            {
                Log.Info($"Successfully updated equipment for prototype {prototype} to name '{name ?? "NONE"}'");
                return true;
            }
            else
            {
                string errorBody = await response.Content.ReadAsStringAsync();
                Log.Error($"Failed to update equipment {prototype}. Status: {response.StatusCode}. Body: {errorBody}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Exception during equipment update: {ex.Message}");
            return false;
        }
    }
}