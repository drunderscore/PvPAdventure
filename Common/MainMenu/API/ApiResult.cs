using System;
using System.Net;

namespace PvPAdventure.Common.MainMenu.API;

public sealed class ApiResult<T>
{
    public bool IsSuccess { get; }
    public HttpStatusCode Status { get; }
    public T? Data { get; }
    public string? ErrorMessage { get; }

    private ApiResult(bool isSuccess, HttpStatusCode status, T? data, string? errorMessage)
    {
        IsSuccess = isSuccess;
        Status = status;
        Data = data;
        ErrorMessage = errorMessage;
    }

    public static ApiResult<T> Success(T data, HttpStatusCode status = HttpStatusCode.OK)
    {
        return new ApiResult<T>(true, status, data, null);
    }

    public static ApiResult<T> Error(HttpStatusCode status, string message)
    {
        return new ApiResult<T>(false, status, default, message);
    }

    public static ApiResult<T> Exception(Exception ex, string? message = null)
    {
        return new ApiResult<T>(false, 0, default, message ?? ex.Message);
    }
}