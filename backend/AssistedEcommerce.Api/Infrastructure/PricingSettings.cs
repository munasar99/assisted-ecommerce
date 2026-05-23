namespace AssistedEcommerce.Api.Infrastructure;

public class PricingSettings
{
    public const string SectionName = "Pricing";

    /// <summary>Flat service fee per order (e.g. 1 = $1 once).</summary>
    public decimal ServiceFeePerItemUsd { get; set; } = 1m;

    /// <summary>KG fee per kilogram (e.g. 1 = $1 per KG) — gooni alaabta, isma dhufo.</summary>
    public decimal KgFeePerKgUsd { get; set; } = 1m;
}
