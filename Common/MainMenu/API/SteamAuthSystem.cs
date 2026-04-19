using Steamworks;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.MainMenu.API;

[Autoload(Side = ModSide.Client)]
public sealed class SteamAuthSystem : ModSystem
{
    private Callback<GetTicketForWebApiResponse_t>? authTicketCallback;
    private HAuthTicket ticketHandle = HAuthTicket.Invalid;
    private static bool steamAvailable;

    public static string? AuthTicketHex { get; private set; }
    public static bool HasTicket => !string.IsNullOrWhiteSpace(AuthTicketHex);
    public static CSteamID? ClientSteamId => steamAvailable ? SteamUser.GetSteamID() : null;

    public override void Load()
    {
        steamAvailable = CanUseSteam();
        if (!steamAvailable)
            return;

        authTicketCallback = Callback<GetTicketForWebApiResponse_t>.Create(OnGetTicketForWebApiResponse);
        ticketHandle = SteamUser.GetAuthTicketForWebApi("api.tpvpa.terraria.sh");
    }

    public override void Unload()
    {
        if (steamAvailable && ticketHandle != HAuthTicket.Invalid)
        {
            SteamUser.CancelAuthTicket(ticketHandle);
            ticketHandle = HAuthTicket.Invalid;
        }

        authTicketCallback?.Dispose();
        authTicketCallback = null;
        AuthTicketHex = null;
        steamAvailable = false;
    }

    private static bool CanUseSteam()
    {
        if (Main.netMode == NetmodeID.Server)
            return false;

        try
        {
            return SteamAPI.IsSteamRunning();
        }
        catch (DllNotFoundException)
        {
            Log.Warn("steam_api was not found. Steam auth is disabled.");
            return false;
        }
        catch (BadImageFormatException)
        {
            Log.Warn("steam_api architecture mismatch. Steam auth is disabled.");
            return false;
        }
        catch (Exception ex)
        {
            Log.Warn($"Steam auth unavailable: {ex.Message}");
            return false;
        }
    }

    private void OnGetTicketForWebApiResponse(GetTicketForWebApiResponse_t param)
    {
        if (param.m_hAuthTicket != ticketHandle)
            return;

        if (param.m_eResult != EResult.k_EResultOK)
        {
            Log.Error($"Failed to get Steam Web API ticket. Result: {param.m_eResult}");
            return;
        }

        byte[] ticketData = new byte[param.m_cubTicket];
        Array.Copy(param.m_rgubTicket, ticketData, param.m_cubTicket);
        AuthTicketHex = BitConverter.ToString(ticketData).Replace("-", "").ToLowerInvariant();

        Log.Info("Successfully obtained Steam Web API auth ticket.");
    }
}