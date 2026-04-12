using PvPAdventure.Common.MainMenu.Profile;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using PvPAdventure.Common.Authentication;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.MainMenu.API;

internal static class ProfileApi
{
    public static Task<ApiResult<ApiProfileResponse>> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        return ApiClient.GetJsonAsync<ApiProfileResponse>("profile/v1", cancellationToken);
    }

    public static Task<ApiResult<List<ApiInventoryItem>>> GetInventoryAsync(CancellationToken cancellationToken = default)
    {
        return ApiClient.GetJsonAsync<List<ApiInventoryItem>>("profile/inventory/v1", cancellationToken);
    }

    public static async Task<ApiResult<bool>> RefreshProfileStateAsync(CancellationToken cancellationToken = default)
    {
        Task<ApiResult<ApiProfileResponse>> profileTask = GetProfileAsync(cancellationToken);
        Task<ApiResult<List<ApiInventoryItem>>> inventoryTask = GetInventoryAsync(cancellationToken);

        await Task.WhenAll(profileTask, inventoryTask).ConfigureAwait(false);

        ApiResult<ApiProfileResponse> profileResult = await profileTask.ConfigureAwait(false);
        if (!profileResult.IsSuccess || profileResult.Data is null)
            return ApiResult<bool>.Error(profileResult.Status, profileResult.ErrorMessage ?? "Failed to load profile.");

        ApiResult<List<ApiInventoryItem>> inventoryResult = await inventoryTask.ConfigureAwait(false);
        if (!inventoryResult.IsSuccess || inventoryResult.Data is null)
            return ApiResult<bool>.Error(inventoryResult.Status, inventoryResult.ErrorMessage ?? "Failed to load inventory.");

        Main.QueueMainThreadAction(() =>
        {
            MainMenuProfileState.Instance.SyncWithBackend(profileResult.Data, inventoryResult.Data);
        });

        return ApiResult<bool>.Success(true);
    }

    public static async Task<ApiResult<bool>> UpdateEquipmentAsync(string prototype, string? name, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            prototype,
            name
        };

        ApiResult<string> result = await ApiClient.PostStringAsync("profile/equip/v1", payload, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
            return ApiResult<bool>.Error(result.Status, result.ErrorMessage ?? "Failed to update equipment.");

        return ApiResult<bool>.Success(true, result.Status);
    }
}