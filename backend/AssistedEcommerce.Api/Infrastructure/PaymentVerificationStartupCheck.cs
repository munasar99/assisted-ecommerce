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
            var path = Path.Combine(AppContext.BaseDirectory, "tessdata", "eng.traineddata");
            if (!File.Exists(path))
                path = Path.Combine(Directory.GetCurrentDirectory(), "tessdata", "eng.traineddata");

            if (!File.Exists(path))
            {
                logger.LogCritical(
                    "MISSING tessdata/eng.traineddata — payment screenshots CANNOT be verified. Run dotnet restore && dotnet build.");
            }
            else
            {
                logger.LogInformation("Payment screenshot OCR ready: {Path}", path);
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
