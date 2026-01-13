using System.Text.RegularExpressions;
using VsaResults.Messaging;
using VsaResults.Sample.WebApi.Messaging.Commands;
using VsaResults.Sample.WebApi.Models;

namespace VsaResults.Sample.WebApi.Features.Users;

/// <summary>
/// Medium complexity mutation: Creates a new user.
/// Demonstrates IMutationFeature with validation and business logic.
/// </summary>
public static partial class CreateUser
{
    private static class ContextKeys
    {
        public const string UserId = "user_id";
        public const string UserRole = "user_role";
    }

    public record Request(string Email, string Name, UserRole Role = UserRole.User);

    public class Feature(
        IFeatureValidator<Request> validator,
        IFeatureMutator<Request, User> mutator,
        IFeatureSideEffects<Request>? sideEffects = null)
        : IMutationFeature<Request, User>
    {
        public IFeatureValidator<Request> Validator => validator;

        public IFeatureMutator<Request, User> Mutator => mutator;

        public IFeatureSideEffects<Request> SideEffects => sideEffects ?? NoOpSideEffects<Request>.Instance;
    }

    public partial class Validator : IFeatureValidator<Request>
    {
        public Task<ErrorOr<Request>> ValidateAsync(Request request, CancellationToken ct = default)
        {
            var errors = new List<Error>();

            if (string.IsNullOrWhiteSpace(request.Email) || !EmailRegex().IsMatch(request.Email))
            {
                errors.Add(DomainErrors.User.InvalidEmail(request.Email ?? string.Empty));
            }

            if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length < 2)
            {
                errors.Add(DomainErrors.User.NameTooShort);
            }
            else if (request.Name.Length > 100)
            {
                errors.Add(DomainErrors.User.NameTooLong);
            }

            return errors.Count > 0
                ? Task.FromResult<ErrorOr<Request>>(errors)
                : Task.FromResult<ErrorOr<Request>>(request);
        }

        [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
        private static partial Regex EmailRegex();
    }

    /// <summary>
    /// Mutator for CreateUser.
    /// Demonstrates the ORCHESTRATION pattern - sending a command as part of
    /// the mutation to coordinate with an external service (email notification).
    ///
    /// Note: The orchestration pattern is optional. If ISendEndpointProvider is not
    /// registered or the send fails, user creation still succeeds (graceful degradation).
    /// For critical orchestration where failure should abort the mutation, check the
    /// send result and return an error.
    /// </summary>
    public class Mutator(
        IUserRepository repository,
        ISendEndpointProvider? sendEndpointProvider = null,
        ILogger<Mutator>? logger = null) : IFeatureMutator<Request, User>
    {
        public async Task<ErrorOr<User>> ExecuteAsync(FeatureContext<Request> context, CancellationToken ct = default)
        {
            var request = context.Request;

            // Check for duplicate email
            if (repository.ExistsByEmail(request.Email))
            {
                return DomainErrors.User.DuplicateEmail(request.Email);
            }

            var user = new User(
                Guid.NewGuid(),
                request.Email,
                request.Name,
                request.Role,
                DateTime.UtcNow);

            repository.Add(user);

            // Add context for wide event logging
            context.AddContext(ContextKeys.UserId, user.Id);
            context.AddContext(ContextKeys.UserRole, user.Role.ToString());

            // ORCHESTRATION PATTERN: Send command to notification service
            // This demonstrates sending a command as part of mutation logic.
            // The command is processed asynchronously by SendWelcomeEmailConsumer.
            if (sendEndpointProvider is not null)
            {
                var command = new SendWelcomeEmail(user.Id, user.Email, user.Name);

                // Get the send endpoint for the notification queue
                var endpointResult = await sendEndpointProvider.GetSendEndpointAsync(
                    EndpointAddress.InMemory("send-welcome-email-consumer"),
                    ct);

                if (endpointResult.IsError)
                {
                    // Log but don't fail - email is not critical for user creation
                    logger?.LogWarning(
                        "Failed to get send endpoint for welcome email: {Errors}",
                        string.Join(", ", endpointResult.Errors.Select(e => e.Description)));
                }
                else
                {
                    var sendResult = await endpointResult.Value.SendAsync(command, ct);
                    if (sendResult.IsError)
                    {
                        logger?.LogWarning(
                            "Failed to send welcome email command: {Errors}",
                            string.Join(", ", sendResult.Errors.Select(e => e.Description)));
                    }
                    else
                    {
                        logger?.LogInformation(
                            "Welcome email command sent for user {UserId}",
                            user.Id);
                        context.AddContext("welcome_email_sent", true);
                    }
                }
            }

            return user;
        }
    }
}
