using System.Net.Http.Json;
using System.Text.Json;
using AssistedEcommerce.Api.Infrastructure;
using Microsoft.Extensions.Options;

namespace AssistedEcommerce.Api.Services;

public interface IAiBackendClient
{
    bool IsEnabled { get; }
    Task<AiChatResponse?> SendChatMessageAsync(string message, string? sessionId, CancellationToken ct = default);
    Task<AiValidationResponse?> ValidateOrderAsync(AiOrderValidationRequest request, CancellationToken ct = default);
    Task<AiValidationResponse?> VerifyReceiptAsync(AiReceiptVerificationRequest request, CancellationToken ct = default);
    Task<AiPaymentNotifyResponse?> NotifyPaymentAsync(AiPaymentNotifyRequest request, CancellationToken ct = default);
    Task<bool> HealthCheckAsync(CancellationToken ct = default);
    Task<bool> IsAiReadyAsync(CancellationToken ct = default);
    Task<JsonElement?> GetNotifyPreviewAsync(CancellationToken ct = default);
}

public record AiChatResponse(string Reply, string? Provider, string? SessionId);
public record AiValidationResponse(string Status, string? Message, string? Provider, JsonElement? Details);
public record AiPaymentNotifyResponse(AiValidationResponse? Ai, JsonElement? Notifications);
public record AiOrderValidationRequest(
    string? ProductName,
    string ProductUrl,
    decimal Price,
    int Quantity,
    string Phone,
    string? ImageBase64);
public record AiReceiptVerificationRequest(
    string OrderId,
    decimal ExpectedAmount,
    decimal SubmittedAmount,
    string? PaymentMethod,
    string ImageBase64);
public record AiPaymentNotifyRequest(
    string OrderId,
    string? CustomerName,
    string? CustomerEmail,
    string? CustomerPhone,
    string? ProductName,
    decimal Amount,
    string? PaymentMethod,
    string? ImageBase64,
    decimal? ExpectedAmount);

public class AiBackendClient(
    HttpClient http,
    IOptions<AiBackendSettings> options,
    ILogger<AiBackendClient> logger) : IAiBackendClient
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public bool IsEnabled => options.Value.IsConfigured;

    public async Task<AiChatResponse?> SendChatMessageAsync(string message, string? sessionId, CancellationToken ct = default)
    {
        if (!IsEnabled) return null;

        var payload = new { message, sessionId = sessionId ?? "default" };
        var response = await PostAsync<AiApiWrapper<AiChatData>>("/api/chat/message", payload, ct);
        if (response?.Data is null) return null;
        return new AiChatResponse(response.Data.Reply, response.Data.Provider, response.Data.SessionId);
    }

    public async Task<AiValidationResponse?> ValidateOrderAsync(AiOrderValidationRequest request, CancellationToken ct = default)
    {
        if (!IsEnabled || !options.Value.ValidateOrders) return null;
        var response = await PostAsync<AiApiWrapper<AiValidationData>>("/api/chat/validate-order", request, ct);
        return MapValidation(response?.Data);
    }

    public async Task<AiValidationResponse?> VerifyReceiptAsync(AiReceiptVerificationRequest request, CancellationToken ct = default)
    {
        if (!IsEnabled || !options.Value.VerifyReceipts) return null;
        var response = await PostAsync<AiApiWrapper<AiValidationData>>("/api/chat/verify-receipt", request, ct);
        return MapValidation(response?.Data);
    }

    public async Task<AiPaymentNotifyResponse?> NotifyPaymentAsync(AiPaymentNotifyRequest request, CancellationToken ct = default)
    {
        if (!IsEnabled || !options.Value.SendPaymentNotifications) return null;
        var response = await PostAsync<AiApiWrapper<AiPaymentNotifyData>>("/api/notify/payment", request, ct);
        if (response?.Data is null) return null;
        return new AiPaymentNotifyResponse(
            MapValidation(response.Data.Ai),
            response.Data.Notifications);
    }

    public async Task<bool> IsAiReadyAsync(CancellationToken ct = default)
    {
        if (!IsEnabled) return false;
        try
        {
            await using var stream = await http.GetStreamAsync("/api/health", ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            if (doc.RootElement.TryGetProperty("features", out var features) &&
                features.TryGetProperty("ai", out var ai))
                return ai.GetBoolean();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AI Backend features check failed");
        }
        return false;
    }

    public async Task<bool> HealthCheckAsync(CancellationToken ct = default)
    {
        if (!IsEnabled) return false;
        try
        {
            var res = await http.GetAsync("/api/health", ct);
            return res.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AI Backend health check failed");
            return false;
        }
    }

    public async Task<JsonElement?> GetNotifyPreviewAsync(CancellationToken ct = default)
    {
        if (!IsEnabled) return null;
        try
        {
            var res = await http.GetAsync("/api/notify/preview", ct);
            if (!res.IsSuccessStatusCode) return null;
            await using var stream = await res.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            if (doc.RootElement.TryGetProperty("data", out var data))
                return data.Clone();
            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AI Backend notify preview failed");
            return null;
        }
    }

    private async Task<T?> PostAsync<T>(string path, object payload, CancellationToken ct)
    {
        try
        {
            var res = await http.PostAsJsonAsync(path, payload, ct);
            if (!res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync(ct);
                logger.LogWarning("AI Backend {Path} failed: {Status} {Body}", path, res.StatusCode, body);
                return default;
            }
            return await res.Content.ReadFromJsonAsync<T>(JsonOpts, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AI Backend {Path} unreachable", path);
            return default;
        }
    }

    private static AiValidationResponse? MapValidation(AiValidationData? data) =>
        data is null ? null : new AiValidationResponse(data.Status, data.Message, data.Provider, data.Details);

    private sealed class AiApiWrapper<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
    }

    private sealed class AiChatData
    {
        public string Reply { get; set; } = "";
        public string? Provider { get; set; }
        public string? SessionId { get; set; }
    }

    private sealed class AiValidationData
    {
        public string Status { get; set; } = "WARNING";
        public string? Message { get; set; }
        public string? Provider { get; set; }
        public JsonElement? Details { get; set; }
    }

    private sealed class AiPaymentNotifyData
    {
        public AiValidationData? Ai { get; set; }
        public JsonElement? Notifications { get; set; }
    }
}
