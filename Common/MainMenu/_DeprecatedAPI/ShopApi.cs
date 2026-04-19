//using System.Net;
//using System.Threading;
//using System.Threading.Tasks;
//using PvPAdventure.Common.Authentication;
//using Terraria.ModLoader;

//namespace PvPAdventure.Common.MainMenu.API;

//internal enum PurchaseResult
//{
//    Purchased,
//    AlreadyOwned
//}

//internal static class ShopApi
//{
//    public static Task<ApiResult<string>> GetShopJsonAsync(CancellationToken cancellationToken = default)
//    {
//        return ApiClient.GetStringAsync("shop/v1", cancellationToken);
//    }

//    public static async Task<ApiResult<PurchaseResult>> PurchaseProductAsync(string prototype, string name, CancellationToken cancellationToken = default)
//    {
//        var payload = new
//        {
//            prototype,
//            name
//        };

//        ApiResult<string> result = await ApiClient.PostStringAsync("shop/purchase/v1", payload, cancellationToken).ConfigureAwait(false);

//        if (result.IsSuccess)
//            return ApiResult<PurchaseResult>.Success(PurchaseResult.Purchased, result.Status);

//        if (result.Status == HttpStatusCode.Conflict)
//            return ApiResult<PurchaseResult>.Success(PurchaseResult.AlreadyOwned, result.Status);

//        return ApiResult<PurchaseResult>.Error(result.Status, result.ErrorMessage ?? "Purchase failed.");
//    }
//}