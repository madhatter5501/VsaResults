using VsaResults.Messaging;
using VsaResults.Sample.WebApi.Messaging.Events;

namespace VsaResults.Sample.WebApi.Features.Users;

public static partial class DeleteUser
{
    /// <summary>
    /// Side effects for DeleteUser: publishes UserDeleted event.
    /// Subscribers can clean up related data, invalidate caches, etc.
    /// </summary>
    public class SideEffects(IPublishEndpoint publishEndpoint) : IFeatureSideEffects<Request>
    {
        public async Task<ErrorOr<Unit>> ExecuteAsync(
            FeatureContext<Request> context,
            CancellationToken ct = default)
        {
            // Retrieve deleted user info from context (set by Requirements and Mutator)
            if (!context.WideEventContext.TryGetValue(ContextKeys.DeletedUserId, out var userIdObj) || userIdObj is not Guid deletedUserId)
            {
                return Error.Unexpected("SideEffects.MissingContext", "Deleted user ID not found in context");
            }

            if (!context.WideEventContext.TryGetValue(ContextKeys.UserEmail, out var emailObj) || emailObj is not string userEmail)
            {
                return Error.Unexpected("SideEffects.MissingContext", "User email not found in context");
            }

            var @event = new UserDeleted(
                UserId: deletedUserId,
                Email: userEmail,
                DeletedAt: DateTime.UtcNow);

            return await publishEndpoint.PublishAsync(@event, ct);
        }
    }
}
