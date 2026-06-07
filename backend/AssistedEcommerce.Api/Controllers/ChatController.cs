using AssistedEcommerce.Api.DTOs;
using AssistedEcommerce.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssistedEcommerce.Api.Controllers;

/// <summary>Proxy chatbot AI requests to Node.js backend.</summary>
[ApiController]
[Route("api/chat")]
[AllowAnonymous]
public class ChatController(IAiBackendClient aiBackend) : ControllerBase
{
    [HttpPost("message")]
    public async Task<ActionResult<ApiResponse<object>>> Message(
        [FromBody] ChatMessageRequest request, CancellationToken ct)
    {
        if (!aiBackend.IsEnabled)
            return StatusCode(503, new ApiResponse<object>(false, null, "AI Backend ma socdo. Hubi Node.js server."));

        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new ApiResponse<object>(false, null, "Message is required"));

        var result = await aiBackend.SendChatMessageAsync(request.Message.Trim(), request.SessionId, ct);
        if (result is null)
            return StatusCode(502, new ApiResponse<object>(false, null, "AI Backend jawaab ma celin."));

        return Ok(new ApiResponse<object>(true, new
        {
            reply = result.Reply,
            provider = result.Provider,
            sessionId = result.SessionId
        }));
    }

    [HttpPost("validate-order")]
    public async Task<ActionResult<ApiResponse<object>>> ValidateOrder(
        [FromBody] ChatValidateOrderRequest request, CancellationToken ct)
    {
        if (!aiBackend.IsEnabled)
            return StatusCode(503, new ApiResponse<object>(false, null, "AI Backend ma socdo."));

        var result = await aiBackend.ValidateOrderAsync(new AiOrderValidationRequest(
            request.ProductName,
            request.ProductUrl,
            request.Price,
            request.Quantity,
            request.Phone,
            request.ImageBase64), ct);

        if (result is null)
            return StatusCode(502, new ApiResponse<object>(false, null, "AI validation failed."));

        return Ok(new ApiResponse<object>(true, result));
    }

    [HttpGet("history/{sessionId}")]
    public ActionResult<ApiResponse<object>> History(string sessionId)
    {
        return Ok(new ApiResponse<object>(true, new { sessionId, messages = Array.Empty<object>(), note = "History stored in Node.js session store" }));
    }
}

public record ChatMessageRequest(string Message, string? SessionId = null);

public record ChatValidateOrderRequest(
    string ProductUrl,
    string Phone,
    decimal Price,
    int Quantity = 1,
    string? ProductName = null,
    string? ImageBase64 = null);
