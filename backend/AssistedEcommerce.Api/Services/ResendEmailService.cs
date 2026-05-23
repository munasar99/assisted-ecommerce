using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AssistedEcommerce.Api.DTOs;
using AssistedEcommerce.Api.Infrastructure;
using Microsoft.Extensions.Options;

namespace AssistedEcommerce.Api.Services;

public interface IEmailService
{
    Task<EmailSendResult> SendAsync(string to, string subject, string html, CancellationToken ct = default);
}

public class ResendEmailService(
    HttpClient http,
    IOptions<ResendSettings> options,
    ILogger<ResendEmailService> logger) : IEmailService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly Uri ResendEmailsUri = new("https://api.resend.com/emails");

    private readonly ResendSettings _settings = options.Value;

    public async Task<EmailSendResult> SendAsync(string to, string subject, string html, CancellationToken ct = default)
    {
        if (!_settings.IsConfigured)
        {
            const string msg = "Resend ma furan: Enabled=false ama ApiKey ma jiro appsettings.Local.json.";
            logger.LogWarning("{Msg}", msg);
            return new EmailSendResult(false, null, ResendErrorTranslator.ToSomali(msg));
        }

        if (string.IsNullOrWhiteSpace(to))
            return new EmailSendResult(false, null, ResendErrorTranslator.ToSomali("customerEmail madhan"));

        var customerEmail = to.Trim().ToLowerInvariant();

        // 1) Isku day in toos loo diro macmiilka (waxa uu form-ka geliyay)
        var result = await SendOnceAsync(customerEmail, subject.Trim(), html, customerEmail, ct);
        if (result.Success)
            return result;

        // 2) Tijaabo redirect (optional) — default OFF; macmiil email waa mudnaan
        if (_settings.UseDevelopmentRedirect && ShouldRetryToRedirect(result.ErrorMessage))
        {
            var redirect = _settings.DevelopmentRedirectTo!.Trim().ToLowerInvariant();
            logger.LogWarning(
                "Resend test: macmiil {Customer} lama dirin, isku day redirect → {Redirect}",
                customerEmail, redirect);

            var devSubject = $"[DEV — macmiil: {customerEmail}] {subject.Trim()}";
            var devHtml =
                $"<p style=\"color:#b45309;font-size:13px;\">Tijaabo Resend: email-ka macmiilka waa <strong>{customerEmail}</strong></p>"
                + html;

            var fallback = await SendOnceAsync(redirect, devSubject, devHtml, customerEmail, ct);
            if (fallback.Success)
            {
                logger.LogInformation(
                    "Email DEV redirect guulaystay → {Redirect} (macmiilka dhabta: {Customer})",
                    redirect, customerEmail);
                return fallback;
            }

            logger.LogError(
                "DEV redirect FAILED → {Redirect}. Hubi DevelopmentRedirectTo = email-ka Resend login (ma aha e-commerce@gmi.so haddii taasi aan la diiwaangelin).",
                redirect);

            return new EmailSendResult(
                false,
                null,
                ResendErrorTranslator.ToSomali(fallback.ErrorMessage ?? result.ErrorMessage));
        }

        return new EmailSendResult(false, null, ResendErrorTranslator.ToSomali(result.ErrorMessage));
    }

    private bool ShouldRetryToRedirect(string? error) =>
        _settings.IsResendTestSender
        && !string.IsNullOrWhiteSpace(_settings.DevelopmentRedirectTo)
        && error is not null
        && (error.Contains("only send testing", StringComparison.OrdinalIgnoreCase)
            || error.Contains("your own email", StringComparison.OrdinalIgnoreCase)
            || error.Contains("validation_error", StringComparison.OrdinalIgnoreCase));

    private async Task<EmailSendResult> SendOnceAsync(
        string recipient,
        string subject,
        string html,
        string customerEmailForLog,
        CancellationToken ct)
    {
        var from = string.IsNullOrWhiteSpace(_settings.FromName)
            ? _settings.FromEmail.Trim()
            : $"{_settings.FromName.Trim()} <{_settings.FromEmail.Trim()}>";

        var payload = new Dictionary<string, object>
        {
            ["from"] = from,
            ["to"] = new[] { recipient },
            ["subject"] = subject,
            ["html"] = html
        };

        if (!string.IsNullOrWhiteSpace(_settings.SupportEmail))
            payload["reply_to"] = _settings.SupportEmail.Trim();

        using var request = new HttpRequestMessage(HttpMethod.Post, ResendEmailsUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey.Trim());
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload, JsonOptions),
            Encoding.UTF8,
            "application/json");

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Resend HTTP error → {To}", recipient);
            return new EmailSendResult(false, null, $"Resend network error: {ex.Message}");
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            var err = ParseResendError(body) ?? $"Resend HTTP {(int)response.StatusCode}";
            logger.LogError("Resend failed → {To} (macmiil {Customer}): {Error}", recipient, customerEmailForLog, err);
            return new EmailSendResult(false, null, err);
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            var id = doc.RootElement.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
            logger.LogInformation("Resend OK id={Id} → {To}", id, recipient);
            return new EmailSendResult(true, id, null);
        }
        catch
        {
            return new EmailSendResult(true, "ok", null);
        }
    }

    private static string? ParseResendError(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("message", out var msg))
            {
                var text = msg.GetString();
                if (doc.RootElement.TryGetProperty("name", out var name))
                    return $"{name.GetString()}: {text}";
                return text;
            }
        }
        catch
        {
            // ignore
        }

        return body.Length > 240 ? body[..240] : body;
    }
}
