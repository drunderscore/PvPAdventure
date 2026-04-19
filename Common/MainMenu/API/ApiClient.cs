using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PvPAdventure.Common.MainMenu.API;

internal static class ApiClient
{
    //private const string BaseUrl = "https://jame.xyz:50000/";
    private const string BaseUrl = "https://api.tpvpa.terraria.sh/";
    private const string PinnedThumbprint = "51A6F42F8479EDBB926C9E4385D7B8286A64C418";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly HttpClient Client = CreateClient(useClientCertificate: false);
    private static readonly HttpClient MatchClient = CreateClient(useClientCertificate: true);

    public static Task<ApiResult<string>> GetStringAsync(string uri, CancellationToken cancellationToken = default)
    {
        return SendForStringAsync(HttpMethod.Get, uri, null, cancellationToken);
    }

    public static Task<ApiResult<string>> PostStringAsync(string uri, object? body = null, CancellationToken cancellationToken = default)
    {
        return SendForStringAsync(HttpMethod.Post, uri, body, cancellationToken);
    }

    public static Task<ApiResult<T>> GetJsonAsync<T>(string uri, CancellationToken cancellationToken = default)
    {
        return SendForJsonAsync<T>(HttpMethod.Get, uri, null, cancellationToken);
    }

    public static Task<ApiResult<T>> PostJsonAsync<T>(string uri, object? body = null, CancellationToken cancellationToken = default)
    {
        return SendForJsonAsync<T>(HttpMethod.Post, uri, body, cancellationToken);
    }

    private static async Task<ApiResult<string>> SendForStringAsync(HttpMethod method, string uri, object? body, CancellationToken cancellationToken)
    {
        try
        {
            using HttpRequestMessage request = new(method, uri);

            if (!string.IsNullOrWhiteSpace(SteamAuthSystem.AuthTicketHex))
                request.Headers.Add("Ticket", SteamAuthSystem.AuthTicketHex);

            if (body != null)
                request.Content = JsonContent.Create(body);

            HttpClient httpClient = ShouldUseMatchClient(method, uri) ? MatchClient : Client;

            using HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            string responseText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                string error;

                try
                {
                    error = BuildErrorMessage(response.StatusCode, response.ReasonPhrase, responseText);
                }
                catch (Exception ex)
                {
                    error = $"Failed to build error message: {ex.Message}";
                }

                Log.Warn($"[ApiClient] [API] {method} {uri} -> {(int)response.StatusCode} {response.StatusCode}. {error}");
                return ApiResult<string>.Error(response.StatusCode, error);
            }

            Log.Info($"[ApiClient] [API] {method} {uri} -> {(int)response.StatusCode} {response.StatusCode}");
            return ApiResult<string>.Success(responseText, response.StatusCode);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return ApiResult<string>.Exception(ex, "The request timed out.");
        }
        catch (Exception ex)
        {
            return ApiResult<string>.Exception(ex);
        }
    }

    private static async Task<ApiResult<T>> SendForJsonAsync<T>(HttpMethod method, string uri, object? body, CancellationToken cancellationToken)
    {
        ApiResult<string> result = await SendForStringAsync(method, uri, body, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
            return ApiResult<T>.Error(result.Status, result.ErrorMessage ?? "The request failed.");

        if (string.IsNullOrWhiteSpace(result.Data))
            return ApiResult<T>.Error(result.Status, "The response body was empty.");

        try
        {
            T? data = JsonSerializer.Deserialize<T>(result.Data, JsonOptions);
            if (data is null)
                return ApiResult<T>.Error(result.Status, "The response body could not be deserialized.");

            return ApiResult<T>.Success(data, result.Status);
        }
        catch (JsonException ex)
        {
            return ApiResult<T>.Exception(ex, $"Invalid JSON returned from '{uri}'.");
        }
    }

    private static bool ShouldUseMatchClient(HttpMethod method, string uri)
    {
        return method == HttpMethod.Post && string.Equals(uri, "match/v1", StringComparison.OrdinalIgnoreCase);
    }

    private static HttpClient CreateClient(bool useClientCertificate)
    {
        var sslOptions = new SslClientAuthenticationOptions
        {
            EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
            RemoteCertificateValidationCallback = ValidateServerCertificate
        };

        if (useClientCertificate)
            sslOptions.ClientCertificates = LoadClientCertificates();

        var handler = new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            UseCookies = false,
            SslOptions = sslOptions
        };

        return new HttpClient(handler)
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(5)
        };
    }

    private static bool ValidateServerCertificate(object? sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
    {
        if (certificate is null)
            return false;

        X509Certificate2 cert2 = certificate as X509Certificate2 ?? new X509Certificate2(certificate);
        string? thumbprint = cert2.Thumbprint?.Replace(" ", "");

        if (string.Equals(thumbprint, PinnedThumbprint, StringComparison.OrdinalIgnoreCase))
            return true;

        return sslPolicyErrors == SslPolicyErrors.None;
    }

    private static string BuildErrorMessage(HttpStatusCode status, string? reasonPhrase, string? body)
    {
        string trimmed = (body ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
        if (trimmed.Length > 256)
            trimmed = trimmed[..256] + "...";

        return string.IsNullOrWhiteSpace(trimmed)
            ? reasonPhrase ?? $"Request failed with status {(int)status}."
            : $"{reasonPhrase ?? "Request failed"} Body='{trimmed}'";
    }

    private static X509CertificateCollection LoadClientCertificates()
    {
        var certificates = new X509CertificateCollection();

        try
        {
            string certPath = @"C:\Users\erikm\Downloads\erky.p12";

            // CHANGE HERE RESIN!
            //string certPath = @"C:\Users\erikm\Downloads\resin.p12";


            string password = ""; // set the real password here if the .p12 has one

            if (!File.Exists(certPath))
            {
                Log.Warn($"[ApiClient] Client certificate not found: {certPath}");
                return certificates;
            }

            X509Certificate2 certificate = new(
                certPath,
                password,
                X509KeyStorageFlags.UserKeySet |
                X509KeyStorageFlags.PersistKeySet |
                X509KeyStorageFlags.Exportable);

            Log.Info($"[ApiClient] Loaded client certificate: {certificate.Subject}");
            Log.Info($"[ApiClient] Client certificate thumbprint: {certificate.Thumbprint}");
            Log.Info($"[ApiClient] Client certificate has private key: {certificate.HasPrivateKey}");

            certificates.Add(certificate);
        }
        catch (Exception ex)
        {
            Log.Error($"[ApiClient] Failed to load client certificate: {ex}");
        }

        return certificates;
    }
}