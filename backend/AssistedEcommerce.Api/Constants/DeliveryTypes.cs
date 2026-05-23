namespace AssistedEcommerce.Api.Constants;

public static class DeliveryTypes
{
    public const string HomeDelivery = "HomeDelivery";
    public const string Pickup = "Pickup";

    /// <summary>Pickup — degmo/zone looma baahna; value in DB.</summary>
    public const string PickupDistrictId = "TABO";
    public const string PickupDistrictName = "Waa Soodonanaa";

    public static bool IsValid(string? value) =>
        value is HomeDelivery or Pickup;

    public static bool IsHomeDelivery(string? value) =>
        string.Equals(value, HomeDelivery, StringComparison.OrdinalIgnoreCase);

    public static bool IsPickup(string? value) =>
        string.Equals(value, Pickup, StringComparison.OrdinalIgnoreCase);

    /// <summary>Canonical value for database — no silent change to another type.</summary>
    public static string Normalize(string? value)
    {
        if (IsHomeDelivery(value)) return HomeDelivery;
        if (IsPickup(value)) return Pickup;
        throw new InvalidOperationException("Invalid delivery type.");
    }
}
