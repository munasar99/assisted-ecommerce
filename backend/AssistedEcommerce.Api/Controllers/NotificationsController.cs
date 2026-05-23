using AssistedEcommerce.Api.DTOs;
using AssistedEcommerce.Api.Exceptions;
using AssistedEcommerce.Api.Infrastructure;
using AssistedEcommerce.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AssistedEcommerce.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize(Policy = "AdminOrDevelopment")]
public class NotificationsController(
    IEmailService emailService,
    INotificationService notificationService,
    IOptions<ResendSettings> resendOptions) : ControllerBase
{
    /// <summary>POST test Resend — POST /api/notifications/email/test</summary>
    [HttpPost("email/test")]
    public async Task<ActionResult<ApiResponse<SendEmailResponse>>> SendTestEmail(
        [FromBody] SendEmailRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.To))
            throw new ApiException("Email recipient (to) is required.");

        var settings = resendOptions.Value;
        if (!settings.IsConfigured)
            throw new ApiException(
                "Resend ma dhameystirna. Geli Resend:Enabled=true iyo Resend:ApiKey appsettings.Local.json.");

        var (subject, html) = BuildTestContent(settings, request);

        var result = await emailService.SendAsync(request.To, subject, html, ct);
        if (!result.Success)
            throw new ApiException(result.ErrorMessage ?? "Resend ma dirin email.");

        return Ok(new ApiResponse<SendEmailResponse>(
            true,
            new SendEmailResponse(result.MessageId!, request.To.Trim(), subject.Trim())));
    }

    /// <summary>Dir email order-ka (customerEmail) — POST /api/notifications/email/order/{orderId}</summary>
    [HttpPost("email/order/{orderId}")]
    public async Task<ActionResult<ApiResponse<SendEmailResponse>>> SendOrderEmail(
        string orderId,
        CancellationToken ct)
    {
        var result = await notificationService.SendOrderEmailAsync(orderId, ct);
        if (!result.Success)
            throw new ApiException(result.ErrorMessage ?? "Email lama dirin.");

        return Ok(new ApiResponse<SendEmailResponse>(
            true,
            new SendEmailResponse(result.MessageId ?? "ok", orderId, "Order notification")));
    }

    /// <summary>GET Resend config (no secrets).</summary>
    [HttpGet("email/status")]
    public ActionResult<ApiResponse<object>> EmailStatus()
    {
        var s = resendOptions.Value;
        return Ok(new ApiResponse<object>(true, new
        {
            enabled = s.Enabled,
            configured = s.IsConfigured,
            sendOnOrderCreated = s.SendEmailOnOrderCreated,
            sendOnPaymentSubmitted = s.SendEmailOnPaymentSubmitted,
            fromEmail = s.FromEmail,
            fromName = s.FromName,
            note = s.FromEmail.Contains("resend.dev", StringComparison.OrdinalIgnoreCase)
                ? "Tijaabo: Resend waxaad u diri kartaa kaliya email-ka aad ku diiwaangelisay Resend.com (ilaa domain la verify gareeyo)."
                : null,
            brandName = s.BrandName,
            supportPhone = s.SupportPhone,
            supportEmail = s.SupportEmail,
            templates = s.Templates
        }));
    }

    private static (string Subject, string Html) BuildTestContent(ResendSettings settings, SendEmailRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Html))
        {
            var html = request.Html.Contains("<html", StringComparison.OrdinalIgnoreCase)
                ? request.Html
                : EmailTemplateRenderer.WrapEmailDocument(request.Html, "");
            var subj = string.IsNullOrWhiteSpace(request.Subject) ? "Test Assisted E-commerce" : request.Subject;
            return (subj, html);
        }

        var sample = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["customerName"] = "Macmiil",
            ["orderId"] = "order99",
            ["status"] = "WaitingPayment",
            ["statusLabel"] = "Sugitaan lacag",
            ["brandName"] = settings.BrandName,
            ["supportPhone"] = settings.SupportPhone,
            ["supportEmail"] = settings.SupportEmail,
        };
        var body = EmailTemplateRenderer.Apply(settings.Templates.OrderStatusHtml, sample);
        var footer = EmailTemplateRenderer.Apply(settings.Templates.FooterHtml, sample);
        var subject = string.IsNullOrWhiteSpace(request.Subject)
            ? EmailTemplateRenderer.Apply(settings.Templates.OrderStatusSubject, sample)
            : request.Subject;
        return (subject, EmailTemplateRenderer.WrapEmailDocument(body, footer));
    }
}
