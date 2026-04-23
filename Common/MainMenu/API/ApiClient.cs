using PvPAdventure.Common.Authentication;
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
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.MainMenu.API;

internal static class ApiClient
{
    private const string BaseUrl = "https://api.tpvpa.terraria.sh";
    private static readonly DateTime OfficialCertificateExpirePriorWarning = DateTime.Now.AddDays(14);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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
        string requestUrl = new Uri(Client.BaseAddress!, uri).ToString();

        try
        {
            using HttpRequestMessage request = new(method, uri);

            var steamAuth = ModContent.GetInstance<SteamAuthentication>();
            var ticket = steamAuth.WebTicket;

            if (!string.IsNullOrWhiteSpace(ticket))
                request.Headers.Add("Ticket", ticket);

            if (body != null)
                request.Content = JsonContent.Create(body, options: JsonOptions);

            using HttpResponseMessage response = await Client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            string responseText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            string requestSummary = $"{method} {requestUrl} -> {(int)response.StatusCode} {response.StatusCode}";

            Log.Debug($"[ApiClient] Raw response body for {method} {requestUrl}: {responseText}");

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

                Log.Warn($"{requestSummary}. {error}");
                return ApiResult<string>.Error(response.StatusCode, error, requestSummary);
            }

            Log.Info(requestSummary);
            return ApiResult<string>.Success(responseText, response.StatusCode, requestSummary);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            string requestSummary = $"{method} {requestUrl} -> timeout";
            return ApiResult<string>.Exception(ex, "The request timed out.", requestSummary);
        }
        catch (Exception ex)
        {
            string requestSummary = $"{method} {requestUrl} -> {ex.GetType().Name}: {ex.Message}";
            return ApiResult<string>.Exception(ex, requestSummary: requestSummary);
        }
    }

    private static async Task<ApiResult<T>> SendForJsonAsync<T>(HttpMethod method, string uri, object? body, CancellationToken cancellationToken)
    {
        ApiResult<string> result = await SendForStringAsync(method, uri, body, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
            return ApiResult<T>.Error(result.Status, result.ErrorMessage ?? "The request failed.", result.RequestSummary);

        if (string.IsNullOrWhiteSpace(result.Data))
            return ApiResult<T>.Error(result.Status, "The response body was empty.", result.RequestSummary);

        try
        {
            T? data = JsonSerializer.Deserialize<T>(result.Data, JsonOptions);
            if (data is null)
                return ApiResult<T>.Error(result.Status, "The response body could not be deserialized.", result.RequestSummary);

            return ApiResult<T>.Success(data, result.Status, result.RequestSummary);
        }
        catch (JsonException ex)
        {
            return ApiResult<T>.Exception(ex, $"Invalid JSON returned from '{uri}'.", result.RequestSummary);
        }
    }

    private static HttpClient CreateClient()
    {
        var sslOptions = new SslClientAuthenticationOptions
        {
            EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
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
            string officialCertificatePathTest = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads", "erky.p12");

            if (File.Exists(officialCertificatePathTest))
            {
                Log.Debug("The file exists");
            }
            else
            {
                Log.Error("NO CERTIFICATE EXISTS, ABORTING!!!");
            }

            //X509KeyStorageFlags.UserKeySet

            const string officialCertificatePath = @"C:\Users\erikm\Downloads\erky.p12";
            const string password = "";

            //X509Certificate2 certificate = new(
            //    officialCertificatePath,
            //    string.IsNullOrWhiteSpace(password) ? null : password,
            //    X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.MachineKeySet);

            X509Certificate2 certificate = new(
    officialCertificatePath,
    string.IsNullOrWhiteSpace(password) ? null : password,
    X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);

            Log.Info($"Loaded client certificate for official identity {certificate.Subject} (expiry {certificate.NotAfter:yyyy-MM-dd HH:mm:ss}, thumbprint {certificate.Thumbprint})");

            if (certificate.NotAfter <= OfficialCertificateExpirePriorWarning)
                Log.Warn("Your PvP Adventure official server certificate will soon expire!");

            certificates.Add(certificate);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to load client certificate: {ex}");
        }

        return certificates;
    }
}
