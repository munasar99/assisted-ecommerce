namespace AssistedEcommerce.Api.DTOs;

public record LoginRequest(string Email, string Password);

public record LoginResponse(string AccessToken, int ExpiresIn, AdminProfileDto Admin);

public record AdminProfileDto(string AdminId, string Email, string FullName, string Role);
