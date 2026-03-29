using Steamworks;
using System;
using Terraria.ModLoader;

namespace PvPAdventure.Common.MainMenu.Shop;

public class SteamAuthSystem : ModSystem
{
    private Callback<GetTicketForWebApiResponse_t>? _authTicketCallback;
    private HAuthTicket _ticketHandle = HAuthTicket.Invalid;

    // Your ShopApi will read this
    public static string? AuthTicketHex { get; private set; }

    // Helper to easily grab the local player's SteamID
    public static CSteamID ClientSteamId => SteamUser.GetSteamID();

    public override void Load()
    {
        // Don't try this on dedicated servers or if Steam isn't running
        if (!SteamAPI.IsSteamRunning())
            return;

        // 1. Set up the callback listener
        _authTicketCallback = Callback<GetTicketForWebApiResponse_t>.Create(OnGetTicketForWebApiResponse);

        // 2. Try to create the auth ticket (Only do this ONCE)
        _ticketHandle = SteamUser.GetAuthTicketForWebApi("api.tpvpa.terraria.sh");
    }

    private void OnGetTicketForWebApiResponse(GetTicketForWebApiResponse_t param)
    {
        // 3. Check if this callback matches our handle
        if (param.m_hAuthTicket != _ticketHandle)
            return;

        // 4. Ensure it was successful
        if (param.m_eResult == EResult.k_EResultOK)
        {
            // 5. Convert the byte array into a Hex String
            byte[] ticketData = new byte[param.m_cubTicket];
            Array.Copy(param.m_rgubTicket, ticketData, param.m_cubTicket);

            AuthTicketHex = BitConverter.ToString(ticketData).Replace("-", "").ToLowerInvariant();

            Log.Info("Successfully obtained Steam WebAPI auth ticket.");
        }
        else
        {
            Log.Error($"Failed to get Steam WebAPI ticket. Result: {param.m_eResult}");
        }
    }

    public override void Unload()
    {
        // 6. ALWAYS cancel the ticket on unload, even if we didn't get a callback yet
        if (_ticketHandle != HAuthTicket.Invalid)
        {
            SteamUser.CancelAuthTicket(_ticketHandle);
            _ticketHandle = HAuthTicket.Invalid;
        }

        if (_authTicketCallback != null)
        {
            _authTicketCallback.Dispose();
            _authTicketCallback = null;
        }

        AuthTicketHex = null;
    }
}