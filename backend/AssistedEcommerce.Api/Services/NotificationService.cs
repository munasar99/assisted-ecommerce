using AssistedEcommerce.Api.Constants;
using AssistedEcommerce.Api.DTOs;
using AssistedEcommerce.Api.Infrastructure;
using AssistedEcommerce.Api.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace AssistedEcommerce.Api.Services;

public interface INotificationService
{
    Task<EmailSendResult?> NotifyOrderStatusAsync(
        string phone,
        string orderId,
        string status,
        string? customerEmail = null,
        string? customerName = null,
        CancellationToken ct = default);

    Task<EmailSendResult> SendOrderEmailAsync(string orderId, CancellationToken ct = default);

    /// <summary>Email marka order la abuuro (macmiil kasta).</summary>
    Task<EmailSendResult?> NotifyOrderCreatedAsync(
        string orderId,
        string? customerEmail,
        string? customerName,
        decimal totalAmountUsd,
        CancellationToken ct = default);

    /// <summary>Email otomaatik marka macmiilku dhammeeyo lacag bixinta (foom + screenshot).</summary>
    Task<EmailSendResult?> NotifyOrderSubmittedAsync(
        string orderId,
        string? customerEmail,
        string? customerName,
        decimal totalAmountUsd,
        CancellationToken ct = default);
}

