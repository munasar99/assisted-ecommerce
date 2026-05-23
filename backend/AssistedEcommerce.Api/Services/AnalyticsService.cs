using AssistedEcommerce.Api.Constants;
using AssistedEcommerce.Api.DTOs;
using AssistedEcommerce.Api.Infrastructure;
using AssistedEcommerce.Api.Models;
using MongoDB.Driver;

namespace AssistedEcommerce.Api.Services;

public interface IAnalyticsService
{
    Task<DashboardAnalyticsDto> GetDashboardAsync(CancellationToken ct = default);
}

public class AnalyticsService(MongoDbContext db) : IAnalyticsService
{
    public async Task<DashboardAnalyticsDto> GetDashboardAsync(CancellationToken ct = default)
    {
        var statusGroupsTask = db.Orders.Aggregate()
            .Group(o => o.Status, g => new StatusCountDto(g.Key, g.Count()))
            .ToListAsync(ct);

        var revenueTask = db.Invoices.Aggregate()
            .Group(_ => 1, g => new { Total = g.Sum(i => i.TotalAmount) })
            .FirstOrDefaultAsync(ct);

        var districtGroupsTask = db.Orders.Aggregate()
            .Group(o => o.DistrictId, g => new DistrictCountDto(g.Key, g.Count()))
            .SortByDescending(d => d.Count)
            .Limit(10)
            .ToListAsync(ct);

        var userRowsTask = db.Users
            .Find(FilterDefinition<User>.Empty)
            .Project(u => new UserPhoneStatus(u.Phone, u.Status))
            .ToListAsync(ct);

        await Task.WhenAll(statusGroupsTask, revenueTask, districtGroupsTask, userRowsTask);

        var statusGroups = await statusGroupsTask;
        var revenueAgg = await revenueTask;
        var districtGroups = await districtGroupsTask;
        var userRows = await userRowsTask;

        long CountStatus(string status) =>
            statusGroups.Where(s => string.Equals(s.Status, status, StringComparison.Ordinal))
                .Sum(s => s.Count);

        var totalOrders = statusGroups.Sum(s => s.Count);
        var pendingOrders = CountStatus(OrderStatuses.Pending);
        var paymentReview = CountStatus(OrderStatuses.PaymentReview);
        var delivered = CountStatus(OrderStatuses.Delivered);
        var revenue = revenueAgg?.Total ?? 0m;

        var totalUsers = userRows
            .GroupBy(u => PhoneNormalizer.CanonicalKey(u.Phone))
            .Count(g => !string.IsNullOrEmpty(g.Key));

        var activeUsers = userRows
            .Where(u => u.Status == UserStatuses.Active)
            .GroupBy(u => PhoneNormalizer.CanonicalKey(u.Phone))
            .Count(g => !string.IsNullOrEmpty(g.Key));

        var blockedUsers = userRows
            .Where(u => u.Status == UserStatuses.Blocked)
            .GroupBy(u => PhoneNormalizer.CanonicalKey(u.Phone))
            .Count(g => !string.IsNullOrEmpty(g.Key));

        return new DashboardAnalyticsDto(
            totalOrders, pendingOrders, paymentReview, delivered, revenue,
            totalUsers, activeUsers, blockedUsers, statusGroups, districtGroups);
    }

    private sealed record UserPhoneStatus(string Phone, string Status);
}
