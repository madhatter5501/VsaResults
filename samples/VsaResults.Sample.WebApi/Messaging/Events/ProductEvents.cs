using VsaResults.Messaging;

namespace VsaResults.Sample.WebApi.Messaging.Events;

/// <summary>
/// Published after stock is successfully reserved for a product.
/// Subscribers can update dashboards, send to analytics, etc.
/// </summary>
public record StockReserved(
    Guid ProductId,
    string ProductName,
    int QuantityReserved,
    int RemainingStock) : IEvent;

/// <summary>
/// Published when product stock falls below a threshold.
/// Subscribers can trigger alerts, auto-reorder, notify warehouse, etc.
/// </summary>
public record StockLow(
    Guid ProductId,
    string ProductName,
    int CurrentStock) : IEvent;
