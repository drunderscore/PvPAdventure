using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PvPAdventure.Common.Authentication;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.MainMenu.API;

internal static class ApiClient
{
#if DEBUG && !USE_PRODUCTION_API_IN_DEBUG
    private const string BaseUrl = "https://api.tpvpa.terraria.sh";
    private const string DevThumbprint = "51A6F42F8479EDBB926C9E4385D7B8286A64C418";
#else
    private const string BaseUrl = "https://api.tpvpa.terraria.sh";
#endif
    private static readonly DateTime OfficialCertificateExpirePriorWarning = DateTime.Now.AddDays(14);

    private const int MaxLoggedBodyLength = 256;

    private static readonly JsonSerializerOptions JsonOptions = new();

    private static readonly HttpClient Client = CreateClient();

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

            var steamAuth = ModContent.GetInstance<SteamAuthentication>();
            var ticket = steamAuth.WebTicket;

            if (!string.IsNullOrWhiteSpace(ticket))
                request.Headers.Add("Ticket", ticket);

            if (body != null)
                request.Content = JsonContent.Create(body);

            using HttpResponseMessage response = await Client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            string responseText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

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

                Log.Warn($"{method} {uri} -> {(int)response.StatusCode} {response.StatusCode}. {error}");
                return ApiResult<string>.Error(response.StatusCode, error);
            }

            Log.Info($"{method} {uri} -> {(int)response.StatusCode} {response.StatusCode}");
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

    private static HttpClient CreateClient()
    {
        var sslOptions = new SslClientAuthenticationOptions
        {
            EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
#if DEBUG && !USE_PRODUCTION_API_IN_DEBUG
            RemoteCertificateValidationCallback = ValidateServerCertificate
#endif
        };

        if (Main.dedServ)
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
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

#if DEBUG && !USE_PRODUCTION_API_IN_DEBUG
    private static bool ValidateServerCertificate(object? sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
    {
        if (certificate is null)
            return false;

        X509Certificate2 cert2 = certificate as X509Certificate2 ?? new X509Certificate2(certificate);
        string thumbprint = cert2.Thumbprint;

        if (string.Equals(thumbprint, DevThumbprint, StringComparison.OrdinalIgnoreCase))
            return true;

        return sslPolicyErrors == SslPolicyErrors.None;
    }
#endif

    private static string BuildErrorMessage(HttpStatusCode status, string? reasonPhrase, string? body)
    {
        string trimmed = (body ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
        if (trimmed.Length > MaxLoggedBodyLength)
            trimmed = trimmed[..MaxLoggedBodyLength] + "...";

        return string.IsNullOrWhiteSpace(trimmed)
            ? reasonPhrase ?? $"Request failed with status {(int)status}."
            : $"{reasonPhrase ?? "Request failed"} Body='{trimmed}'";
    }

    private static X509CertificateCollection LoadClientCertificates()
    {
        var certificates = new X509CertificateCollection();

        try
        {
            var officialCertificatePath = Environment.GetEnvironmentVariable("PVPA_OFFICIAL_CERTIFICATE_PATH");
            if (officialCertificatePath != null)
            {
                X509Certificate2 certificate = new(
                    officialCertificatePath,
                    (string)null,
                    X509KeyStorageFlags.UserKeySet);

                Log.Info(
                    $"Loaded client certificate for official identity {certificate.Subject} (expiry {certificate.GetExpirationDateString()}, thumbprint {certificate.Thumbprint})");

                if (certificate.NotAfter <= OfficialCertificateExpirePriorWarning)
                    Console.WriteLine("Your PvP Adventure official server certificate will soon expire!");

                certificates.Add(certificate);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to load client certificate: {ex}");
        }

        return certificates;
    }
}