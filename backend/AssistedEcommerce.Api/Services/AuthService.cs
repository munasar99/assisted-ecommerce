using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AssistedEcommerce.Api.Constants;
using AssistedEcommerce.Api.DTOs;
using AssistedEcommerce.Api.Exceptions;
using AssistedEcommerce.Api.Infrastructure;
using AssistedEcommerce.Api.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;

namespace AssistedEcommerce.Api.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, string? ip, CancellationToken ct = default);
    Task LogoutAsync(string adminId, string? ip, CancellationToken ct = default);
    Task<AdminProfileDto> GetProfileAsync(string adminId, CancellationToken ct = default);
}

public class AuthService(
    MongoDbContext db,
    IOptions<JwtSettings> jwtOptions,
    IAuditService auditService) : IAuthService
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request, string? ip, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var admin = await db.Admins.Find(a => a.Email == email).FirstOrDefaultAsync(ct)
            ?? throw new UnauthorizedAppException("Invalid email or password.");

        if (!admin.IsActive || !BCrypt.Net.BCrypt.Verify(request.Password, admin.PasswordHash))
            throw new UnauthorizedAppException("Invalid email or password.");

        admin.LastLoginAt = DateTime.UtcNow;
        await db.Admins.ReplaceOneAsync(a => a.Id == admin.Id, admin, cancellationToken: ct);

        var jwt = jwtOptions.Value;
        var expires = DateTime.UtcNow.AddHours(jwt.ExpiresHours);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, admin.AdminId),
            new Claim(JwtRegisteredClaimNames.Email, admin.Email),
            new Claim("role", Roles.Admin),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(jwt.Issuer, jwt.Audience, claims, expires: expires, signingCredentials: creds);
        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        await auditService.LogAsync(admin.AdminId, "LOGIN", "Admins", admin.AdminId, ip: ip);

        return new LoginResponse(
            accessToken,
            (int)TimeSpan.FromHours(jwt.ExpiresHours).TotalSeconds,
            new AdminProfileDto(admin.AdminId, admin.Email, admin.FullName, admin.Role));
    }

    public async Task LogoutAsync(string adminId, string? ip, CancellationToken ct = default)
    {
        await auditService.LogAsync(adminId, "LOGOUT", "Admins", adminId, ip: ip);
    }

    public async Task<AdminProfileDto> GetProfileAsync(string adminId, CancellationToken ct = default)
    {
        var admin = await db.Admins.Find(a => a.AdminId == adminId).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Admin not found.");
        return new AdminProfileDto(admin.AdminId, admin.Email, admin.FullName, admin.Role);
    }
}
