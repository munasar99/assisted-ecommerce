namespace AssistedEcommerce.Api.Infrastructure;

public static class ResendErrorTranslator
{
    public static string ToSomali(string? apiError)
    {
        if (string.IsNullOrWhiteSpace(apiError))
            return "Email lama dirin — sabab aan la aqoon.";

        var e = apiError.ToLowerInvariant();

        if (e.Contains("api key") || e.Contains("unauthorized") || e.Contains("401"))
            return "API key-ga Resend ma saxna ama wuu dhacay. Samee key cusub resend.com/api-keys kadib geli appsettings.Local.json.";

        if (e.Contains("only send testing emails") || e.Contains("your own email"))
            return "Email macmiilka lama diri karo ilaa domain la verify gareeyo. Resend.com → Domains → verify gmi.so kadib beddel FromEmail: E-commerce@gmi.so (ama noreply@gmi.so). onboarding@resend.dev waxay u dirtaa kaliya email-kaaga Resend.";

        if (e.Contains("domain") && e.Contains("verify"))
            return "Domain-kaaga Resend ma verify. resend.com → Domains → verify kadib beddel FromEmail.";

        if (e.Contains("not configured") || e.Contains("enabled=false"))
            return "Resend ma furan. Hubi appsettings.Local.json: Enabled=true + ApiKey, kadib API dib u bilow (dotnet run).";

        if (e.Contains("customeremail") || e.Contains("email ma lahan"))
            return "Dalabkan email ma lahan. Samee dalab cusub oo email ku qor form-ka.";

        return apiError.Length > 180 ? apiError[..180] + "…" : apiError;
    }
}
