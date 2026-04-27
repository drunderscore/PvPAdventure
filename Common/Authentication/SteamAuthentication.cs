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
    private const string WebTicketIdentity = "api.tpvpa.terraria.sh";

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
        On_Main.Update += OnMainUpdate;

        if (Main.dedServ)
        {
            if (!GameServer.Init(0, 7775, 7774, EServerMode.eServerModeAuthentication, Main.versionNumber))
                throw new Exception("Failed to initialize Steam for game server");

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

    private void OnMainUpdate(On_Main.orig_Update orig, Main self, GameTime gameTime)
    {
        if (Main.dedServ)
        {
            GameServer.RunCallbacks();
        }
        else
        {
            // Needed?
            //SteamAPI.RunCallbacks();
        }

        orig(self, gameTime);
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
        ulong steamId = param.m_SteamID.m_SteamID;
        Log.Debug($"Steam auth callback: steamId={steamId}, response={param.m_eAuthSessionResponse}");

        try
        {
            if (!authentication.TryGetValue(steamId, out var info))
            {
                Log.Warn($"Steam auth callback ignored: no pending session for steamId={steamId}");
                return;
            }

            bool wasAlreadyOk = info.Ok;
            bool isOk = param.m_eAuthSessionResponse == EAuthSessionResponse.k_EAuthSessionResponseOK;

            if (isOk)
            {
                info.Ok = true;
                Log.Info($"Steam auth accepted: steamId={steamId}, whoAmI={info.Who}, alreadyOk={wasAlreadyOk}");
            }
            else
            {
                authentication.Remove(steamId);
                Log.Warn($"Steam auth rejected: steamId={steamId}, whoAmI={info.Who}, response={param.m_eAuthSessionResponse}");
            }

            info.Callback(steamId, info.Who, param.m_eAuthSessionResponse, wasAlreadyOk);
        }
        catch (Exception e)
        {
            throw new Exception("Steam ticket validation unexpected", e);
        }
    }

    #region Multiplayer auth
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

    public bool BeginMultiplayerSessionWith(byte whoAmI, ulong id, string ticket, AuthenticationResponseCallback callback)
    {
        if (!Main.dedServ)
            return false;

        if (string.IsNullOrWhiteSpace(ticket))
        {
            Log.Warn($"Steam auth rejected before BeginAuthSession: empty ticket, whoAmI={whoAmI}, claimedSteamId={id}");
            return false;
        }

        byte[] ticketData;

        try
        {
            ticketData = Convert.FromHexString(ticket);
        }
        catch (Exception e)
        {
            Log.Warn($"Steam auth rejected before BeginAuthSession: invalid hex ticket, whoAmI={whoAmI}, claimedSteamId={id}, error={e.Message}");
            return false;
        }

        var remote = Netplay.Clients[whoAmI].Socket.GetRemoteAddress().GetIdentifier();

        Log.Info($"Steam auth begin: whoAmI={whoAmI}, claimedSteamId={id}, remote={remote}, ticketBytes={ticketData.Length}");

        var result = SteamGameServer.BeginAuthSession(ticketData, ticketData.Length, new CSteamID(id));

        if (result != EBeginAuthSessionResult.k_EBeginAuthSessionResultOK)
        {
            Log.Warn($"Steam auth BeginAuthSession failed: whoAmI={whoAmI}, claimedSteamId={id}, result={result}");
            return false;
        }

        authentication[id] = new(whoAmI, callback);

        Log.Debug($"Steam auth pending: whoAmI={whoAmI}, claimedSteamId={id}, remote={remote}");

        return true;
    }

    public void EndMultiplayerSessionWith(byte whoAmI)
    {
        if (!Main.dedServ)
            return;

        ulong? steamId = null;

        foreach (var kv in authentication)
        {
            if (kv.Value.Who == whoAmI)
            {
                steamId = kv.Key;
                break;
            }
        }

        if (!steamId.HasValue)
        {
            Log.Debug($"Steam auth end skipped: no session for whoAmI={whoAmI}");
            return;
        }

        EndMultiplayerSessionWith(steamId.Value);
    }

    public void EndMultiplayerSessionWith(ulong steamId)
    {
        if (!Main.dedServ)
            return;

        if (!authentication.Remove(steamId))
        {
            Log.Debug($"Steam auth end skipped: no session for steamId={steamId}");
            return;
        }

        SteamGameServer.EndAuthSession(new CSteamID(steamId));
        Log.Info($"Steam auth ended: steamId={steamId}");
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
    #endregion

    #region Unload
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
    #endregion
}