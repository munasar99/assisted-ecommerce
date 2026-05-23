namespace AssistedEcommerce.Api.Constants;

public static class PaymentStatuses
{
    public const string Pending = "Pending";
    public const string UnderReview = "UnderReview";
    public const string Confirmed = "Confirmed";
    public const string Rejected = "Rejected";

    public static readonly IReadOnlyList<string> All = [Pending, UnderReview, Confirmed, Rejected];
}
