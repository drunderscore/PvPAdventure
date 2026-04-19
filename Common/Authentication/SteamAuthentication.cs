using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Steamworks;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Authentication;

/// <summary>
/// Implementation of client and server authentication mechanisms with Steam.
/// </summary>
public class SteamAuthentication : ModSystem
{
#if DEBUG
    //private const string WebTicketIdentity = "dev.api.tpvpa.terraria.sh";
    private const string WebTicketIdentity = "api.tpvpa.terraria.sh";
#else
    private const string WebTicketIdentity = "api.tpvpa.terraria.sh";
#endif

    private Callback<GetTicketForWebApiResponse_t> ticketForWebApiCallback;
    private Callback<ValidateAuthTicketResponse_t> validateAuthTicketCallback;

    private Callback<SteamServersConnected_t> steamServersConnectedCallback;
    private Callback<SteamServersDisconnected_t> steamServersDisconnectedCallback;
    private Callback<SteamServerConnectFailure_t> steamServerConnectFailureCallback;

    // Used for web API authentication by this client (on the backend API tavernkeep).
    private HAuthTicket webTicketHandle = HAuthTicket.Invalid;

    // Used for game session authentication by this client on a server.
    private HAuthTicket multiplayerTicketHandle = HAuthTicket.Invalid;

    public delegate void AuthenticationResponseCallback(ulong id, byte whoAmI, EAuthSessionResponse authSessionResponse,
        bool alreadyOk);

    private class Authentication(byte who, AuthenticationResponseCallback callback)
    {
        public byte Who { get; init; } = who;
        public AuthenticationResponseCallback Callback { get; init; } = callback;
        public bool Ok { get; set; }
    }

    // Authentication updates for this user. Removed upon first failure reported.
    private readonly IDictionary<ulong, Authentication> authentication = new Dictionary<ulong, Authentication>();

    public static CSteamID ClientSteamId => SteamUser.GetSteamID();

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public string WebTicket { get; private set; }

    public override void Load()
    {
        if (Main.dedServ)
        {
            if (!GameServer.Init(0, 7775, 7774, EServerMode.eServerModeAuthentication, Main.versionNumber))
                throw new Exception("Failed to initialize Steam for game server");

            On_Main.Update += OnMainUpdate;

            SteamGameServer.SetGameDescription("PvP Adventure");
            SteamGameServer.SetProduct("tModLoader");
            SteamGameServer.LogOnAnonymous();

            validateAuthTicketCallback =
                Callback<ValidateAuthTicketResponse_t>.CreateGameServer(OnValidateAuthTicketResponse);

            steamServersConnectedCallback = Callback<SteamServersConnected_t>.CreateGameServer(OnSteamServersConnected);
            steamServersDisconnectedCallback =
                Callback<SteamServersDisconnected_t>.CreateGameServer(OnSteamServersDisconnected);
            steamServerConnectFailureCallback =
                Callback<SteamServerConnectFailure_t>.CreateGameServer(OnSteamServerConnectFailure);
        }
        else
        {
            ticketForWebApiCallback = Callback<GetTicketForWebApiResponse_t>.Create(OnGetTicketForWebApiResponse);

            steamServersConnectedCallback = Callback<SteamServersConnected_t>.Create(OnSteamServersConnected);
            steamServersDisconnectedCallback = Callback<SteamServersDisconnected_t>.Create(OnSteamServersDisconnected);
            steamServerConnectFailureCallback =
                Callback<SteamServerConnectFailure_t>.Create(OnSteamServerConnectFailure);

            webTicketHandle = SteamUser.GetAuthTicketForWebApi(WebTicketIdentity);
            Log.Debug("Requested Steam web ticket");

            Netplay.OnDisconnect += OnDisconnect;
        }
    }

    public override void Unload()
    {
        if (webTicketHandle != HAuthTicket.Invalid)
        {
            SteamUser.CancelAuthTicket(webTicketHandle);
            WebTicket = null;
            webTicketHandle = HAuthTicket.Invalid;
        }

        if (multiplayerTicketHandle != HAuthTicket.Invalid)
        {
            SteamUser.CancelAuthTicket(multiplayerTicketHandle);
            multiplayerTicketHandle = HAuthTicket.Invalid;
        }

        if (validateAuthTicketCallback != null)
        {
            validateAuthTicketCallback.Dispose();
            validateAuthTicketCallback = null;
        }

        if (ticketForWebApiCallback != null)
        {
            ticketForWebApiCallback.Dispose();
            ticketForWebApiCallback = null;
        }

        if (steamServersConnectedCallback != null)
        {
            steamServersConnectedCallback.Dispose();
            steamServersConnectedCallback = null;
        }

        if (steamServersDisconnectedCallback != null)
        {
            steamServersDisconnectedCallback.Dispose();
            steamServersDisconnectedCallback = null;
        }

        if (steamServerConnectFailureCallback != null)
        {
            steamServerConnectFailureCallback.Dispose();
            steamServerConnectFailureCallback = null;
        }

        if (Main.dedServ)
        {
            foreach (var (id, _) in authentication)
                SteamGameServer.EndAuthSession(new(id));

            authentication.Clear();

            GameServer.Shutdown();
        }
        else
        {
            Netplay.OnDisconnect -= OnDisconnect;
        }
    }

