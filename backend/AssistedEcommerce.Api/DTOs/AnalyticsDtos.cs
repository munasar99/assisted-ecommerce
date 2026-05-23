namespace AssistedEcommerce.Api.DTOs;

public record DashboardAnalyticsDto(
    long TotalOrders,
    long PendingOrders,
    long PaymentReviewOrders,
    long DeliveredOrders,
    decimal Revenue,
    long TotalUsers,
    long ActiveUsers,
    long BlockedUsers,
    IReadOnlyList<StatusCountDto> OrdersByStatus,
    IReadOnlyList<DistrictCountDto> OrdersByDistrict);

public record StatusCountDto(string Status, long Count);

public record DistrictCountDto(string DistrictId, long Count);
