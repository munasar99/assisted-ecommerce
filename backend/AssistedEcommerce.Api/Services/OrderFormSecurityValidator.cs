using AssistedEcommerce.Api.DTOs;
using AssistedEcommerce.Api.Exceptions;
using AssistedEcommerce.Api.Infrastructure;

namespace AssistedEcommerce.Api.Services;

public static class OrderFormSecurityValidator
{

    public static void ValidateCreateRequest(
        CreateOrderRequest request,
        OrderFormSecuritySettings security)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) || request.FullName.Trim().Length < 2)
            throw new ApiException("Geli magaca saxda ah.");

        if (!IsValidPhone(request.Phone))
            throw new ApiException("Telefoonka ma saxna (+2526...).");

        if (!IsValidEmail(request.Email))
            throw new ApiException("Geli email sax ah.");

        if (string.IsNullOrWhiteSpace(request.ProductUrl) || !Uri.TryCreate(request.ProductUrl.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new ApiException("Link alaabta waa inuu noqdaa URL sax ah (https://).");

        if (request.Quantity < 1 || request.Quantity > security.MaxQuantity)
            throw new ApiException($"Tirada waa 1 ilaa {security.MaxQuantity} kaliya.");

        if (request.ProductUnitPriceUsd < security.MinProductUnitPriceUsd
            || request.ProductUnitPriceUsd > security.MaxProductUnitPriceUsd)
            throw new ApiException($"Qiimaha alaabta waa inuu u dhexeeyaa ${security.MinProductUnitPriceUsd} iyo ${security.MaxProductUnitPriceUsd}.");
    }

    public static bool IsValidPhone(string phone)
    {
        var normalized = PhoneNormalizer.Normalize(phone);
        return normalized.Length >= 12 && normalized.StartsWith("+252", StringComparison.Ordinal);
    }

    public static string NormalizePhone(string phone) => PhoneNormalizer.Normalize(phone);

    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;
        var trimmed = email.Trim();
        return trimmed.Contains('@', StringComparison.Ordinal)
               && trimmed.Length >= 5
               && trimmed.IndexOf('@') > 0
               && trimmed.IndexOf('@') < trimmed.Length - 2;
    }

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
