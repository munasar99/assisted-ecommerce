using AssistedEcommerce.Api.Infrastructure;
using Microsoft.Extensions.Options;

namespace AssistedEcommerce.Api.Services;

/// <summary>Logs Resend config on startup so email issues are obvious in the console.</summary>
public class ResendStartupCheck(
    IOptions<ResendSettings> options,
    IWebHostEnvironment env,
    ILogger<ResendStartupCheck> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var s = options.Value;

        if (!s.IsConfigured)
        {
            logger.LogWarning(
                "EMAIL OFF: Resend ma configured. Geli appsettings.Local.json → Resend:Enabled=true + ApiKey, kadib API dib u bilow.");
            return Task.CompletedTask;
        }

        logger.LogInformation("EMAIL ON: Resend from={From}, paymentEmail={Payment}", s.FromEmail, s.SendEmailOnPaymentSubmitted);

        if (s.IsResendTestSender)
        {
            logger.LogWarning(
                "EMAIL: onboarding@resend.dev — macmiil email MA TAGO. " +
                "Si user walbo u helo email-kiisa: verify domain resend.com/domains → beddel FromEmail tusaale E-commerce@gmi.so");
            if (s.UseDevelopmentRedirect && !string.IsNullOrWhiteSpace(s.DevelopmentRedirectTo))
                logger.LogInformation("EMAIL tijaabo redirect ON → {Redirect} (macmiil email ma tago)", s.DevelopmentRedirectTo.Trim());
        }
        else if (s.CanSendToCustomerEmail)
        {
            logger.LogInformation("EMAIL: domain verified mode — email waxaa loo diri karaa macmiil kasta (form email).");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
