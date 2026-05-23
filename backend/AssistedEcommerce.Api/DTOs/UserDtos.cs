namespace AssistedEcommerce.Api.DTOs;

public record CreateUserRequest(string FullName, string Phone);

public record UpdateUserStatusRequest(string Status);

/// <summary>PUT /api/users/{userId} — geli kaliya waxaad beddeshay (createdAt/totalOrders lama ogola).</summary>
public class UpdateUserRequest
{
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    /// <summary>active ama blocked kaliya</summary>
    public string? Status { get; set; }
}

public record UserDto(string UserId, string FullName, string Phone, string Status, int TotalOrders, DateTime CreatedAt);
