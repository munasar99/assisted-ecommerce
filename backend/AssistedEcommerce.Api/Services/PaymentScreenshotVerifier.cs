using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using AssistedEcommerce.Api.Infrastructure;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.Processing;
using Tesseract;

namespace AssistedEcommerce.Api.Services;

public interface IPaymentScreenshotVerifier
{
    Task<PaymentScreenshotVerificationResult> VerifyAsync(
        Stream imageStream,
        decimal expectedAmountUsd,
        DateTime serverUtcNow,
        CancellationToken ct = default);
}

public partial class PaymentScreenshotVerifier(
    IOptions<PaymentVerificationSettings> options,
    ILogger<PaymentScreenshotVerifier> logger) : IPaymentScreenshotVerifier
{
    private static readonly string[] PaymentKeywords =
    [
        "sent", "paid", "payment", "transfer", "successful", "success", "received",
        "receipt", "transaction", "evc", "zaad", "edahab", "sahal", "premier", "taaj",
        "e-dahab", "mobile", "money", "lacag", "bixinta", "approved", "completed",
        "withdraw", "deposit", "balance", "merchant", "reference", "ref", "uwareejiyay",
        "uwareejisay", "bixiyay", "plus", "waafi", "evcplus", "evc plus", "haraagaagu"
    ];

    private static readonly string[] DateFormats =
    [
        "dd/MM/yyyy HH:mm:ss", "dd/MM/yyyy HH:mm", "dd/MM/yyyy",
        "MM/dd/yyyy HH:mm:ss", "MM/dd/yyyy HH:mm", "MM/dd/yyyy",
        "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm", "yyyy-MM-dd",
        "dd-MM-yyyy HH:mm", "dd-MM-yyyy",
        "d/M/yyyy H:mm", "M/d/yyyy H:mm"
    ];

    public async Task<PaymentScreenshotVerificationResult> VerifyAsync(
        Stream imageStream,
        decimal expectedAmountUsd,
        DateTime serverUtcNow,
        CancellationToken ct = default)
    {
        var settings = options.Value;
        if (!settings.Enabled)
            return PaymentScreenshotVerificationResult.Ok(expectedAmountUsd, serverUtcNow, null);

        await using var copy = new MemoryStream();
        await imageStream.CopyToAsync(copy, ct);
        var bytes = copy.ToArray();

        if (bytes.Length < 512)
            return PaymentScreenshotVerificationResult.Fail("Sawirka aad soo dirtay aad buu u yar yahay.");

        var dimensionError = ValidateImageDimensions(bytes, settings);
        if (dimensionError is not null)
            return PaymentScreenshotVerificationResult.Fail(dimensionError);

        var exifUtc = TryReadExifDateUtc(bytes);

        string ocrText;
        try
        {
            ocrText = await Task.Run(() => RunOcrWithFallback(bytes), ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "OCR failed (net + CLI): {Message}", ex.Message);
            return PaymentScreenshotVerificationResult.Fail(
                "OCR (akhrinta sawirka) ma shaqaynayo server-ka. Isku day mar kale ama sawir weyn oo cad. Haddii weli fashilmo, la xidhiidh taageerada.");
        }

        var compact = CompactText(ocrText);
        var snippet = compact.Length > 240 ? compact[..240] + "…" : compact;

        if (settings.StrictMode && compact.Length < settings.MinOcrTextLength)
            return PaymentScreenshotVerificationResult.Fail(
                "Screenshot-ku ma muujinayo qoraal cad. Soo rar sawirka app-ka lacagta oo dhammaystiran.",
                snippet);

        if (settings.RequirePaymentKeywords || settings.MinPaymentKeywords > 0)
        {
            var keywordHits = CountPaymentKeywords(compact);
            var required = settings.RequirePaymentKeywords
                ? Math.Max(1, settings.MinPaymentKeywords)
                : settings.MinPaymentKeywords;
            if (keywordHits < required)
                return PaymentScreenshotVerificationResult.Fail(
                    "Sawirkan uma eka screenshot lacag bixinta. Soo rar EVC/Zaad screenshot dhab ah.",
                    snippet);
        }

        var amountCheck = TryMatchExpectedAmount(compact, expectedAmountUsd, settings.AmountToleranceUsd);
        if (!amountCheck.Matched)
        {
            var msg = amountCheck.ClosestWrongAmount is { } wrong
                ? $"Screenshot-ku waxa uu muujiyaa {FormatMoney(wrong)}. Wadarta dalabka waa {FormatMoney(expectedAmountUsd)} — soo rar screenshot sax ah."
                : $"Lacagta {FormatMoney(expectedAmountUsd)} lama helin sawirka. Soo rar screenshot muujinaya lacagta saxda ah.";
            return PaymentScreenshotVerificationResult.Fail(msg, snippet);
        }

        var detectedAmount = amountCheck.DetectedAmount!.Value;

        if (!settings.VerifyDateAndTime)
        {
            logger.LogInformation(
                "Payment screenshot OK (amount only): expected ${Expected}, detected ${Detected}",
                expectedAmountUsd, detectedAmount);
            return PaymentScreenshotVerificationResult.Ok(detectedAmount, serverUtcNow, snippet);
        }

        var detectedAt = FindDateTime(compact, exifUtc);
        if (detectedAt is null && settings.RequireDateInImage)
            return PaymentScreenshotVerificationResult.Fail(
                "Taariikhda sawirka lama helin. Soo rar screenshot cusub.",
                snippet);

        if (detectedAt is null)
            detectedAt = exifUtc ?? serverUtcNow;

        if (settings.RequireTimeInImage && !TimeInTextPattern().IsMatch(compact) && exifUtc is null)
            return PaymentScreenshotVerificationResult.Fail(
                "Waqtiga (saacad) sawirka lama helin.",
                snippet);

        var atUtc = ToUtc(detectedAt.Value);

        if (atUtc.Year < 2020 || atUtc.Year > serverUtcNow.Year + 1)
            return PaymentScreenshotVerificationResult.Fail("Taariikhda sawirka ma saxna.", snippet);

        if (atUtc > serverUtcNow.AddMinutes(settings.MaxFutureMinutes))
            return PaymentScreenshotVerificationResult.Fail(
                "Waqtiga sawirka wuxuu u muuqdaa mustaqbalka.",
                snippet);

        var maxAge = TimeSpan.FromHours(settings.MaxScreenshotAgeHours);
        if (serverUtcNow - atUtc > maxAge)
            return PaymentScreenshotVerificationResult.Fail(
                $"Screenshot-ku waa duug (ka badan {settings.MaxScreenshotAgeHours} saac).",
                snippet);

        logger.LogInformation(
            "Payment screenshot OK: expected ${Expected}, detected ${Detected}",
            expectedAmountUsd, detectedAmount);

        return PaymentScreenshotVerificationResult.Ok(detectedAmount, atUtc, snippet);
    }

    private static DateTime ToUtc(DateTime dt) =>
        dt.Kind switch
        {
            DateTimeKind.Utc => dt,
            DateTimeKind.Local => dt.ToUniversalTime(),
            _ => DateTime.SpecifyKind(dt, DateTimeKind.Utc)
        };

    private static string? ValidateImageDimensions(byte[] bytes, PaymentVerificationSettings settings)
    {
        try
        {
            using var image = Image.Load(bytes);
            if (image.Width < settings.MinImageWidth || image.Height < settings.MinImageHeight)
                return $"Sawirka aad u yar yahay (ugu yaraan {settings.MinImageWidth}×{settings.MinImageHeight}).";
        }
        catch
        {
            return "Faylkan ma aha sawir sax ah (JPG/PNG/WEBP).";
        }

        return null;
    }

    private string RunOcrWithFallback(byte[] imageBytes)
    {
        var ocrInput = PreprocessForOcr(imageBytes);
        Exception? last = null;

        // Railway Docker: system tesseract CLI is more reliable than .NET native interop.
        if (OperatingSystem.IsLinux() && IsTesseractCliAvailable())
        {
            try
            {
                var text = RunOcrCli(ocrInput);
                logger.LogDebug("OCR via tesseract CLI, length={Len}", text.Length);
                return text;
            }
            catch (Exception ex)
            {
                last = ex;
                logger.LogWarning(ex, "tesseract CLI OCR failed, trying .NET Tesseract");
            }
        }

        try
        {
            var text = RunOcrTesseractNet(ocrInput);
            logger.LogDebug("OCR via .NET Tesseract, length={Len}", text.Length);
            return text;
        }
        catch (Exception ex)
        {
            last = ex;
            logger.LogWarning(ex, ".NET Tesseract OCR failed");
        }

        if (!OperatingSystem.IsLinux() && IsTesseractCliAvailable())
        {
            try
            {
                return RunOcrCli(ocrInput);
            }
            catch (Exception ex)
            {
                last = ex;
            }
        }

        throw new InvalidOperationException("OCR failed (CLI and .NET).", last);
    }

    private static byte[] PreprocessForOcr(byte[] imageBytes)
    {
        try
        {
            using var image = Image.Load(imageBytes);
            image.Mutate(x => x
                .Resize(new ResizeOptions
                {
                    Size = new Size(Math.Max(image.Width * 2, 800), 0),
                    Mode = ResizeMode.Max
                })
                .Grayscale());
            using var ms = new MemoryStream();
            image.SaveAsPng(ms);
            return ms.ToArray();
        }
        catch
        {
            return imageBytes;
        }
    }

    private static string RunOcrTesseractNet(byte[] ocrInput)
    {
        var tessPath = ResolveTessDataPath();
        using var engine = new TesseractEngine(tessPath, "eng", EngineMode.Default);
        engine.DefaultPageSegMode = PageSegMode.Auto;
        using var pix = Pix.LoadFromMemory(ocrInput);
        using var page = engine.Process(pix);
        return page.GetText() ?? string.Empty;
    }

    private static bool IsTesseractCliAvailable()
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "tesseract",
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            if (proc is null)
                return false;
            proc.WaitForExit(5000);
            return proc.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static string RunOcrCli(byte[] pngBytes)
    {
        var dir = Path.Combine(Path.GetTempPath(), "ae-ocr-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var input = Path.Combine(dir, "in.png");
        var outputBase = Path.Combine(dir, "out");
        try
        {
            File.WriteAllBytes(input, pngBytes);
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "tesseract",
                Arguments = $"\"{input}\" \"{outputBase}\" -l eng --psm 6",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }) ?? throw new InvalidOperationException("Could not start tesseract process.");

            if (!proc.WaitForExit(60_000))
            {
                try { proc.Kill(entireProcessTree: true); } catch { /* ignore */ }
                throw new TimeoutException("tesseract CLI timed out.");
            }

            var err = proc.StandardError.ReadToEnd();
            if (proc.ExitCode != 0)
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(err) ? $"tesseract exit {proc.ExitCode}" : err);

            var txtFile = outputBase + ".txt";
            if (!File.Exists(txtFile))
                throw new FileNotFoundException("tesseract did not produce output text.");

            return File.ReadAllText(txtFile);
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { /* ignore */ }
        }
    }

    private static string ResolveTessDataPath()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "tessdata"),
            Path.Combine(Directory.GetCurrentDirectory(), "tessdata"),
            "/app/tessdata",
            "/usr/share/tesseract-ocr/5/tessdata",
            "/usr/share/tesseract-ocr/4.00/tessdata"
        };

        foreach (var path in candidates)
        {
            if (string.IsNullOrWhiteSpace(path))
                continue;
            if (File.Exists(Path.Combine(path, "eng.traineddata")))
                return path;
        }

        throw new FileNotFoundException("OCR tessdata (eng.traineddata) not found.");
    }

    private static DateTime? TryReadExifDateUtc(byte[] bytes)
    {
        try
        {
            using var image = Image.Load(bytes);
            if (image.Metadata.ExifProfile is not { } exif)
                return null;

            if (exif.TryGetValue(ExifTag.DateTimeOriginal, out IExifValue<string>? original)
                && original?.Value is { } s1
                && DateTime.TryParse(s1, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt1))
                return dt1.ToUniversalTime();

            if (exif.TryGetValue(ExifTag.DateTimeDigitized, out IExifValue<string>? digitized)
                && digitized?.Value is { } s2
                && DateTime.TryParse(s2, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt2))
                return dt2.ToUniversalTime();
        }
        catch
        {
            // ignore
        }

        return null;
    }

    private static string CompactText(string text) =>
        WhitespacePattern().Replace(text ?? string.Empty, " ").Trim();

    private static int CountPaymentKeywords(string compact)
    {
        var lower = compact.ToLowerInvariant();
        return PaymentKeywords.Count(k => lower.Contains(k, StringComparison.Ordinal));
    }

    private sealed record AmountMatchResult(bool Matched, decimal? DetectedAmount, decimal? ClosestWrongAmount);

    private static AmountMatchResult TryMatchExpectedAmount(string compact, decimal expected, decimal tolerance)
    {
        var target = Math.Round(expected, 2);
        var normalized = compact.Replace(',', '.');

        foreach (var v in BuildAmountSearchVariants(target))
        {
            if (normalized.Contains(v, StringComparison.OrdinalIgnoreCase))
                return new AmountMatchResult(true, target, null);
        }

        var amounts = ExtractAmounts(normalized);
        if (amounts.Count == 0)
            return new AmountMatchResult(false, null, null);

        var withinTolerance = amounts
            .Where(a => Math.Abs(a - target) <= tolerance)
            .OrderBy(a => Math.Abs(a - target))
            .ToList();

        if (withinTolerance.Count > 0)
            return new AmountMatchResult(true, withinTolerance[0], null);

        var closest = amounts.OrderBy(a => Math.Abs(a - target)).First();
        return new AmountMatchResult(false, null, closest);
    }

    /// <summary>Mobile EVC/Zaad: $3 iyo $3.00 isku mid — labadaba la raadiyo.</summary>
    private static IEnumerable<string> BuildAmountSearchVariants(decimal target)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            target.ToString("F2", CultureInfo.InvariantCulture),
            "$" + target.ToString("F2", CultureInfo.InvariantCulture),
            "$ " + target.ToString("F2", CultureInfo.InvariantCulture),
            target.ToString("0.##", CultureInfo.InvariantCulture),
            "$" + target.ToString("0.##", CultureInfo.InvariantCulture)
        };

        if (target != Math.Floor(target))
            return set;

        var whole = ((int)target).ToString(CultureInfo.InvariantCulture);
        set.Add(whole);
        set.Add("$" + whole);
        set.Add("$ " + whole);
        set.Add("USD " + whole);
        set.Add("USD" + whole);
        set.Add("[-EVCPLUS-] $" + whole);
        set.Add("EVCPLUS- $" + whole);
        return set;
    }

    private static List<decimal> ExtractAmounts(string normalized)
    {
        var list = new List<decimal>();
        foreach (Match m in LooseAmountPattern().Matches(normalized))
        {
            var raw = (m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value).Replace(',', '.');
            if (string.IsNullOrWhiteSpace(raw))
                continue;
            if (!decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
                continue;
            if (amount <= 0 || amount >= 1_000_000)
                continue;
            // Telefoon (613508774) ha loo qaadin lacag
            if (amount >= 1000 && amount == Math.Floor(amount))
                continue;
            list.Add(Math.Round(amount, 2));
        }

        return list.Distinct().ToList();
    }

    private static string FormatMoney(decimal amount) => $"${amount:F2}";

    private static DateTime? FindDateTime(string compact, DateTime? exifUtc)
    {
        var candidates = new List<DateTime>();

        foreach (Match m in DateTimePattern().Matches(compact))
        {
            var raw = m.Value.Trim();
            if (DateTime.TryParseExact(raw, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var dt)
                || DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out dt))
            {
                if (dt.Year >= 2020)
                    candidates.Add(dt);
            }
        }

        if (candidates.Count > 0)
            return candidates.OrderByDescending(d => d).First();

        return exifUtc;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();

    [GeneratedRegex(@"\b\d{1,2}:\d{2}(?::\d{2})?\s*(?:AM|PM|am|pm)?\b")]
    private static partial Regex TimeInTextPattern();

  // EVC Plus: "[-EVCPLUS-] $3", "USD 12.50", "12,50"
    [GeneratedRegex(@"(?:\$|USD\s*)(\d{1,6}(?:[.,]\d{1,2})?)|(\d{1,6}(?:[.,]\d{1,2})?)\s*(?:USD|\$)", RegexOptions.IgnoreCase)]
    private static partial Regex LooseAmountPattern();

    [GeneratedRegex(
        @"\b(\d{1,2}[\/\-\.]\d{1,2}[\/\-\.]\d{2,4}(?:\s+\d{1,2}:\d{2}(?::\d{2})?\s*(?:AM|PM)?)?|\d{4}[\/\-\.]\d{1,2}[\/\-\.]\d{1,2}(?:\s+\d{1,2}:\d{2})?)\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex DateTimePattern();
}
