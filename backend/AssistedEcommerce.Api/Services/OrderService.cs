using AssistedEcommerce.Api.Constants;
using AssistedEcommerce.Api.DTOs;
using AssistedEcommerce.Api.Exceptions;
using AssistedEcommerce.Api.Infrastructure;
using AssistedEcommerce.Api.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace AssistedEcommerce.Api.Services;

public interface IOrderService
{
    Task<CreateOrderResponse> CreateOrderAsync(CreateOrderRequest request, CancellationToken ct = default);
    Task<OrderDetailDto> TrackOrderAsync(string orderId, string phone, CancellationToken ct = default);
    Task<PagedResult<OrderListItemDto>> GetOrdersAsync(int page, int pageSize, string? status, string? districtId, string? search, CancellationToken ct = default);
    Task<OrderDetailDto> GetOrderDetailAsync(string orderId, CancellationToken ct = default);
    Task<OrderDetailDto> UpdateStatusAsync(string orderId, UpdateOrderStatusRequest request, string adminId, CancellationToken ct = default);
    Task<OrderDetailDto> UpdateOrderAsync(string orderId, UpdateOrderRequest request, string adminId, CancellationToken ct = default);
    Task DeleteOrderAsync(string orderId, string adminId, CancellationToken ct = default);
    Task<InvoiceDto> CreateInvoiceAsync(string orderId, CreateInvoiceRequest request, string adminId, CancellationToken ct = default);
}

