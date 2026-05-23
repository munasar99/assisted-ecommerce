namespace AssistedEcommerce.Api.Services;

/// <summary>
/// Wadarta = alaabta + GK1 ($1 gooni) + service + gaarsiin. Tirada order-ka kuma dhufto GK1.
/// </summary>
public static class OrderPricingHelper
{
    public static (decimal AlaabtaUsd, decimal KgFeeUsd, decimal ServiceFeeUsd, decimal TotalAmountUsd) ComputeTotals(
        decimal alaabtaPriceUsd,
        int quantityKg,
        decimal deliveryFeeUsd,
        decimal kgFeePerKgUsd,
        decimal flatServiceFeeUsd)
    {
        _ = Math.Max(1, quantityKg);
        var alaabta = Math.Round(alaabtaPriceUsd, 2, MidpointRounding.AwayFromZero);
        var kgFee = Math.Round(kgFeePerKgUsd, 2, MidpointRounding.AwayFromZero);
        var serviceFee = Math.Round(flatServiceFeeUsd, 2, MidpointRounding.AwayFromZero);
        var total = alaabta + kgFee + serviceFee + deliveryFeeUsd;
        return (alaabta, kgFee, serviceFee, total);
    }
}
