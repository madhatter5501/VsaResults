using VsaResults.Sample.WebApi.Models;

namespace VsaResults.Sample.WebApi.Services;

public interface IOrderService
{
    ErrorOr<Order> GetById(Guid id);
    ErrorOr<List<Order>> GetByUserId(Guid userId);
    Task<ErrorOr<Order>> CreateAsync(CreateOrderRequest request);
    ErrorOr<Order> Cancel(Guid orderId);
    ErrorOr<Order> UpdateStatus(Guid orderId, OrderStatus newStatus);
}

/// <summary>
/// Demonstrates complex ErrorOr patterns with async operations,
/// multiple service dependencies, and transaction-like operations.
/// </summary>
public class OrderService : IOrderService
{
    private readonly IUserService _userService;
    private readonly IProductService _productService;

    private static readonly Dictionary<Guid, Order> _orders = [];

    public OrderService(IUserService userService, IProductService productService)
    {
        _userService = userService;
        _productService = productService;
    }

    public ErrorOr<Order> GetById(Guid id)
    {
        if (_orders.TryGetValue(id, out var order))
        {
            return order;
        }

        return DomainErrors.Order.NotFound(id);
    }

    public ErrorOr<List<Order>> GetByUserId(Guid userId)
    {
        // First validate user exists, then filter orders
        return _userService.GetById(userId)
            .Then(_ => _orders.Values.Where(o => o.UserId == userId).ToList());
    }

    /// <summary>
    /// Complex example: Validates multiple inputs, checks dependencies,
    /// and performs a multi-step operation.
    /// </summary>
    public async Task<ErrorOr<Order>> CreateAsync(CreateOrderRequest request)
    {
        // Step 1: Validate the user exists
        var userResult = _userService.GetById(request.UserId);
        if (userResult.IsError)
        {
            return userResult.Errors;
        }

        // Step 2: Validate items
        if (request.Items.Count == 0)
        {
            return DomainErrors.Order.EmptyItems;
        }

        // Step 3: Validate each item and collect errors
        var errors = new List<Error>();
        var orderItems = new List<OrderItem>();

        foreach (var item in request.Items)
        {
            if (item.Quantity <= 0)
            {
                errors.Add(DomainErrors.Order.InvalidQuantity(item.ProductId));
                continue;
            }

            // Check product exists and has stock
            var productResult = _productService.GetById(item.ProductId);
            if (productResult.IsError)
            {
                errors.AddRange(productResult.Errors);
                continue;
            }

            var product = productResult.Value;
            if (product.StockQuantity < item.Quantity)
            {
                errors.Add(DomainErrors.Order.InsufficientStock(
                    item.ProductId,
                    item.Quantity,
                    product.StockQuantity));
                continue;
            }

            orderItems.Add(new OrderItem(
                product.Id,
                product.Name,
                item.Quantity,
                product.Price));
        }

        // Return all errors at once
        if (errors.Count > 0)
        {
            return errors;
        }

        // Step 4: Reserve stock for each product
        foreach (var item in request.Items)
        {
            var reserveResult = _productService.ReserveStock(item.ProductId, item.Quantity);
            if (reserveResult.IsError)
            {
                // In a real app, you'd rollback previous reservations
                return reserveResult.Errors;
            }
        }

        // Simulate async operation (e.g., saving to database)
        await Task.Delay(10);

        // Step 5: Create the order
        var order = new Order(
            Guid.NewGuid(),
            request.UserId,
            orderItems,
            OrderStatus.Pending,
            orderItems.Sum(i => i.UnitPrice * i.Quantity),
            DateTime.UtcNow);

        _orders[order.Id] = order;

        return order;
    }

    /// <summary>
    /// Complex example: Business rule validation with state checks.
    /// </summary>
    public ErrorOr<Order> Cancel(Guid orderId)
    {
        return GetById(orderId)
            .Then<Order>(order => order.Status switch
            {
                OrderStatus.Cancelled => DomainErrors.Order.AlreadyCancelled(orderId),
                OrderStatus.Shipped or OrderStatus.Delivered => DomainErrors.Order.CannotCancelShipped(orderId),
                _ => UpdateOrderStatus(order, OrderStatus.Cancelled),
            });
    }

    /// <summary>
    /// Simple state transition with validation.
    /// </summary>
    public ErrorOr<Order> UpdateStatus(Guid orderId, OrderStatus newStatus)
    {
        return GetById(orderId)
            .Then(order => ValidateStatusTransition(order, newStatus))
            .Then(order => UpdateOrderStatus(order, newStatus));
    }

    private static ErrorOr<Order> ValidateStatusTransition(Order order, OrderStatus newStatus)
    {
        // Define valid transitions
        var validTransitions = new Dictionary<OrderStatus, OrderStatus[]>
        {
            [OrderStatus.Pending] = [OrderStatus.Processing, OrderStatus.Cancelled],
            [OrderStatus.Processing] = [OrderStatus.Shipped, OrderStatus.Cancelled],
            [OrderStatus.Shipped] = [OrderStatus.Delivered],
            [OrderStatus.Delivered] = [],
            [OrderStatus.Cancelled] = [],
        };

        if (!validTransitions[order.Status].Contains(newStatus))
        {
            return Error.Validation(
                "Order.InvalidTransition",
                $"Cannot transition order from '{order.Status}' to '{newStatus}'.");
        }

        return order;
    }

    private static Order UpdateOrderStatus(Order order, OrderStatus newStatus)
    {
        var updatedOrder = order with { Status = newStatus };
        _orders[order.Id] = updatedOrder;
        return updatedOrder;
    }
}
