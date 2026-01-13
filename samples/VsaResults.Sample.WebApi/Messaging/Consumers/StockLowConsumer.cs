using VsaResults.Messaging;
using VsaResults.Sample.WebApi.Messaging.Events;

namespace VsaResults.Sample.WebApi.Messaging.Consumers;

/// <summary>
/// Consumes StockLow events to trigger alerts or auto-reordering.
/// Demonstrates reactive event handling for operational concerns.
/// </summary>
public class StockLowConsumer(ILogger<StockLowConsumer> logger) : IConsumer<StockLow>
{
    public Task<ErrorOr<Unit>> ConsumeAsync(
        ConsumeContext<StockLow> context,
        CancellationToken ct = default)
    {
        var message = context.Message;

        logger.LogWarning(
            "[StockLowConsumer] LOW STOCK ALERT: Product {ProductId} ({ProductName}) has only {Stock} units remaining!",
            message.ProductId,
            message.ProductName,
            message.CurrentStock);

        // In a real implementation, this might:
        // - Send Slack/Teams alert to warehouse team
        // - Trigger auto-reorder workflow
        // - Update inventory management dashboard
        // - Send email to procurement

        // Could also publish follow-up events:
        // await context.PublishAsync(new ReorderRequired(
        //     message.ProductId,
        //     message.ProductName,
        //     CalculateReorderQuantity(message.CurrentStock)));
        context.AddContext("alert_type", "low_stock");
        context.AddContext("product_id", message.ProductId.ToString());
        context.AddContext("current_stock", message.CurrentStock.ToString());

        return Task.FromResult<ErrorOr<Unit>>(Unit.Value);
    }
}
