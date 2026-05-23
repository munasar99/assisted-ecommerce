namespace AssistedEcommerce.Api.Infrastructure;

public class OrderFormSecuritySettings
{
    public const string SectionName = "OrderForm";

    public int MaxQuantity { get; set; } = 99;

    public decimal MaxProductUnitPriceUsd { get; set; } = 50_000m;

    public decimal MinProductUnitPriceUsd { get; set; } = 0.01m;
}
