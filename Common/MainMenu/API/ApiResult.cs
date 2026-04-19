using System;
using System.Net;

namespace PvPAdventure.Common.MainMenu.API;

public sealed class ApiResult<T>
{
    public bool IsSuccess { get; }
    public HttpStatusCode Status { get; }
    public T? Data { get; }
    public string? ErrorMessage { get; }
    public string RequestSummary { get; }

    private ApiResult(bool isSuccess, HttpStatusCode status, T? data, string? errorMessage, string requestSummary)
    {
        IsSuccess = isSuccess;
        Status = status;
        Data = data;
        ErrorMessage = errorMessage;
        RequestSummary = requestSummary;
    }

    public static ApiResult<T> Success(T data, HttpStatusCode status = HttpStatusCode.OK, string requestSummary = "")
    {
        return new ApiResult<T>(true, status, data, null, requestSummary);
    }

    public static ApiResult<T> Error(HttpStatusCode status, string message, string requestSummary = "")
    {
        return new ApiResult<T>(false, status, default, message, requestSummary);
    }

    public static ApiResult<T> Exception(Exception ex, string? message = null, string requestSummary = "")
    {
        return new ApiResult<T>(false, 0, default, message ?? ex.Message, requestSummary);
    }
}
