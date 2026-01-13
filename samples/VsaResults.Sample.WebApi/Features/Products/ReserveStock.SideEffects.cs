using VsaResults.Messaging;
using VsaResults.Sample.WebApi.Messaging.Events;

namespace VsaResults.Sample.WebApi.Features.Products;

public static partial class ReserveStock
{
    /// <summary>
    /// Side effects for ReserveStock: publishes StockReserved and conditionally StockLow events.
    /// Demonstrates publishing multiple events and conditional event publishing.
    /// </summary>
    public class SideEffects(IPublishEndpoint publishEndpoint) : IFeatureSideEffects<Request>
    {
        private const int LowStockThreshold = 10;

        public async Task<ErrorOr<Unit>> ExecuteAsync(
            FeatureContext<Request> context,
            CancellationToken ct = default)
        {
            // Retrieve stock info from context (set by Mutator)
            if (!context.WideEventContext.TryGetValue(ContextKeys.ProductName, out var nameObj) || nameObj is not string productName)
            {
                return Error.Unexpected("SideEffects.MissingContext", "Product name not found in context");
            }

            if (!context.WideEventContext.TryGetValue(ContextKeys.ReservedQuantity, out var qtyObj) || qtyObj is not int reservedQty)
            {
                return Error.Unexpected("SideEffects.MissingContext", "Reserved quantity not found in context");
            }

            if (!context.WideEventContext.TryGetValue(ContextKeys.RemainingStock, out var stockObj) || stockObj is not int remainingStock)
            {
                return Error.Unexpected("SideEffects.MissingContext", "Remaining stock not found in context");
            }

            // Always publish StockReserved event
            var reservedEvent = new StockReserved(
                ProductId: context.Request.ProductId,
                ProductName: productName,
                QuantityReserved: reservedQty,
                RemainingStock: remainingStock);

            var result = await publishEndpoint.PublishAsync(reservedEvent, ct);
            if (result.IsError)
            {
                return result;
            }

            // Conditionally publish StockLow alert when below threshold
            if (remainingStock < LowStockThreshold)
            {
                var lowStockEvent = new StockLow(
                    ProductId: context.Request.ProductId,
                    ProductName: productName,
                    CurrentStock: remainingStock);

                return await publishEndpoint.PublishAsync(lowStockEvent, ct);
            }

            return Unit.Value;
        }
    }
}
