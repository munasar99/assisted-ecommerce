using AssistedEcommerce.Api.Constants;
using AssistedEcommerce.Api.DTOs;
using AssistedEcommerce.Api.Exceptions;
using AssistedEcommerce.Api.Infrastructure;
using AssistedEcommerce.Api.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace AssistedEcommerce.Api.Services;

public interface IPaymentService
{
    Task<PagedResult<PaymentDto>> GetPaymentsAsync(
        int page, int pageSize, string? status, string? orderId, CancellationToken ct = default);
    Task<PaymentDto> GetPaymentByIdAsync(string paymentId, CancellationToken ct = default);
    Task<PaymentDto> CreatePaymentAsync(CreatePaymentRequest request, string adminId, CancellationToken ct = default);
    Task<PaymentDto> UpdatePaymentAsync(string paymentId, UpdatePaymentRequest request, string adminId, CancellationToken ct = default);
    Task DeletePaymentAsync(string paymentId, string adminId, CancellationToken ct = default);
    Task<PaymentUploadResponse> UploadPaymentAsync(
        string orderId,
        string phone,
        string payerPhone,
        string? paymentMethod,
        IFormFile file,
        CancellationToken ct = default);
}

public class PaymentService(
    MongoDbContext db,
    IIdGeneratorService idGenerator,
    IFileStorageService fileStorage,
    IPaymentScreenshotVerifier screenshotVerifier,
    INotificationService notificationService,
    IAuditService auditService,
    IOptions<PricingSettings> pricingOptions) : IPaymentService
{
    private static readonly string[] AllowedUploadStatuses =
    [
        OrderStatuses.InvoiceSent,
        OrderStatuses.WaitingPayment
    ];

    public async Task<PagedResult<PaymentDto>> GetPaymentsAsync(
        int page, int pageSize, string? status, string? orderId, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var filter = Builders<Payment>.Filter.Empty;
        if (!string.IsNullOrWhiteSpace(status))
            filter &= Builders<Payment>.Filter.Eq(p => p.Status, status);
        if (!string.IsNullOrWhiteSpace(orderId))
            filter &= Builders<Payment>.Filter.Eq(p => p.OrderId, orderId.Trim());

        var total = await db.Payments.CountDocumentsAsync(filter, cancellationToken: ct);
        var items = await db.Payments.Find(filter)
            .SortByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(ct);

        var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        return new PagedResult<PaymentDto>(items.Select(Map).ToList(), page, pageSize, total, totalPages);
    }

    public async Task<PaymentDto> GetPaymentByIdAsync(string paymentId, CancellationToken ct = default)
    {
        var payment = await FindPaymentAsync(paymentId, ct);
        return Map(payment);
    }

    public async Task<PaymentDto> CreatePaymentAsync(CreatePaymentRequest request, string adminId, CancellationToken ct = default)
    {
        var order = await db.Orders.Find(o => o.OrderId == request.OrderId.Trim()).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Order not found.");

        var status = string.IsNullOrWhiteSpace(request.Status) ? PaymentStatuses.UnderReview : request.Status.Trim();
        if (!PaymentStatuses.All.Contains(status))
            throw new ApiException("Invalid payment status.");

        var payment = new Payment
        {
            PaymentId = await idGenerator.NextPaymentIdAsync(ct),
            OrderId = order.OrderId,
            UserId = order.UserId,
            PayerPhone = string.IsNullOrWhiteSpace(request.PayerPhone) ? null : UserService.NormalizePhone(request.PayerPhone),
            PaymentMethod = request.PaymentMethod?.Trim(),
            ScreenshotUrl = request.ScreenshotUrl,
            AmountUsd = request.AmountUsd > 0 ? request.AmountUsd : order.TotalAmountUsd,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await db.Payments.InsertOneAsync(payment, cancellationToken: ct);
        await SyncOrderFromPaymentAsync(order, payment, adminId, ct);
        await auditService.LogAsync(adminId, "CREATE_PAYMENT", "Payments", payment.PaymentId);

        return Map(payment);
    }

    public async Task<PaymentDto> UpdatePaymentAsync(
        string paymentId, UpdatePaymentRequest request, string adminId, CancellationToken ct = default)
    {
        var payment = await FindPaymentAsync(paymentId, ct);
        var order = await db.Orders.Find(o => o.OrderId == payment.OrderId).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Order not found.");

        if (!string.IsNullOrWhiteSpace(request.PayerPhone))
            payment.PayerPhone = UserService.NormalizePhone(request.PayerPhone);
        if (request.PaymentMethod is not null)
            payment.PaymentMethod = string.IsNullOrWhiteSpace(request.PaymentMethod) ? null : request.PaymentMethod.Trim();
        if (request.ScreenshotUrl is not null)
            payment.ScreenshotUrl = request.ScreenshotUrl;
        if (request.AmountUsd is { } amount && amount > 0)
            payment.AmountUsd = amount;

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!PaymentStatuses.All.Contains(request.Status))
                throw new ApiException("Invalid payment status.");
            payment.Status = request.Status;
        }

        payment.UpdatedAt = DateTime.UtcNow;
        await db.Payments.ReplaceOneAsync(p => p.Id == payment.Id, payment, cancellationToken: ct);
        await SyncOrderFromPaymentAsync(order, payment, adminId, ct);
        await auditService.LogAsync(adminId, "UPDATE_PAYMENT", "Payments", payment.PaymentId);

        return Map(payment);
    }

    public async Task DeletePaymentAsync(string paymentId, string adminId, CancellationToken ct = default)
    {
        var payment = await FindPaymentAsync(paymentId, ct);
        await db.Payments.DeleteOneAsync(p => p.Id == payment.Id, ct);
        await auditService.LogAsync(adminId, "DELETE_PAYMENT", "Payments", payment.PaymentId);
    }

    public async Task<PaymentUploadResponse> UploadPaymentAsync(
        string orderId,
        string phone,
        string payerPhone,
        string? paymentMethod,
        IFormFile file,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(payerPhone))
            throw new ApiException("Payment number is required.");

        var normalizedPhone = UserService.NormalizePhone(phone);
        var user = await db.Users.Find(u => u.Phone == normalizedPhone).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Order not found.");

        var order = await db.Orders.Find(o => o.OrderId == orderId && o.UserId == user.UserId).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Order not found.");

        if (!AllowedUploadStatuses.Contains(order.Status))
            throw new ApiException("Payment upload is not allowed for the current order status.");

        var alreadySubmitted = await db.Payments
            .Find(p => p.OrderId == orderId && p.Status == PaymentStatuses.UnderReview)
            .AnyAsync(ct);
        if (alreadySubmitted || order.Status == OrderStatuses.PaymentReview)
            throw new ApiException("Screenshot lacagta horay ayaa loo soo diray. Sug xaqiijinta admin.");

        fileStorage.ValidateFile(file);
        await using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, ct);
        var bytes = buffer.ToArray();

        var p = pricingOptions.Value;
        var (_, _, _, expectedTotal) = OrderPricingHelper.ComputeTotals(
            order.ProductUnitPriceUsd,
            order.Quantity,
            order.DeliveryFee,
            p.KgFeePerKgUsd,
            p.ServiceFeePerItemUsd);

        var verification = await screenshotVerifier.VerifyAsync(
            new MemoryStream(bytes),
            expectedTotal,
            DateTime.UtcNow,
            ct);
        if (!verification.IsValid)
            throw new ApiException(verification.ErrorMessage ?? "Screenshot-ka lama aqbalin.");

        var uploadFile = FormFileHelper.FromBytes(bytes, file);
        var url = await fileStorage.SaveAsync(uploadFile, $"payments/{orderId}", ct);
        var payer = UserService.NormalizePhone(payerPhone);

        order.PayerPhone = payer;
        order.PaymentScreenshotUrl = url;
        order.Status = OrderStatuses.PaymentReview;
        order.UpdatedAt = DateTime.UtcNow;
        var methodNote = string.IsNullOrWhiteSpace(paymentMethod) ? "" : $" via {paymentMethod.Trim()}";
        order.StatusHistory.Add(new StatusHistoryEntry
        {
            Status = OrderStatuses.PaymentReview,
            At = DateTime.UtcNow,
            By = user.UserId,
            Note = $"Payment sent from {order.PayerPhone}{methodNote}"
        });

        await db.Orders.ReplaceOneAsync(o => o.Id == order.Id, order, cancellationToken: ct);

        var payment = new Payment
        {
            PaymentId = await idGenerator.NextPaymentIdAsync(ct),
            OrderId = order.OrderId,
            UserId = order.UserId,
            PayerPhone = payer,
            PaymentMethod = paymentMethod?.Trim(),
            ScreenshotUrl = url,
            AmountUsd = expectedTotal,
            Status = PaymentStatuses.UnderReview,
            ScreenshotDetectedAmountUsd = verification.DetectedAmountUsd,
            ScreenshotDetectedAtUtc = verification.DetectedAtUtc,
            ScreenshotVerificationNote = verification.OcrSnippet,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await db.Payments.InsertOneAsync(payment, cancellationToken: ct);

        var emailResult = await notificationService.NotifyOrderSubmittedAsync(
            order.OrderId,
            order.CustomerEmail,
            order.CustomerFullName,
            expectedTotal,
            ct);

        var emailSent = emailResult?.Success == true;
        string? emailError = emailResult?.ErrorMessage;
        if (!emailSent && string.IsNullOrWhiteSpace(emailError))
        {
            emailError = ResendErrorTranslator.ToSomali(
                string.IsNullOrWhiteSpace(order.CustomerEmail)
                    ? "Dalabkan email ma lahan"
                    : "Resend ma configured — hubi appsettings.Local.json kadib dotnet run");
        }
        else if (!emailSent && !string.IsNullOrWhiteSpace(emailError))
        {
            emailError = ResendErrorTranslator.ToSomali(emailError);
        }

        return new PaymentUploadResponse(
            payment.PaymentId,
            order.OrderId,
            order.Status,
            url,
            emailSent,
            emailError);
    }

    private async Task SyncOrderFromPaymentAsync(Order order, Payment payment, string by, CancellationToken ct)
    {
        order.PayerPhone = payment.PayerPhone;
        order.PaymentScreenshotUrl = payment.ScreenshotUrl;
        order.UpdatedAt = DateTime.UtcNow;

        var newOrderStatus = payment.Status switch
        {
            PaymentStatuses.Confirmed => OrderStatuses.Confirmed,
            PaymentStatuses.Rejected => OrderStatuses.WaitingPayment,
            PaymentStatuses.UnderReview => OrderStatuses.PaymentReview,
            _ => order.Status
        };

        if (newOrderStatus != order.Status && OrderStatuses.CanTransition(order.Status, newOrderStatus))
        {
            order.Status = newOrderStatus;
            order.StatusHistory.Add(new StatusHistoryEntry
            {
                Status = newOrderStatus,
                At = DateTime.UtcNow,
                By = by,
                Note = $"Payment {payment.PaymentId} → {payment.Status}"
            });
        }

        await db.Orders.ReplaceOneAsync(o => o.Id == order.Id, order, cancellationToken: ct);

        var user = await db.Users.Find(u => u.UserId == order.UserId).FirstOrDefaultAsync(ct);
        if (user is not null)
            await notificationService.NotifyOrderStatusAsync(
            user.Phone, order.OrderId, order.Status, order.CustomerEmail, order.CustomerFullName, ct);
    }

    private async Task<Payment> FindPaymentAsync(string paymentId, CancellationToken ct)
    {
        var payment = await db.Payments.Find(p => p.PaymentId == paymentId).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Payment not found.");
        return payment;
    }

    private static PaymentDto Map(Payment p) =>
        new(p.PaymentId, p.OrderId, p.UserId, p.PayerPhone, p.PaymentMethod, p.ScreenshotUrl,
            p.AmountUsd, p.Status, p.CreatedAt, p.UpdatedAt);
}
