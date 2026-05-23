using AssistedEcommerce.Api.Models;
using MongoDB.Driver;

namespace AssistedEcommerce.Api.Infrastructure;

public interface IIdGeneratorService
{
    Task<string> NextUserIdAsync(CancellationToken ct = default);
    Task<string> NextOrderIdAsync(string userId, CancellationToken ct = default);
    Task<string> NextInvoiceNumberAsync(CancellationToken ct = default);
    Task<string> NextPaymentIdAsync(CancellationToken ct = default);
    Task<string> NextAdminIdAsync(CancellationToken ct = default);
}

public class IdGeneratorService(MongoDbContext db) : IIdGeneratorService
{
    /// <summary>01, 02, … 99, 100 — bilowga counter ka dib reset</summary>
    public Task<string> NextUserIdAsync(CancellationToken ct = default) =>
        NextFormattedAsync("user_global", n => n < 100 ? $"{n:D2}" : $"{n}", ct);

    /// <summary>order1, order2, order3 — global sequence</summary>
    public Task<string> NextOrderIdAsync(string userId, CancellationToken ct = default) =>
        NextFormattedAsync("order_global", n => $"order{n}", ct);

    public async Task<string> NextInvoiceNumberAsync(CancellationToken ct = default)
    {
        var year = DateTime.UtcNow.Year;
        var seq = await IncrementAsync($"invoice_{year}", ct);
        return $"INV-{year}-{seq:D4}";
    }

    public Task<string> NextPaymentIdAsync(CancellationToken ct = default) =>
        NextFormattedAsync("payment_global", n => $"PAY-{n:D4}", ct);

    public Task<string> NextAdminIdAsync(CancellationToken ct = default) =>
        NextFormattedAsync("admin_global", n => $"ADM-{n:D3}", ct);

    private async Task<string> NextFormattedAsync(string key, Func<long, string> format, CancellationToken ct)
    {
        var seq = await IncrementAsync(key, ct);
        return format(seq);
    }

    private async Task<long> IncrementAsync(string key, CancellationToken ct)
    {
        var filter = Builders<Counter>.Filter.Eq(c => c.Key, key);
        var update = Builders<Counter>.Update.Inc(c => c.Seq, 1);
        var options = new FindOneAndUpdateOptions<Counter>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After
        };
        var counter = await db.Counters.FindOneAndUpdateAsync(filter, update, options, ct);
        return counter.Seq;
    }
}
