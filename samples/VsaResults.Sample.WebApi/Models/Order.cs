namespace VsaResults.Sample.WebApi.Models;

public record Order(
    Guid Id,
    Guid UserId,
    List<OrderItem> Items,
    OrderStatus Status,
    decimal Total,
    DateTime CreatedAt);

public record OrderItem(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);

public enum OrderStatus
{
    Pending,
    Processing,
    Shipped,
    Delivered,
    Cancelled,
}

public record CreateOrderRequest(Guid UserId, List<CreateOrderItemRequest> Items);

public record CreateOrderItemRequest(Guid ProductId, int Quantity);
