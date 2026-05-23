using AssistedEcommerce.Api.DTOs;
using AssistedEcommerce.Api.Infrastructure;
using AssistedEcommerce.Api.Models;
using AssistedEcommerce.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AssistedEcommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController(
    MongoDbContext db,
    IOptions<MongoDbSettings> mongoSettings,
    IOptions<CloudinarySettings> cloudinarySettings,
    IOptions<ResendSettings> resendSettings,
    IEmailService emailService,
    DatabaseSeeder seeder,
    IWebHostEnvironment env) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get() => Ok(new { success = true, status = "healthy", timestamp = DateTime.UtcNow });

    /// <summary>Test MongoDB connection and show database name (ubaxsana).</summary>
    [HttpGet("mongodb")]
    [AllowAnonymous]
    public async Task<IActionResult> MongoDb(CancellationToken ct)
    {
        var settings = mongoSettings.Value;
        try
        {
            await db.DeliveryZones.Database.RunCommandAsync<BsonDocument>(
                new BsonDocument("ping", 1), cancellationToken: ct);

            var collections = await db.DeliveryZones.Database.ListCollectionNames().ToListAsync(ct);
            var zoneCount = await db.DeliveryZones.CountDocumentsAsync(FilterDefinition<Models.DeliveryZone>.Empty, cancellationToken: ct);

            return Ok(new
            {
                success = true,
                message = "MongoDB connected",
                database = settings.DatabaseName,
                connection = settings.ConnectionString,
                collections,
                deliveryZonesCount = zoneCount
            });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new
            {
                success = false,
                message = "MongoDB connection failed. Is MongoDB running?",
                database = settings.DatabaseName,
                error = ex.Message
            });
        }
    }

    /// <summary>Count documents saved in database ubaxsana (proves data is stored).</summary>
    [HttpGet("data")]
    [AllowAnonymous]
    public async Task<IActionResult> DataCounts(CancellationToken ct)
    {
        try
        {
            await db.Database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: ct);

            var counts = new
            {
                admins = await db.Admins.CountDocumentsAsync(FilterDefinition<Models.Admin>.Empty, cancellationToken: ct),
                users = await db.Users.CountDocumentsAsync(FilterDefinition<Models.User>.Empty, cancellationToken: ct),
                orders = await db.Orders.CountDocumentsAsync(FilterDefinition<Models.Order>.Empty, cancellationToken: ct),
                invoices = await db.Invoices.CountDocumentsAsync(FilterDefinition<Models.Invoice>.Empty, cancellationToken: ct),
                deliveryZones = await db.DeliveryZones.CountDocumentsAsync(FilterDefinition<Models.DeliveryZone>.Empty, cancellationToken: ct),
                auditLogs = await db.AuditLogs.CountDocumentsAsync(FilterDefinition<Models.AuditLog>.Empty, cancellationToken: ct),
                payments = await db.Payments.CountDocumentsAsync(FilterDefinition<Models.Payment>.Empty, cancellationToken: ct)
            };

            return Ok(new
            {
                success = true,
                message = "Xogta waa ku kaydsan MongoDB (persistent). API xidh kadib waa sii jirtaa.",
                database = mongoSettings.Value.DatabaseName,
                documentCounts = counts,
                howToSave = "POST /api/orders with JSON body → data appears here and in Compass",
                imageStorage = cloudinarySettings.Value.IsConfigured
                    ? $"Cloudinary ({cloudinarySettings.Value.CloudName})"
                    : "local /uploads"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { success = false, error = ex.Message });
        }
    }

    /// <summary>Resend config + orders without customer email (debug).</summary>
    [HttpGet("resend")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendStatus(CancellationToken ct)
    {
        var s = resendSettings.Value;
        var ordersWithoutEmail = await db.Orders.CountDocumentsAsync(
            Builders<Order>.Filter.Or(
                Builders<Order>.Filter.Eq(o => o.CustomerEmail, null),
                Builders<Order>.Filter.Eq(o => o.CustomerEmail, "")),
            cancellationToken: ct);

        return Ok(new
        {
            success = true,
            configured = s.IsConfigured,
            canSendToCustomerEmail = s.CanSendToCustomerEmail,
            fromEmail = s.FromEmail,
            testSender = s.IsResendTestSender,
            sendOnOrderCreated = s.SendEmailOnOrderCreated,
            sendOnPaymentSubmitted = s.SendEmailOnPaymentSubmitted,
            ordersWithoutCustomerEmail = ordersWithoutEmail,
            fix = s.IsResendTestSender
                ? "Verify domain resend.com/domains (gmi.so) → beddel FromEmail: E-commerce@gmi.so — markaas email waxaa u tagaya macmiil (form email)"
                : ordersWithoutEmail > 0
                    ? "Dalabyada qaar email ma laha — samee dalab cusub email leh"
                    : "OK — email macmiil waa la diri karaa"
        });
    }

    /// <summary>Development: tijaabi Resend — POST body { "to": "your@email.com" }</summary>
    [HttpPost("resend-test")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendTest([FromBody] SendEmailRequest request, CancellationToken ct)
    {
        if (!env.IsDevelopment())
            return NotFound();

        if (string.IsNullOrWhiteSpace(request.To))
            return BadRequest(new { success = false, message = "Geli { \"to\": \"email@example.com\" }" });

        var result = await emailService.SendAsync(
            request.To,
            "Assisted E-commerce — tijaabo",
            "<p>Haddii aad tan aragto, Resend wuu shaqaynayaa.</p>",
            ct);

        return Ok(new
        {
            success = result.Success,
            messageId = result.MessageId,
            error = result.ErrorMessage
        });
    }

    /// <summary>Development: tirtir xogta — IDs dib u bilaab USR-001, ORD-001-01</summary>
    [HttpPost("reset-data")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetData(CancellationToken ct)
    {
        if (!env.IsDevelopment())
            return NotFound();

        await seeder.ResetTransactionalDataAsync(ct);
        return Ok(new
        {
            success = true,
            message = "Xogta waa la tirtiray. Dalab cusub: User 01 → order1.",
            nextIds = new { userId = "01", orderId = "order1" }
        });
    }
}
