using System.Text.RegularExpressions;

namespace AssistedEcommerce.Api.Infrastructure;

public static partial class PhoneNormalizer
{
    /// <summary>Qaab hal ah: +252XXXXXXXXX (9 lambar kadib 252).</summary>
    public static string Normalize(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return string.Empty;

        var digits = DigitsOnly().Replace(phone.Trim(), "");
        if (digits.Length == 0)
            return string.Empty;

        if (digits.StartsWith("252", StringComparison.Ordinal) && digits.Length >= 12)
            digits = digits[3..];
        else if (digits.StartsWith("252", StringComparison.Ordinal) && digits.Length > 9)
            digits = digits[3..];

        if (digits.StartsWith('0') && digits.Length > 9)
            digits = digits[1..];

        if (digits.Length > 9)
            digits = digits[^9..];

        if (digits.Length < 9)
            return "+" + digits;

        return "+252" + digits;
    }

    public static string CanonicalKey(string phone) => Normalize(phone);

    [GeneratedRegex(@"\D")]
    private static partial Regex DigitsOnly();
}
