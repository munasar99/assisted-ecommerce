using Microsoft.Extensions.Options;

namespace AssistedEcommerce.Api.Infrastructure;

/// <summary>Hubi in OCR diyaar yahay marka API bilaabmo.</summary>
public class PaymentVerificationStartupCheck(
    IOptions<PaymentVerificationSettings> options,
    ILogger<PaymentVerificationStartupCheck> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled)
        {
            logger.LogWarning("PaymentVerification.Enabled=false — screenshot uploads will be REJECTED until enabled.");
            return Task.CompletedTask;
        }

        try
        {
            var tessFile = Path.Combine(AppContext.BaseDirectory, "tessdata", "eng.traineddata");
            if (!File.Exists(tessFile))
                tessFile = "/app/tessdata/eng.traineddata";

            var hasTessData = File.Exists(tessFile);
            var hasCli = OperatingSystem.IsLinux() && File.Exists("/usr/bin/tesseract");

            if (hasCli)
                logger.LogInformation("Payment OCR: tesseract CLI available (Railway/Linux).");
            if (hasTessData)
                logger.LogInformation("Payment OCR: tessdata at {Path}", tessFile);
            if (!hasCli && !hasTessData)
            {
                logger.LogCritical(
                    "Payment OCR NOT READY — install tesseract-ocr or tessdata/eng.traineddata.");
            }
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Payment verification startup check failed.");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
