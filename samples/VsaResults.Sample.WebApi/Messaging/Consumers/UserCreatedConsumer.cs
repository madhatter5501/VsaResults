using VsaResults.Messaging;
using VsaResults.Sample.WebApi.Messaging.Events;

namespace VsaResults.Sample.WebApi.Messaging.Consumers;

/// <summary>
/// Consumes UserCreated events.
/// Demonstrates the event subscription pattern - reacting to domain events.
/// In a real application, this might sync to a search index, analytics, or CRM.
/// </summary>
public class UserCreatedConsumer(ILogger<UserCreatedConsumer> logger) : IConsumer<UserCreated>
{
    public Task<ErrorOr<Unit>> ConsumeAsync(
        ConsumeContext<UserCreated> context,
        CancellationToken ct = default)
    {
        var message = context.Message;

        logger.LogInformation(
            "[UserCreatedConsumer] User created notification received: {UserId} - {Email} (Role: {Role})",
            message.UserId,
            message.Email,
            message.Role);

        // Add telemetry context for wide events
        context.AddContext("processed_user_id", message.UserId.ToString());
        context.AddContext("processed_user_role", message.Role.ToString());

        // Real implementation might:
        // - Update search index (Elasticsearch, Algolia)
        // - Send to analytics (Segment, Mixpanel)
        // - Sync to external CRM (Salesforce, HubSpot)
        // - Trigger follow-up workflows
        return Task.FromResult<ErrorOr<Unit>>(Unit.Value);
    }
}
