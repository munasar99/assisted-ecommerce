namespace AssistedEcommerce.Api.Constants;

public static class OrderStatuses
{
    public const string Pending = "Pending";
    public const string InvoiceSent = "InvoiceSent";
    public const string WaitingPayment = "WaitingPayment";
    public const string PaymentReview = "PaymentReview";
    public const string Confirmed = "Confirmed";
    public const string OrderedFromSupplier = "OrderedFromSupplier";
    public const string Shipping = "Shipping";
    public const string ArrivedMogadishu = "ArrivedMogadishu";
    public const string OutForDelivery = "OutForDelivery";
    public const string Delivered = "Delivered";

    public static readonly IReadOnlyList<string> All =
    [
        Pending, InvoiceSent, WaitingPayment, PaymentReview, Confirmed,
        OrderedFromSupplier, Shipping, ArrivedMogadishu, OutForDelivery, Delivered
    ];

    private static readonly Dictionary<string, string[]> AllowedTransitions = new(StringComparer.OrdinalIgnoreCase)
    {
        [Pending] = [InvoiceSent],
        [InvoiceSent] = [WaitingPayment],
        [WaitingPayment] = [PaymentReview],
        [PaymentReview] = [Confirmed, WaitingPayment],
        [Confirmed] = [OrderedFromSupplier],
        [OrderedFromSupplier] = [Shipping],
        [Shipping] = [ArrivedMogadishu],
        [ArrivedMogadishu] = [OutForDelivery],
        [OutForDelivery] = [Delivered],
        [Delivered] = []
    };

    public static bool CanTransition(string from, string to)
    {
        if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
            return true;
        return AllowedTransitions.TryGetValue(from, out var next) &&
               next.Contains(to, StringComparer.OrdinalIgnoreCase);
    }
}
