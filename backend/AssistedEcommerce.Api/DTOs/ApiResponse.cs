namespace AssistedEcommerce.Api.DTOs;

public record ApiResponse<T>(bool Success, T? Data, string? Message = null);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, long TotalCount, int TotalPages);
