using AssistedEcommerce.Api.Constants;
using AssistedEcommerce.Api.DTOs;
using AssistedEcommerce.Api.Exceptions;
using AssistedEcommerce.Api.Infrastructure;
using AssistedEcommerce.Api.Models;
using MongoDB.Driver;

namespace AssistedEcommerce.Api.Services;

public interface IUserService
{
    Task<UserDto> CreateUserAsync(CreateUserRequest request, string adminId, CancellationToken ct = default);
    Task<PagedResult<UserDto>> GetUsersAsync(int page, int pageSize, string? search, CancellationToken ct = default);
    Task<UserDto> GetUserByIdAsync(string userId, CancellationToken ct = default);
    Task<UserDto> UpdateUserAsync(string userId, UpdateUserRequest request, string adminId, CancellationToken ct = default);
    Task<UserDto> UpdateUserStatusAsync(string userId, string status, string adminId, CancellationToken ct = default);
    Task DeleteUserAsync(string userId, string adminId, CancellationToken ct = default);
    Task<User> FindOrCreateByPhoneAsync(string fullName, string phone, CancellationToken ct = default);
}

public class UserService(MongoDbContext db, IIdGeneratorService idGenerator, IAuditService auditService) : IUserService
{
    public async Task<UserDto> CreateUserAsync(CreateUserRequest request, string adminId, CancellationToken ct = default)
    {
        var phone = NormalizePhone(request.Phone);
        var existing = await db.Users.Find(u => u.Phone == phone).FirstOrDefaultAsync(ct);
        if (existing is not null)
            throw new ApiException("User with this phone already exists.");

        var user = new User
        {
            UserId = await idGenerator.NextUserIdAsync(ct),
            FullName = request.FullName.Trim(),
            Phone = phone,
            Status = UserStatuses.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await db.Users.InsertOneAsync(user, cancellationToken: ct);
        await auditService.LogAsync(adminId, "CREATE_USER", "Users", user.UserId);

        return Map(user);
    }

    public async Task<PagedResult<UserDto>> GetUsersAsync(int page, int pageSize, string? search, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var filter = Builders<User>.Filter.Empty;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            filter = Builders<User>.Filter.Or(
                Builders<User>.Filter.Regex(u => u.FullName, new MongoDB.Bson.BsonRegularExpression(s, "i")),
                Builders<User>.Filter.Regex(u => u.Phone, new MongoDB.Bson.BsonRegularExpression(s, "i")),
                Builders<User>.Filter.Regex(u => u.UserId, new MongoDB.Bson.BsonRegularExpression(s, "i")));
        }

        var all = await db.Users.Find(filter)
            .SortBy(u => u.CreatedAt)
            .ToListAsync(ct);

        var distinct = all
            .GroupBy(u => PhoneNormalizer.CanonicalKey(u.Phone))
            .Select(g =>
            {
                var keep = g.OrderBy(u => u.CreatedAt).ThenBy(u => u.UserId).First();
                var orders = g.Sum(x => x.TotalOrders);
                return Map(keep) with { TotalOrders = orders };
            })
            .OrderBy(u => u.CreatedAt)
            .ThenBy(u => u.UserId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var total = distinct.Count;
        var items = distinct
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        return new PagedResult<UserDto>(items, page, pageSize, total, totalPages);
    }

    public async Task<UserDto> GetUserByIdAsync(string userId, CancellationToken ct = default)
    {
        var user = await db.Users.Find(u => u.UserId == userId).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("User not found.");
        return Map(user);
    }

    public async Task<UserDto> UpdateUserAsync(string userId, UpdateUserRequest request, string adminId, CancellationToken ct = default)
    {
        var user = await db.Users.Find(u => u.UserId == userId).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("User not found.");

        if (!string.IsNullOrWhiteSpace(request.FullName))
            user.FullName = request.FullName.Trim();

        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            var phone = NormalizePhone(request.Phone);
            var phoneTaken = await db.Users.Find(u => u.Phone == phone && u.UserId != userId).AnyAsync(ct);
            if (phoneTaken)
                throw new ApiException("Phone already used by another user.");
            user.Phone = phone;
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = request.Status.Trim().ToLowerInvariant();
            if (status is not UserStatuses.Active and not UserStatuses.Blocked)
                throw new ApiException("Status must be 'active' or 'blocked' (not '" + request.Status + "').");
            user.Status = status;
        }

        user.UpdatedAt = DateTime.UtcNow;
        await db.Users.ReplaceOneAsync(u => u.Id == user.Id, user, cancellationToken: ct);
        await auditService.LogAsync(adminId, "UPDATE_USER", "Users", userId);

        return Map(user);
    }

