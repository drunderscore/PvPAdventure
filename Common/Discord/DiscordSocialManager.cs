using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Discord;

public class DiscordSocialManager : ModSystem
{
    private string verifier;

    [StructLayout(LayoutKind.Sequential)]
    private record struct DiscordString(nint Ptr, ulong Size)
    {
        public override string ToString()
        {
            return Ptr == nint.Zero ? null : Marshal.PtrToStringAnsi(Ptr, (int)Size);
        }
    }

    private enum ClientStatus
    {
        Disconnected = 0,
        Connecting = 1,
        Connected = 2,
        Ready = 3,
        Reconnecting = 4,
        Disconnecting = 5,
        HttpWait = 6,
    }

    private enum LoggingSeverity
    {
        Verbose = 1,
        Info = 2,
        Warning = 3,
        Error = 4,
        None = 5,
    }

    public enum AuthorizationTokenType
    {
        User = 0,
        Bearer = 1
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ClientOnStatusChanged(ClientStatus status, int error, int errorDetail, nint userData);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ClientLogCallback(DiscordString message, LoggingSeverity sev, nint userData);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ClientAuthorizationCallback(ref nint result, DiscordString code, DiscordString redirectUri,
        ref nint userData);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void TokenExchangeCallback(ref nint result, DiscordString accessToken, DiscordString refreshToken,
        AuthorizationTokenType tokenType, int expiresIn, DiscordString scopes, nint userData);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void UpdateTokenCallback(ref nint result, nint userData);

    private const string OpenIdScopes = "openid identify";
    private nint client = nint.Zero;
    private HttpClient http = new();

    public record struct User(
        [property: JsonPropertyName("id")] ulong Id,
        [property: JsonPropertyName("username")]
        string Username,
        [property: JsonPropertyName("global_name")]
        string GlobalName);

    public User? CurrentUser { get; private set; }

    public override void Load()
    {
        if (!Main.dedServ)
        {
            On_Main.Update += OnMainUpdate;

            Discord_Client_Init(out client);

            if (client == nint.Zero)
            {
                Log.Error("Failed to create Discord Social SDK client");
                return;
            }

            Discord_Client_AddLogCallback(ref client, OnLog, nint.Zero, nint.Zero, LoggingSeverity.Verbose);
            Discord_Client_SetStatusChangedCallback(ref client, OnStatusChanged, nint.Zero, nint.Zero);

            Authorize();
        }
    }

    public override void Unload()
    {
        if (client != nint.Zero)
        {
            Discord_Client_Drop(ref client);
            client = nint.Zero;
        }
    }

    public void Authorize()
    {
        if (client == nint.Zero)
            return;

        Discord_Client_CreateAuthorizationCodeVerifier(ref client, out var codeVerifier);
        Discord_AuthorizationArgs_Init(out var authorizationArgs);

        Discord_AuthorizationArgs_SetClientId(ref authorizationArgs, 1298376502999646238);

        {
            var unmanagedScopes = Marshal.StringToHGlobalAnsi(OpenIdScopes);
            try
            {
                Discord_AuthorizationArgs_SetScopes(ref authorizationArgs,
                    new(unmanagedScopes, (ulong)OpenIdScopes.Length));
            }
            finally
            {
                Marshal.FreeHGlobal(unmanagedScopes);
            }
        }

        Discord_AuthorizationCodeVerifier_Challenge(ref codeVerifier, out var codeVerifierChallenge);

        Discord_AuthorizationArgs_SetCodeChallenge(ref authorizationArgs, ref codeVerifierChallenge);

        Discord_AuthorizationCodeVerifier_Verifier(ref codeVerifier, out var verifierRaw);
        verifier = verifierRaw.ToString();

        Discord_Client_Authorize(ref client, ref authorizationArgs, OnAuthorization, nint.Zero, nint.Zero);
    }

    private async void CacheLocalFromToken(string accessToken)
    {
        try
        {
            http.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);

            CurrentUser = await http.GetFromJsonAsync<User>("https://discord.com/api/v10/users/@me");
            Log.Debug(CurrentUser);
        }
        catch (HttpRequestException e)
        {
            Log.Warn($"Unsuccessful in requesting Discord user {e}");
        }
        catch (Exception e)
        {
            Log.Error($"Unexpected error using Discord token {e}");
        }
    }

    private static void OnLog(DiscordString message, LoggingSeverity sev, nint userData)
    {
        Log.Debug($"Discord/{sev}: {message}");
    }

    private static void OnStatusChanged(ClientStatus status, int error, int errorDetail, nint userData)
    {
        Log.Debug($"Discord client status changed: {status} (error {error}, detail {errorDetail})");
    }

