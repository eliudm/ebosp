namespace EBOSP.Contracts.Sales;

public sealed record DeliveryLineResponse(Guid ProductId, int Quantity);

public sealed record DeliveryResponse(
    Guid Id,
    Guid SalesOrderId,
    Guid WarehouseId,
    Guid DeliveredByUserId,
    DateTimeOffset DeliveredAt,
    IReadOnlyList<DeliveryLineResponse> Lines);
