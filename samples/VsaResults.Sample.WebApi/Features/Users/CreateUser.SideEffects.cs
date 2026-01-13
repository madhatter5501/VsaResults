using VsaResults.Messaging;
using VsaResults.Sample.WebApi.Messaging.Events;
using VsaResults.Sample.WebApi.Models;

namespace VsaResults.Sample.WebApi.Features.Users;

public static partial class CreateUser
{
    /// <summary>
    /// Side effects for CreateUser: publishes UserCreated event.
    /// Demonstrates the NOTIFICATION pattern - fire-and-forget event publishing
    /// after a successful mutation.
    ///
    /// Side effects run AFTER the mutator succeeds, making them ideal for:
    /// - Publishing domain events
    /// - Sending notifications
    /// - Updating caches
    /// - Triggering async workflows
    /// </summary>
    public class SideEffects(IPublishEndpoint publishEndpoint) : IFeatureSideEffects<Request>
    {
        public async Task<ErrorOr<Unit>> ExecuteAsync(
            FeatureContext<Request> context,
            CancellationToken ct = default)
        {
            // Retrieve the created user's ID from context (set by Mutator)
            if (!context.WideEventContext.TryGetValue(ContextKeys.UserId, out var userIdObj) || userIdObj is not Guid userId)
            {
                return Error.Unexpected("SideEffects.MissingContext", "User ID not found in context");
            }

            var request = context.Request;

            // Publish notification event - subscribers can react asynchronously
            // This is fire-and-forget from the feature's perspective
            var @event = new UserCreated(
                UserId: userId,
                Email: request.Email,
                Name: request.Name,
                Role: request.Role,
                CreatedAt: DateTime.UtcNow);

            return await publishEndpoint.PublishAsync(@event, ct);
        }
    }
}