public class NotificationService(
    IEmailService emailService,
    IOptions<ResendSettings> resendOptions,
    MongoDbContext db,
    ILogger<NotificationService> logger) : INotificationService
{
    private readonly ResendSettings _resend = resendOptions.Value;

    public async Task<EmailSendResult?> NotifyOrderStatusAsync(
        string phone,
        string orderId,
        string status,
        string? customerEmail = null,
        string? customerName = null,
        CancellationToken ct = default)
    {
        logger.LogInformation(
            "[Notify] Order {OrderId} → {Status} (phone {Phone}, email {Email})",
            orderId, status, phone, string.IsNullOrWhiteSpace(customerEmail) ? "—" : customerEmail);

        if (string.IsNullOrWhiteSpace(customerEmail))
        {
            logger.LogWarning(
                "Email lama dirin order {OrderId}: customerEmail madhan (macmiilku ma gelin email form-ka).",
                orderId);
            return null;
        }

        return await SendToCustomerAsync(customerEmail, customerName, orderId, status, ct);
    }

    public async Task<EmailSendResult> SendOrderEmailAsync(string orderId, CancellationToken ct = default)
    {
        var order = await db.Orders.Find(o => o.OrderId == orderId.Trim()).FirstOrDefaultAsync(ct)
            ?? throw new Exceptions.NotFoundException("Order not found.");

        if (string.IsNullOrWhiteSpace(order.CustomerEmail))
            throw new Exceptions.ApiException(
                "Order-kan email ma lahan. Macmiilku waa inuu geliyaa email marka uu dalab sameeyo, ama admin ku dar PUT.");

        return await SendToCustomerAsync(
            order.CustomerEmail,
            order.CustomerFullName,
            order.OrderId,
            order.Status,
            ct);
    }

    public async Task<EmailSendResult?> NotifyOrderCreatedAsync(
        string orderId,
        string? customerEmail,
        string? customerName,
        decimal totalAmountUsd,
        CancellationToken ct = default)
    {
        if (!_resend.SendEmailOnOrderCreated)
            return null;

        if (string.IsNullOrWhiteSpace(customerEmail))
        {
            logger.LogWarning("OrderCreated email skipped {OrderId}: no customerEmail", orderId);
            return new EmailSendResult(
                false, null,
                ResendErrorTranslator.ToSomali("Dalabkan email ma lahan — geli email form-ka."));
        }

        return await SendTemplatedAsync(
            customerEmail,
            customerName,
            orderId,
            _resend.Templates.OrderCreatedSubject,
            _resend.Templates.OrderCreatedHtml,
            totalAmountUsd,
            ct);
    }

    public async Task<EmailSendResult?> NotifyOrderSubmittedAsync(
        string orderId,
        string? customerEmail,
        string? customerName,
        decimal totalAmountUsd,
        CancellationToken ct = default)
    {
        if (!_resend.SendEmailOnPaymentSubmitted)
        {
            logger.LogDebug("OrderSubmitted email disabled (SendEmailOnPaymentSubmitted=false).");
            return null;
        }

        logger.LogInformation(
            "[Notify] Order submitted {OrderId} → email {Email}, total ${Total:F2}",
            orderId,
            string.IsNullOrWhiteSpace(customerEmail) ? "—" : customerEmail,
            totalAmountUsd);

        if (string.IsNullOrWhiteSpace(customerEmail))
        {
            logger.LogWarning(
                "OrderSubmitted email lama dirin {OrderId}: customerEmail madhan.",
                orderId);
            return new EmailSendResult(
                false,
                null,
                ResendErrorTranslator.ToSomali("Dalabkan email ma lahan — geli email marka order la sameeyo."));
        }

        return await SendTemplatedAsync(
            customerEmail,
            customerName,
            orderId,
            _resend.Templates.OrderSubmittedSubject,
            _resend.Templates.OrderSubmittedHtml,
            totalAmountUsd,
            ct);
    }

    private async Task<EmailSendResult> SendTemplatedAsync(
        string customerEmail,
        string? customerName,
        string orderId,
        string subjectTemplate,
        string bodyTemplate,
        decimal totalAmountUsd,
        CancellationToken ct)
    {
        if (!_resend.IsConfigured)
        {
            var msg = "Resend ma configured. Geli Resend:Enabled=true iyo ApiKey appsettings.Local.json.";
            logger.LogError("{Msg}", msg);
            return new EmailSendResult(false, null, ResendErrorTranslator.ToSomali(msg));
        }

        var name = string.IsNullOrWhiteSpace(customerName) ? "Macmiil" : customerName.Trim();
        var total = $"${totalAmountUsd:F2}";
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["customerName"] = name,
            ["orderId"] = orderId,
            ["totalAmount"] = total,
            ["brandName"] = _resend.BrandName,
            ["supportPhone"] = _resend.SupportPhone,
            ["supportEmail"] = _resend.SupportEmail,
        };

        var subject = EmailTemplateRenderer.Apply(subjectTemplate, values);
        var bodyHtml = EmailTemplateRenderer.Apply(bodyTemplate, values);
        var footerHtml = EmailTemplateRenderer.Apply(_resend.Templates.FooterHtml, values);
        var html = EmailTemplateRenderer.WrapEmailDocument(bodyHtml, footerHtml);

        var result = await emailService.SendAsync(customerEmail.Trim(), subject, html, ct);
        if (!result.Success)
            logger.LogError("Email FAILED {OrderId} → {Email}: {Error}", orderId, customerEmail, result.ErrorMessage);
        else
            logger.LogInformation("Email SENT {OrderId} → {Email} id={Id}", orderId, customerEmail, result.MessageId);

        return result;
    }

    private async Task<EmailSendResult> SendToCustomerAsync(
        string customerEmail,
        string? customerName,
        string orderId,
        string status,
        CancellationToken ct)
    {
        if (!_resend.IsConfigured)
        {
            var msg = "Resend ma configured. Geli Resend:Enabled=true iyo ApiKey appsettings.Local.json.";
            logger.LogError("{Msg}", msg);
            return new EmailSendResult(false, null, msg);
        }

        var name = string.IsNullOrWhiteSpace(customerName) ? "Macmiil" : customerName.Trim();
        var statusLabel = StatusLabel(status);
        var values = BuildPlaceholderValues(name, orderId, status, statusLabel);

        var subject = EmailTemplateRenderer.Apply(_resend.Templates.OrderStatusSubject, values);
        var bodyHtml = EmailTemplateRenderer.Apply(_resend.Templates.OrderStatusHtml, values);
        var footerHtml = EmailTemplateRenderer.Apply(_resend.Templates.FooterHtml, values);
        var html = EmailTemplateRenderer.WrapEmailDocument(bodyHtml, footerHtml);

        var result = await emailService.SendAsync(customerEmail.Trim(), subject, html, ct);
        if (!result.Success)
            logger.LogError("Email FAILED order {OrderId} → {Email}: {Error}", orderId, customerEmail, result.ErrorMessage);
        else
            logger.LogInformation("Email SENT order {OrderId} → {Email} id={Id}", orderId, customerEmail, result.MessageId);

        return result;
    }

    private Dictionary<string, string> BuildPlaceholderValues(
        string customerName, string orderId, string status, string statusLabel) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["customerName"] = customerName,
            ["orderId"] = orderId,
            ["status"] = status,
            ["statusLabel"] = statusLabel,
            ["brandName"] = _resend.BrandName,
            ["supportPhone"] = _resend.SupportPhone,
            ["supportEmail"] = _resend.SupportEmail,
        };

    private static string StatusLabel(string status) => status switch
    {
        OrderStatuses.Pending => "Sugitaan",
        OrderStatuses.InvoiceSent => "Invoice la diray",
        OrderStatuses.WaitingPayment => "Sugitaan lacag",
        OrderStatuses.PaymentReview => "Lacagta waa la eegayaa",
        OrderStatuses.Confirmed => "La xaqiijiyay",
        OrderStatuses.OrderedFromSupplier => "Waxaa laga dalbaday alaab-bixiyaha",
        OrderStatuses.Shipping => "Wadada",
        OrderStatuses.ArrivedMogadishu => "Muqdisho yimid",
        OrderStatuses.OutForDelivery => "Gaarsiin socda",
        OrderStatuses.Delivered => "La keenay",
        _ => status
    };
}
