namespace WO.Property.Shared.DTOs.Common;

/// <summary>
/// 统一 API 响应格式
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public PaginationInfo? Pagination { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null) => new()
    {
        Success = true,
        Message = message,
        Data = data
    };

    public static ApiResponse<T> Fail(string message) => new()
    {
        Success = false,
        Message = message
    };

    public static ApiResponse<T> OkPage(T data, PaginationInfo pagination) => new()
    {
        Success = true,
        Data = data,
        Pagination = pagination
    };
}

public class PaginationInfo
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

/// <summary>
/// 无泛型的简单响应（用于简单操作）
/// </summary>
public class SimpleResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public object? Data { get; set; }

    public static SimpleResponse Ok(object? data = null, string? message = null) => new()
    {
        Success = true,
        Message = message,
        Data = data
    };

    public static SimpleResponse Fail(string message) => new()
    {
        Success = false,
        Message = message
    };
}