    public async Task DeleteUserAsync(string userId, string adminId, CancellationToken ct = default)
    {
        var user = await db.Users.Find(u => u.UserId == userId).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("User not found.");

        var orderCount = await db.Orders.CountDocumentsAsync(o => o.UserId == userId, cancellationToken: ct);
        if (orderCount > 0)
            throw new ApiException("Cannot delete user with existing orders. Delete orders first.");

        await db.Users.DeleteOneAsync(u => u.Id == user.Id, ct);
        await auditService.LogAsync(adminId, "DELETE_USER", "Users", userId);
    }

    public async Task<UserDto> UpdateUserStatusAsync(string userId, string status, string adminId, CancellationToken ct = default)
    {
        if (status is not UserStatuses.Active and not UserStatuses.Blocked)
            throw new ApiException("Status must be 'active' or 'blocked'.");

        var user = await db.Users.Find(u => u.UserId == userId).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("User not found.");

        var old = user.Status;
        user.Status = status;
        user.UpdatedAt = DateTime.UtcNow;
        await db.Users.ReplaceOneAsync(u => u.Id == user.Id, user, cancellationToken: ct);
        await auditService.LogAsync(adminId, "UPDATE_USER_STATUS", "Users", userId, new { old, status });

        return Map(user);
    }

    public async Task<User> FindOrCreateByPhoneAsync(string fullName, string phone, CancellationToken ct = default)
    {
        var normalized = NormalizePhone(phone);
        if (string.IsNullOrEmpty(normalized))
            throw new ApiException("Telefoonka ma saxna.");

        var user = await FindUserByPhoneAsync(normalized, ct);
        if (user is not null)
        {
            if (user.Status == UserStatuses.Blocked)
                throw new ApiException("This account is blocked.", 403);

            if (user.Phone != normalized)
            {
                user.Phone = normalized;
                user.UpdatedAt = DateTime.UtcNow;
                await db.Users.ReplaceOneAsync(u => u.Id == user.Id, user, cancellationToken: ct);
            }

            if (!string.IsNullOrWhiteSpace(fullName) && user.FullName != fullName.Trim())
            {
                user.FullName = fullName.Trim();
                user.UpdatedAt = DateTime.UtcNow;
                await db.Users.ReplaceOneAsync(u => u.Id == user.Id, user, cancellationToken: ct);
            }

            return user;
        }

        user = new User
        {
            UserId = await idGenerator.NextUserIdAsync(ct),
            FullName = fullName.Trim(),
            Phone = normalized,
            Status = UserStatuses.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await db.Users.InsertOneAsync(user, cancellationToken: ct);
        return user;
    }

    private async Task<User?> FindUserByPhoneAsync(string canonicalPhone, CancellationToken ct)
    {
        var user = await db.Users.Find(u => u.Phone == canonicalPhone).FirstOrDefaultAsync(ct);
        if (user is not null)
            return user;

        if (canonicalPhone.Length < 9)
            return null;

        var suffix = canonicalPhone[^9..];
        var candidates = await db.Users.Find(u => u.Phone.EndsWith(suffix)).ToListAsync(ct);
        return candidates.FirstOrDefault(u =>
            PhoneNormalizer.CanonicalKey(u.Phone) == PhoneNormalizer.CanonicalKey(canonicalPhone));
    }

    internal static string NormalizePhone(string phone) => PhoneNormalizer.Normalize(phone);

    private static UserDto Map(User u) =>
        new(u.UserId, u.FullName, u.Phone, u.Status, u.TotalOrders, u.CreatedAt);
}
