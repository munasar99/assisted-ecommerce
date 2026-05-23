namespace AssistedEcommerce.Api.Infrastructure;

public class PaymentVerificationSettings
{
    public const string SectionName = "PaymentVerification";

    public bool Enabled { get; set; } = true;

    /// <summary>Hubi taariikhda iyo waqtiga screenshot-ka (default: false — lacagta oo keliya).</summary>
    public bool VerifyDateAndTime { get; set; } = false;

    /// <summary>OCR adag — qoraal yar oo aan la aqrin karin.</summary>
    public bool StrictMode { get; set; } = true;

    public int MaxScreenshotAgeHours { get; set; } = 72;

    public int MaxFutureMinutes { get; set; } = 30;

    /// <summary>Kala duwanaanshaha la ogol yahay marka lacagta la hubiyo.</summary>
    public decimal AmountToleranceUsd { get; set; } = 0.02m;

    public int MinOcrTextLength { get; set; } = 6;

    public int MinImageWidth { get; set; } = 120;

    public int MinImageHeight { get; set; } = 120;

    public int MinPaymentKeywords { get; set; } = 0;

    public bool RequirePaymentKeywords { get; set; } = false;

    public bool RequireDateInImage { get; set; } = false;

    public bool RequireTimeInImage { get; set; } = false;
}
