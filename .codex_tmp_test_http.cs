using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Security;
using System.Threading.Tasks;

var handler = new HttpClientHandler();
handler.ClientCertificateOptions = ClientCertificateOption.Automatic;
handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
using var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.tpvpa.terraria.sh/") };
try
{
    var response = await client.GetAsync("shop/v1");
    Console.WriteLine($"Status: {(int)response.StatusCode} {response.StatusCode}");
    Console.WriteLine(await response.Content.ReadAsStringAsync());
}
catch (Exception ex)
{
    Console.WriteLine(ex.ToString());
    if (ex.InnerException != null)
        Console.WriteLine("INNER: " + ex.InnerException);
}