public class OrderService(
    MongoDbContext db,
    IIdGeneratorService idGenerator,
    IUserService userService,
    IDeliveryService deliveryService,
    INotificationService notificationService,
    IAuditService auditService,
    IOptions<PricingSettings> pricingOptions,
    IOptions<OrderFormSecuritySettings> formSecurityOptions,
    IOptions<ResendSettings> resendOptions,
    ILogger<OrderService> logger) : IOrderService
{
    public async Task<CreateOrderResponse> CreateOrderAsync(CreateOrderRequest request, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var formSecurity = formSecurityOptions.Value;
        OrderFormSecurityValidator.ValidateCreateRequest(request, formSecurity);

        if (!DeliveryTypes.IsValid(request.DeliveryType))
            throw new ApiException("Invalid delivery type. Dooro HomeDelivery ama Pickup.");

        var deliveryType = DeliveryTypes.Normalize(request.DeliveryType);
        var isHomeDelivery = DeliveryTypes.IsHomeDelivery(deliveryType);

        string districtId;
        string districtName;
        string addressDetail;
        decimal deliveryFee;

        if (isHomeDelivery)
        {
            if (string.IsNullOrWhiteSpace(request.DistrictId))
                throw new ApiException("Dooro degmada (districtId) — Home delivery kaliya.");
            if (string.IsNullOrWhiteSpace(request.AddressDetail))
                throw new ApiException("Address is required for home delivery.");

            var zone = await deliveryService.GetActiveZoneByZoneIdAsync(request.DistrictId.Trim(), ct);
            districtId = zone.ZoneId;
            districtName = zone.DistrictName;
            addressDetail = request.AddressDetail.Trim();
            deliveryFee = zone.FeeUsd;
        }
        else
        {
            districtId = DeliveryTypes.PickupDistrictId;
            districtName = DeliveryTypes.PickupDistrictName;
            addressDetail = request.AddressDetail?.Trim() ?? DeliveryTypes.PickupDistrictName;
            deliveryFee = 0;
        }

        var pricing = pricingOptions.Value;
        var (alaabta, kgFee, serviceFee, totalAmount) = OrderPricingHelper.ComputeTotals(
            request.ProductUnitPriceUsd,
            request.Quantity,
            deliveryFee,
            pricing.KgFeePerKgUsd,
            pricing.ServiceFeePerItemUsd);

        var user = await userService.FindOrCreateByPhoneAsync(request.FullName, request.Phone, ct);

        var orderId = await idGenerator.NextOrderIdAsync(user.UserId, ct);
        var invoiceNumber = await idGenerator.NextInvoiceNumberAsync(ct);

        var invoice = new Invoice
        {
            InvoiceNumber = invoiceNumber,
            OrderId = orderId,
            UserId = user.UserId,
            ProductCost = alaabta,
            ServiceFee = serviceFee,
            DeliveryFee = deliveryFee,
            OtherCharges = kgFee,
            TotalAmount = totalAmount,
            IssuedAt = now,
            IssuedBy = "system"
        };
        await db.Invoices.InsertOneAsync(invoice, cancellationToken: ct);

        var order = new Order
        {
            OrderId = orderId,
            UserId = user.UserId,
            CustomerFullName = request.FullName.Trim(),
            CustomerPhone = user.Phone,
            CustomerEmail = OrderFormSecurityValidator.NormalizeEmail(request.Email),
            ProductUrl = request.ProductUrl.Trim(),
            ProductName = request.ProductName?.Trim(),
            Quantity = request.Quantity,
            Notes = request.Notes?.Trim(),
            DeliveryType = deliveryType,
            DistrictId = districtId,
            DistrictName = districtName,
            AddressDetail = addressDetail,
            DeliveryFee = deliveryFee,
            ProductUnitPriceUsd = request.ProductUnitPriceUsd,
            ProductSubtotalUsd = alaabta,
            ServiceFeeUsd = serviceFee,
            TotalAmountUsd = totalAmount,
            InvoiceId = invoiceNumber,
            Status = OrderStatuses.WaitingPayment,
            OrderScreenshotUrl = request.OrderScreenshotUrl,
            SubmittedAtUtc = now,
            UserRegisteredAt = user.CreatedAt,
            StatusHistory =
            [
                new StatusHistoryEntry { Status = OrderStatuses.Pending, At = now, By = "system" },
                new StatusHistoryEntry { Status = OrderStatuses.InvoiceSent, At = now, By = "system", Note = "Auto invoice" },
                new StatusHistoryEntry { Status = OrderStatuses.WaitingPayment, At = now, By = "system" }
            ],
            CreatedAt = now,
            UpdatedAt = now
        };

        await db.Orders.InsertOneAsync(order, cancellationToken: ct);
        await db.Users.UpdateOneAsync(
            u => u.Id == user.Id,
            Builders<User>.Update.Inc(u => u.TotalOrders, 1).Set(u => u.UpdatedAt, DateTime.UtcNow),
            cancellationToken: ct);

        logger.LogInformation(
            "SAVED to MongoDB database '{Database}' → Orders.{OrderId}, User.{UserId} (persistent)",
            db.DatabaseName, order.OrderId, user.UserId);

        var emailResult = await notificationService.NotifyOrderCreatedAsync(
            order.OrderId,
            order.CustomerEmail,
            order.CustomerFullName,
            order.TotalAmountUsd,
            ct);

        var emailSent = emailResult?.Success == true;
        var emailError = emailResult?.ErrorMessage;
        if (!emailSent && string.IsNullOrWhiteSpace(emailError) && string.IsNullOrWhiteSpace(order.CustomerEmail))
            emailError = ResendErrorTranslator.ToSomali("Geli email sax ah form-ka.");
        else if (!emailSent && !string.IsNullOrWhiteSpace(emailError))
            emailError = ResendErrorTranslator.ToSomali(emailError);

        logger.LogInformation(
            "Order saved: {OrderId} deliveryType={DeliveryType} districtId={DistrictId} districtName={DistrictName} emailSent={EmailSent}",
            order.OrderId, order.DeliveryType, order.DistrictId, order.DistrictName, emailSent);

        return new CreateOrderResponse(
            order.OrderId,
            order.UserId,
            order.DeliveryType,
            order.DistrictId,
            order.DistrictName,
            order.DeliveryFee,
            order.ProductSubtotalUsd,
            order.ServiceFeeUsd,
            order.TotalAmountUsd,
            order.InvoiceId,
            order.Status,
            emailSent,
            emailError);
    }

    public async Task<OrderDetailDto> TrackOrderAsync(string orderId, string phone, CancellationToken ct = default)
    {
        var user = await FindUserByPhoneForTrackAsync(phone, ct);
        var order = await db.Orders.Find(o => o.OrderId == orderId.Trim() && o.UserId == user.UserId).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Order not found.");

        if (string.IsNullOrWhiteSpace(order.CustomerFullName))
            order.CustomerFullName = user.FullName;
        if (string.IsNullOrWhiteSpace(order.CustomerPhone))
            order.CustomerPhone = user.Phone;

        return MapDetail(order);
    }

    private async Task<User> FindUserByPhoneForTrackAsync(string phone, CancellationToken ct)
    {
        var normalized = UserService.NormalizePhone(phone);
        var user = await db.Users.Find(u => u.Phone == normalized).FirstOrDefaultAsync(ct);
        if (user is not null)
            return user;

        if (normalized.Length >= 9)
        {
            var suffix = normalized[^9..];
            var candidates = await db.Users.Find(u => u.Phone.EndsWith(suffix)).ToListAsync(ct);
            user = candidates.FirstOrDefault(u =>
                PhoneNormalizer.CanonicalKey(u.Phone) == PhoneNormalizer.CanonicalKey(normalized));
            if (user is not null)
                return user;
        }

        throw new NotFoundException("Order not found.");
    }

    public async Task<PagedResult<OrderListItemDto>> GetOrdersAsync(int page, int pageSize, string? status, string? districtId, string? search, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 500);

        var filter = Builders<Order>.Filter.Empty;
        if (!string.IsNullOrWhiteSpace(status))
            filter &= Builders<Order>.Filter.Eq(o => o.Status, status);
        if (!string.IsNullOrWhiteSpace(districtId))
            filter &= Builders<Order>.Filter.Eq(o => o.DistrictId, districtId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            filter &= Builders<Order>.Filter.Or(
                Builders<Order>.Filter.Regex(o => o.OrderId, new MongoDB.Bson.BsonRegularExpression(s, "i")),
                Builders<Order>.Filter.Regex(o => o.UserId, new MongoDB.Bson.BsonRegularExpression(s, "i")));
        }

        var total = await db.Orders.CountDocumentsAsync(filter, cancellationToken: ct);
        var items = await db.Orders.Find(filter)
            .SortBy(o => o.UserRegisteredAt)
            .ThenBy(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(ct);

        var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        var userIds = items.Select(o => o.UserId).Distinct().ToList();
        var users = userIds.Count == 0
            ? []
            : await db.Users.Find(u => userIds.Contains(u.UserId)).ToListAsync(ct);
        var userMap = users.ToDictionary(u => u.UserId);

        var dtos = items.Select(o =>
        {
            userMap.TryGetValue(o.UserId, out var user);
            var fullName = !string.IsNullOrWhiteSpace(o.CustomerFullName) ? o.CustomerFullName : user?.FullName;
            var phone = !string.IsNullOrWhiteSpace(o.CustomerPhone) ? o.CustomerPhone : user?.Phone;
            return new OrderListItemDto(
                o.OrderId,
                o.UserId,
                fullName,
                phone,
                o.CustomerEmail,
                o.Status,
                o.DistrictId,
                o.DeliveryFee,
                o.TotalAmountUsd,
                o.ProductName,
                o.PaymentScreenshotUrl,
                o.InvoiceId,
                o.CreatedAt);
        }).ToList();

        return new PagedResult<OrderListItemDto>(dtos, page, pageSize, total, totalPages);
    }

    public async Task<OrderDetailDto> GetOrderDetailAsync(string orderId, CancellationToken ct = default)
    {
        var order = await db.Orders.Find(o => o.OrderId == orderId).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Order not found.");
        return MapDetail(order);
    }

    public async Task<OrderDetailDto> UpdateStatusAsync(string orderId, UpdateOrderStatusRequest request, string adminId, CancellationToken ct = default)
    {
        var order = await db.Orders.Find(o => o.OrderId == orderId).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Order not found.");

        if (!OrderStatuses.All.Contains(request.Status))
            throw new ApiException("Invalid status.");

        if (string.Equals(order.Status, request.Status, StringComparison.OrdinalIgnoreCase))
            return MapDetail(order);

        if (!OrderStatuses.CanTransition(order.Status, request.Status))
            throw new ApiException($"Cannot transition from {order.Status} to {request.Status}.");

        var old = order.Status;
        order.Status = request.Status;
        order.UpdatedAt = DateTime.UtcNow;
        order.StatusHistory.Add(new StatusHistoryEntry
        {
            Status = request.Status,
            At = DateTime.UtcNow,
            By = adminId,
            Note = request.Note
        });

        await db.Orders.ReplaceOneAsync(o => o.Id == order.Id, order, cancellationToken: ct);
        await auditService.LogAsync(adminId, "ORDER_STATUS_UPDATE", "Orders", orderId, new { old, request.Status });

        var user = await db.Users.Find(u => u.UserId == order.UserId).FirstOrDefaultAsync(ct);
        if (user is not null)
            await notificationService.NotifyOrderStatusAsync(
            user.Phone, order.OrderId, order.Status, order.CustomerEmail, order.CustomerFullName, ct);

        return MapDetail(order);
    }

    public async Task<OrderDetailDto> UpdateOrderAsync(string orderId, UpdateOrderRequest request, string adminId, CancellationToken ct = default)
    {
        var order = await db.Orders.Find(o => o.OrderId == orderId).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Order not found.");

        if (!string.IsNullOrWhiteSpace(request.FullName) || !string.IsNullOrWhiteSpace(request.Phone))
        {
            var user = await db.Users.Find(u => u.UserId == order.UserId).FirstOrDefaultAsync(ct);
            if (user is not null)
            {
                if (!string.IsNullOrWhiteSpace(request.FullName))
                {
                    user.FullName = request.FullName.Trim();
                    order.CustomerFullName = user.FullName;
                }
                if (!string.IsNullOrWhiteSpace(request.Phone))
                {
                    user.Phone = request.Phone.Trim();
                    order.CustomerPhone = user.Phone;
                }
                user.UpdatedAt = DateTime.UtcNow;
                await db.Users.ReplaceOneAsync(u => u.Id == user.Id, user, cancellationToken: ct);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            if (!OrderFormSecurityValidator.IsValidEmail(request.Email))
                throw new ApiException("Geli email sax ah.");
            order.CustomerEmail = OrderFormSecurityValidator.NormalizeEmail(request.Email);
        }

        if (!string.IsNullOrWhiteSpace(request.OrderScreenshotUrl))
            order.OrderScreenshotUrl = request.OrderScreenshotUrl.Trim();

        if (!string.IsNullOrWhiteSpace(request.ProductUrl))
            order.ProductUrl = request.ProductUrl.Trim();
        if (request.ProductName is not null)
            order.ProductName = string.IsNullOrWhiteSpace(request.ProductName) ? null : request.ProductName.Trim();
        if (request.Notes is not null)
            order.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        if (!string.IsNullOrWhiteSpace(request.AddressDetail))
            order.AddressDetail = request.AddressDetail.Trim();

        if (request.Quantity is { } qty)
        {
            if (qty < 1) throw new ApiException("Quantity must be at least 1.");
            order.Quantity = qty;
        }

        if (request.ProductUnitPriceUsd is { } price)
        {
            if (price <= 0) throw new ApiException("Product price must be greater than zero.");
            order.ProductUnitPriceUsd = price;
        }

        var p = pricingOptions.Value;

        if (!string.IsNullOrWhiteSpace(request.DeliveryType))
        {
            if (!DeliveryTypes.IsValid(request.DeliveryType))
                throw new ApiException("deliveryType: HomeDelivery ama Pickup kaliya.");
            await ApplyDeliveryToOrderAsync(order, DeliveryTypes.Normalize(request.DeliveryType), request.DistrictId, ct);
        }
        else if (!string.IsNullOrWhiteSpace(request.DistrictId))
        {
            if (!DeliveryTypes.IsHomeDelivery(order.DeliveryType))
                throw new ApiException("districtId waxaa loo isticmaalaa Home delivery kaliya.");
            await ApplyDeliveryToOrderAsync(order, order.DeliveryType, request.DistrictId, ct);
        }

        var (alaabtaUpd, kgFeeUpd, serviceUpd, totalUpd) = OrderPricingHelper.ComputeTotals(
            order.ProductUnitPriceUsd,
            order.Quantity,
            order.DeliveryFee,
            p.KgFeePerKgUsd,
            p.ServiceFeePerItemUsd);
        order.ProductSubtotalUsd = alaabtaUpd;
        order.ServiceFeeUsd = serviceUpd;
        order.TotalAmountUsd = totalUpd;

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            !string.Equals(order.Status, request.Status, StringComparison.OrdinalIgnoreCase))
        {
            if (!OrderStatuses.All.Contains(request.Status))
                throw new ApiException("Invalid status.");
            if (!OrderStatuses.CanTransition(order.Status, request.Status))
                throw new ApiException($"Cannot transition from {order.Status} to {request.Status}.");

            var old = order.Status;
            order.Status = request.Status;
            order.StatusHistory.Add(new StatusHistoryEntry
            {
                Status = request.Status,
                At = DateTime.UtcNow,
                By = adminId,
                Note = request.StatusNote ?? "Updated via PUT"
            });
            await auditService.LogAsync(adminId, "ORDER_STATUS_UPDATE", "Orders", orderId, new { old, request.Status });
        }

        order.UpdatedAt = DateTime.UtcNow;
        await db.Orders.ReplaceOneAsync(o => o.Id == order.Id, order, cancellationToken: ct);

        if (!string.IsNullOrWhiteSpace(order.InvoiceId))
        {
            var invoice = await db.Invoices.Find(i => i.InvoiceNumber == order.InvoiceId).FirstOrDefaultAsync(ct);
            if (invoice is not null)
            {
                invoice.ProductCost = order.ProductSubtotalUsd;
                invoice.OtherCharges = kgFeeUpd;
                invoice.ServiceFee = order.ServiceFeeUsd;
                invoice.TotalAmount = order.TotalAmountUsd;
                await db.Invoices.ReplaceOneAsync(i => i.Id == invoice.Id, invoice, cancellationToken: ct);
            }
        }

        await auditService.LogAsync(adminId, "UPDATE_ORDER", "Orders", orderId);
        return MapDetail(order);
    }

    public async Task DeleteOrderAsync(string orderId, string adminId, CancellationToken ct = default)
    {
        var order = await db.Orders.Find(o => o.OrderId == orderId).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Order not found.");

        if (!string.IsNullOrWhiteSpace(order.InvoiceId))
            await db.Invoices.DeleteOneAsync(i => i.InvoiceNumber == order.InvoiceId, ct);

        await db.Orders.DeleteOneAsync(o => o.Id == order.Id, ct);

        await db.Users.UpdateOneAsync(
            u => u.UserId == order.UserId && u.TotalOrders > 0,
            Builders<User>.Update.Inc(u => u.TotalOrders, -1).Set(u => u.UpdatedAt, DateTime.UtcNow),
            cancellationToken: ct);

        await auditService.LogAsync(adminId, "DELETE_ORDER", "Orders", orderId);
    }

    public async Task<InvoiceDto> CreateInvoiceAsync(string orderId, CreateInvoiceRequest request, string adminId, CancellationToken ct = default)
    {
        var order = await db.Orders.Find(o => o.OrderId == orderId).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Order not found.");

        if (order.Status is not OrderStatuses.Pending and not OrderStatuses.WaitingPayment)
            throw new ApiException("Invoice can only be created for pending orders.");

        if (order.InvoiceId is not null)
            throw new ApiException("Invoice already exists for this order.");

        var total = request.ProductCost + request.ServiceFee + order.DeliveryFee + request.OtherCharges;
        var invoiceNumber = await idGenerator.NextInvoiceNumberAsync(ct);

        var invoice = new Invoice
        {
            InvoiceNumber = invoiceNumber,
            OrderId = order.OrderId,
            UserId = order.UserId,
            ProductCost = request.ProductCost,
            ServiceFee = request.ServiceFee,
            DeliveryFee = order.DeliveryFee,
            OtherCharges = request.OtherCharges,
            TotalAmount = total,
            IssuedAt = DateTime.UtcNow,
            IssuedBy = adminId
        };
        await db.Invoices.InsertOneAsync(invoice, cancellationToken: ct);

        order.InvoiceId = invoiceNumber;
        order.UpdatedAt = DateTime.UtcNow;

        if (OrderStatuses.CanTransition(order.Status, OrderStatuses.InvoiceSent))
        {
            order.Status = OrderStatuses.InvoiceSent;
            order.StatusHistory.Add(new StatusHistoryEntry
            {
                Status = OrderStatuses.InvoiceSent,
                At = DateTime.UtcNow,
                By = adminId,
                Note = "Invoice created"
            });
        }

        if (OrderStatuses.CanTransition(order.Status, OrderStatuses.WaitingPayment))
        {
            order.Status = OrderStatuses.WaitingPayment;
            order.StatusHistory.Add(new StatusHistoryEntry
            {
                Status = OrderStatuses.WaitingPayment,
                At = DateTime.UtcNow,
                By = adminId
            });
        }

        await db.Orders.ReplaceOneAsync(o => o.Id == order.Id, order, cancellationToken: ct);
        await auditService.LogAsync(adminId, "CREATE_INVOICE", "Invoices", invoiceNumber, new { orderId, total });

        var user = await db.Users.Find(u => u.UserId == order.UserId).FirstOrDefaultAsync(ct);
        if (user is not null)
            await notificationService.NotifyOrderStatusAsync(
            user.Phone, order.OrderId, order.Status, order.CustomerEmail, order.CustomerFullName, ct);

        return new InvoiceDto(
            invoice.InvoiceNumber, invoice.OrderId, invoice.ProductCost, invoice.ServiceFee,
            invoice.DeliveryFee, invoice.OtherCharges, invoice.TotalAmount, invoice.Currency, invoice.IssuedAt);
    }

    private async Task ApplyDeliveryToOrderAsync(Order order, string deliveryType, string? districtIdOrNull, CancellationToken ct)
    {
        order.DeliveryType = deliveryType;

        if (DeliveryTypes.IsPickup(deliveryType))
        {
            order.DistrictId = DeliveryTypes.PickupDistrictId;
            order.DistrictName = DeliveryTypes.PickupDistrictName;
            order.DeliveryFee = 0;
            if (string.IsNullOrWhiteSpace(order.AddressDetail))
                order.AddressDetail = DeliveryTypes.PickupDistrictName;
            return;
        }

        if (string.IsNullOrWhiteSpace(districtIdOrNull))
            throw new ApiException("Home delivery: dooro districtId (tusaale ZONE-HODAN).");

        var zone = await deliveryService.GetActiveZoneByZoneIdAsync(districtIdOrNull.Trim(), ct);
        order.DistrictId = zone.ZoneId;
        order.DistrictName = zone.DistrictName;
        order.DeliveryFee = zone.FeeUsd;
    }

    private OrderDetailDto MapDetail(Order o)
    {
        var p = pricingOptions.Value;
        var (alaabta, kgFee, serviceFee, total) = OrderPricingHelper.ComputeTotals(
            o.ProductUnitPriceUsd,
            o.Quantity,
            o.DeliveryFee,
            p.KgFeePerKgUsd,
            p.ServiceFeePerItemUsd);

        return new OrderDetailDto(
            o.OrderId, o.UserId, o.CustomerFullName, o.CustomerPhone, o.CustomerEmail, o.ProductUrl, o.ProductName, o.Quantity, o.Notes,
            o.DeliveryType, o.DistrictId, o.DistrictName, o.AddressDetail, o.DeliveryFee,
            o.ProductUnitPriceUsd, alaabta, kgFee, serviceFee, total,
            o.Status,
            o.OrderScreenshotUrl, o.PaymentScreenshotUrl, o.InvoiceId,
            o.StatusHistory.Select(h => new StatusHistoryDto(h.Status, h.At, h.By, h.Note)).ToList(),
            o.CreatedAt, o.UpdatedAt);
    }
}