    private static void OnAuthorization(ref nint result, DiscordString code, DiscordString redirectUri,
        ref nint userData)
    {
        var success = Discord_ClientResult_Successful(ref result);
        Log.Debug($"Authorization result successful: {success}");

        if (success)
        {
            var discord = ModContent.GetInstance<DiscordSocialManager>();

            {
                var unmanagedVerifier = Marshal.StringToHGlobalAnsi(discord.verifier);
                try
                {
                    Discord_Client_GetToken(ref discord.client, 1298376502999646238, code,
                        new(unmanagedVerifier, (ulong)discord.verifier.Length), redirectUri, OnTokenExchange, nint.Zero,
                        nint.Zero);
                }
                finally
                {
                    Marshal.FreeHGlobal(unmanagedVerifier);
                }
            }
        }
    }

    private static void OnTokenExchange(ref nint result, DiscordString accessToken, DiscordString refreshToken,
        AuthorizationTokenType tokenType, int expiresIn, DiscordString scopes, nint userData)
    {
        var success = Discord_ClientResult_Successful(ref result);
        Log.Debug($"Token exchange result successful: {success}");

        if (success)
            ModContent.GetInstance<DiscordSocialManager>().CacheLocalFromToken(accessToken.ToString());
    }

    private void OnMainUpdate(On_Main.orig_Update orig, Main self, GameTime gameTime)
    {
        Discord_RunCallbacks();
        orig(self, gameTime);
    }

    [DllImport("Discord Social SDK", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
    private static extern void Discord_Client_Init(out nint self);

    [DllImport("Discord Social SDK", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
    private static extern void Discord_Client_Drop(ref nint self);

    [DllImport("Discord Social SDK", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
    private static extern void Discord_Client_Connect(ref nint self);

    [DllImport("Discord Social SDK", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
    private static extern void Discord_Client_Disconnect(ref nint self);

    [DllImport("Discord Social SDK", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
    private static extern ClientStatus Discord_Client_GetStatus(ref nint self);

    [DllImport("Discord Social SDK", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
    private static extern void Discord_Client_Authorize(ref nint self, ref nint args, ClientAuthorizationCallback cb,
        nint freeFn, nint userData);

    [DllImport("Discord Social SDK", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
    private static extern void Discord_Client_GetToken(ref nint self, ulong applicationId, DiscordString code,
        DiscordString codeVerifier, DiscordString redirectUri, TokenExchangeCallback cb, nint freeFn, nint userData);

    [DllImport("Discord Social SDK", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
    private static extern void Discord_Client_UpdateToken(ref nint self, AuthorizationTokenType tokenType,
        DiscordString token, UpdateTokenCallback cb, nint freeFn, nint userData);

    [DllImport("Discord Social SDK", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
    private static extern void Discord_RunCallbacks();

    [DllImport("Discord Social SDK", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
    private static extern void Discord_Client_AddLogCallback(ref nint client, ClientLogCallback cb, nint freeFn,
        nint userData, LoggingSeverity minSev);

    [DllImport("Discord Social SDK", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
    private static extern void Discord_Client_SetStatusChangedCallback(ref nint client, ClientOnStatusChanged cb,
        nint freeFn, nint userData);

    [DllImport("Discord Social SDK", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
    private static extern void Discord_Client_CreateAuthorizationCodeVerifier(ref nint client, out nint self);

    [DllImport("Discord Social SDK", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
    private static extern void Discord_AuthorizationCodeVerifier_Drop(ref nint self);

    [DllImport("Discord Social SDK", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
    private static extern void Discord_AuthorizationArgs_Init(out nint self);

    [DllImport("Discord Social SDK", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
    private static extern void Discord_AuthorizationArgs_Drop(ref nint self);

    [DllImport("Discord Social SDK", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
    private static extern void Discord_AuthorizationArgs_SetClientId(ref nint self, ulong value);

    [DllImport("Discord Social SDK", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
    private static extern void Discord_AuthorizationArgs_SetScopes(ref nint self, DiscordString value);

    [DllImport("Discord Social SDK", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
    private static extern void Discord_AuthorizationCodeVerifier_Challenge(ref nint self, out nint value);

    [DllImport("Discord Social SDK", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
    private static extern void Discord_AuthorizationArgs_SetCodeChallenge(ref nint self, ref nint value);

    [DllImport("Discord Social SDK", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
    private static extern void Discord_AuthorizationCodeVerifier_Verifier(ref nint self, out DiscordString value);

    [DllImport("Discord Social SDK", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static extern bool Discord_ClientResult_Successful(ref nint self);

    [DllImport("Discord Social SDK", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static extern bool Discord_Client_GetCurrentUserV2(ref nint self, out nint value);

    [DllImport("Discord Social SDK", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
    private static extern ulong Discord_UserHandle_Id(ref nint self);
}