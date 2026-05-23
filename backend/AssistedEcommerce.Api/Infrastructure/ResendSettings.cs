namespace AssistedEcommerce.Api.Infrastructure;

public class ResendSettings
{
    public const string SectionName = "Resend";

    public bool Enabled { get; set; }

    /// <summary>Email marka order la abuuro — default true.</summary>
    public bool SendEmailOnOrderCreated { get; set; } = true;

    /// <summary>Email marka macmiilku dhammeeyo lacag bixinta — default true.</summary>
    public bool SendEmailOnPaymentSubmitted { get; set; } = true;

    /// <summary>re_... from https://resend.com/api-keys</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Verified sender in Resend (e.g. onboarding@resend.dev for tests).</summary>
    public string FromEmail { get; set; } = "onboarding@resend.dev";

    public string FromName { get; set; } = "Assisted E-commerce";

    public string BaseUrl { get; set; } = "https://api.resend.com/";

    public string BrandName { get; set; } = "Assisted E-commerce";

    public string SupportPhone { get; set; } = "+252 61 3508774";

    public string SupportEmail { get; set; } = "E-commerce@gmi.so";

    public ResendEmailTemplates Templates { get; set; } = new();

    /// <summary>
    /// Tijaabo kaliya: haddii true, marka macmiil email diido, u dir DevelopmentRedirectTo.
    /// Macmiil email toos ah → verify domain + FromEmail @domain-kaaga (UseDevelopmentRedirect=false).
    /// </summary>
    public bool UseDevelopmentRedirect { get; set; }

    /// <summary>Email-ka Resend login (tijaabo redirect kaliya).</summary>
    public string? DevelopmentRedirectTo { get; set; }

    public bool IsConfigured => Enabled && !string.IsNullOrWhiteSpace(ApiKey);

    public bool IsResendTestSender =>
        FromEmail.Contains("resend.dev", StringComparison.OrdinalIgnoreCase);

    /// <summary>Domain verify kadib — email macmiil kasta waa la diri karaa.</summary>
    public bool CanSendToCustomerEmail => IsConfigured && !IsResendTestSender;
}
