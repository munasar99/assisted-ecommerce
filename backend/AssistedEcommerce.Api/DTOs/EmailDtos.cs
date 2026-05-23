namespace AssistedEcommerce.Api.DTOs;

public record SendEmailRequest(string To, string? Subject, string? Html);

public record SendEmailResponse(string Id, string To, string Subject);

public record EmailSendResult(bool Success, string? MessageId, string? ErrorMessage);