    public byte[] BeginMultiplayerSession()
    {
        const int ticketMaxLength = 1024;
        var ticket = new byte[ticketMaxLength];

        // FIXME: What should this identity be? how is it verified? what the heck is this?
        var identity = new SteamNetworkingIdentity();
        identity.Clear();

        multiplayerTicketHandle =
            SteamUser.GetAuthSessionTicket(ticket, ticketMaxLength, out var ticketLength, ref identity);

        if (multiplayerTicketHandle == HAuthTicket.Invalid)
        {
            Log.Error("Steam provided an invalid session ticket!");
            return null;
        }

        if (ticketLength == 0)
        {
            Log.Error("Steam provided a zero-length session ticket?");
            return null;
        }

        Array.Resize(ref ticket, (int)ticketLength);

        Log.Debug("Steam session ticket created");

        return ticket;
    }

    public bool BeginMultiplayerSessionWith(byte whoAmI, ulong id, string ticket,
        AuthenticationResponseCallback callback)
    {
        if (!Main.dedServ)
            return false;

        var ticketData = Convert.FromHexString(ticket);
        if (SteamGameServer.BeginAuthSession(ticketData, ticketData.Length, new(id)) !=
            EBeginAuthSessionResult.k_EBeginAuthSessionResultOK)
            return false;

        Log.Debug(
            $"{whoAmI}/{Netplay.Clients[whoAmI].Socket.GetRemoteAddress().GetIdentifier()} is wishing to authenticate as {id}");

        authentication[id] = new(whoAmI, callback);

        return true;
    }

    public void EndMultiplayerSessionWith(byte whoAmI)
    {
        if (!Main.dedServ)
            return;

        foreach (var kv in authentication)
        {
            if (kv.Value.Who == whoAmI)
            {
                Log.Debug($"Ending Steam auth session with {kv.Key}/{whoAmI}");
                SteamGameServer.EndAuthSession(new(kv.Key));
                authentication.Remove(kv);
                break;
            }
        }
    }

    public ulong? GetAuthenticatedIdentity(byte whoAmI)
    {
        if (!Main.dedServ)
        {
            Log.Debug("Attempt to query authentication on the client (wrong side!)");
            return null;
        }

        foreach (var kv in authentication)
        {
            if (kv.Value.Who == whoAmI)
            {
                if (kv.Value.Ok)
                    return kv.Key;

                return null;
            }
        }

        return null;
    }

    private void OnGetTicketForWebApiResponse(GetTicketForWebApiResponse_t param)
    {
        if (param.m_hAuthTicket == webTicketHandle)
        {
            if (param.m_eResult != EResult.k_EResultOK)
            {
                Log.Error($"Failed to obtain Steam web ticket ({param.m_eResult})");
                return;
            }

            WebTicket = Convert.ToHexString(param.m_rgubTicket[..param.m_cubTicket]);
            Log.Debug("Obtained Steam web ticket");
        }
    }

    private void OnValidateAuthTicketResponse(ValidateAuthTicketResponse_t param)
    {
        Log.Debug($"Steam ticket validation response for {param.m_SteamID}: {param.m_eAuthSessionResponse}");

        try
        {
            if (authentication.TryGetValue(param.m_SteamID.m_SteamID, out var info))
            {
                info.Callback(param.m_SteamID.m_SteamID, info.Who, param.m_eAuthSessionResponse, info.Ok);

                if (param.m_eAuthSessionResponse == EAuthSessionResponse.k_EAuthSessionResponseOK)
                    info.Ok = true;
                else
                    authentication.Remove(param.m_SteamID.m_SteamID);
            }
        }
        catch (Exception e)
        {
            throw new Exception("Steam ticket validation unexpected", e);
        }
    }

    private void OnDisconnect()
    {
        if (multiplayerTicketHandle != HAuthTicket.Invalid)
        {
            Log.Info("Canceling Steam session ticket");
            SteamUser.CancelAuthTicket(multiplayerTicketHandle);
            multiplayerTicketHandle = HAuthTicket.Invalid;
        }
    }

    private void OnSteamServerConnectFailure(SteamServerConnectFailure_t param)
    {
        var msg = $"Failed to connect to Steam ({param.m_eResult}, retrying: {param.m_bStillRetrying})";

        Log.Error(msg);
        if (Main.dedServ)
            Console.WriteLine(msg);
    }

    private void OnSteamServersDisconnected(SteamServersDisconnected_t param)
    {
        var msg = $"Lost connection to Steam servers ({param.m_eResult})";

        Log.Error(msg);
        if (Main.dedServ)
            Console.WriteLine(msg);
    }

    private void OnSteamServersConnected(SteamServersConnected_t param)
    {
        const string msg = "Connection to Steam servers established.";

        Log.Info(msg);
        if (Main.dedServ)
            Console.WriteLine(msg);
    }

    private void OnMainUpdate(On_Main.orig_Update orig, Main self, GameTime gameTime)
    {
        GameServer.RunCallbacks();
        orig(self, gameTime);
    }
}