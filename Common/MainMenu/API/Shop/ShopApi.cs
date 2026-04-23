using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace PvPAdventure.Common.MainMenu.API.Shop;

internal enum PurchaseResult
{
    Purchased,
    AlreadyOwned
}

internal static class ShopApi
{
    public static async Task<ApiResult<PurchaseResult>> PurchaseProductAsync(string prototype, string name, CancellationToken cancellationToken = default)
    {
        // Keep the old top-level shape, but also include a nested product for newer backend handlers.
        var payload = new
        {
            prototype,
            name,
            product = new
            {
                prototype,
                name
            }
        };

        ApiResult<string> result = await ApiClient.PostStringAsync("shop/purchase/v1", payload, cancellationToken).ConfigureAwait(false);

        if (result.IsSuccess)
            return ApiResult<PurchaseResult>.Success(PurchaseResult.Purchased, result.Status, result.RequestSummary);

        if (result.Status == HttpStatusCode.Conflict)
            return ApiResult<PurchaseResult>.Success(PurchaseResult.AlreadyOwned, result.Status, result.RequestSummary);

        return ApiResult<PurchaseResult>.Error(result.Status, result.ErrorMessage ?? "Purchase failed.", result.RequestSummary);
    }
}
