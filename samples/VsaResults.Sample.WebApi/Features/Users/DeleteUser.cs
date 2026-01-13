using VsaResults.Sample.WebApi.Models;

namespace VsaResults.Sample.WebApi.Features.Users;

/// <summary>
/// Complex mutation: Deletes a user with requirements enforcement.
/// Demonstrates IMutationFeature with Requirements stage for authorization/entity loading.
/// </summary>
public static partial class DeleteUser
{
    private static class ErrorCodes
    {
        public const string InvalidId = "User.InvalidId";
    }

    private static class ErrorMessages
    {
        public const string IdCannotBeEmpty = "User ID cannot be empty.";
    }

    private static class ContextKeys
    {
        public const string UserEmail = "user_email";
        public const string DeletedUserId = "deleted_user_id";
    }

    public record Request(Guid Id);

    public class Feature(
        IFeatureValidator<Request> validator,
        IFeatureRequirements<Request> requirements,
        IFeatureMutator<Request, Unit> mutator,
        IFeatureSideEffects<Request>? sideEffects = null)
        : IMutationFeature<Request, Unit>
    {
        public IFeatureValidator<Request> Validator => validator;

        public IFeatureRequirements<Request> Requirements => requirements;

        public IFeatureMutator<Request, Unit> Mutator => mutator;

        public IFeatureSideEffects<Request> SideEffects => sideEffects ?? NoOpSideEffects<Request>.Instance;
    }

    public class Validator : IFeatureValidator<Request>
    {
        public Task<ErrorOr<Request>> ValidateAsync(Request request, CancellationToken ct = default) =>
            request.Id == Guid.Empty
                ? Task.FromResult<ErrorOr<Request>>(Error.Validation(ErrorCodes.InvalidId, ErrorMessages.IdCannotBeEmpty))
                : Task.FromResult<ErrorOr<Request>>(request);
    }

    /// <summary>
    /// Requirements stage: Loads the user and enforces business rules.
    /// This is where you'd typically check authorization and load entities.
    /// </summary>
    public class Requirements(IUserRepository repository) : IFeatureRequirements<Request>
    {
        private const string UserKey = "user";

        public Task<ErrorOr<FeatureContext<Request>>> EnforceAsync(Request request, CancellationToken ct = default)
        {
            var userResult = repository.GetById(request.Id);

            if (userResult.IsError)
            {
                return Task.FromResult<ErrorOr<FeatureContext<Request>>>(userResult.Errors);
            }

            var user = userResult.Value;

            // Business rule: Admin users cannot be deleted
            if (user.Role == UserRole.Admin)
            {
                return Task.FromResult<ErrorOr<FeatureContext<Request>>>(DomainErrors.User.CannotDeleteAdmin);
            }

            // Store the loaded entity in context for use in mutator
            var context = new FeatureContext<Request> { Request = request };
            context.SetEntity(UserKey, user);
            context.AddContext(ContextKeys.UserEmail, user.Email);

            return Task.FromResult<ErrorOr<FeatureContext<Request>>>(context);
        }

        public static User GetUser(FeatureContext<Request> context) =>
            context.GetEntity<User>(UserKey);
    }

    public class Mutator(IUserRepository repository) : IFeatureMutator<Request, Unit>
    {
        public Task<ErrorOr<Unit>> ExecuteAsync(FeatureContext<Request> context, CancellationToken ct = default)
        {
            // User was already loaded and validated in Requirements stage
            var user = Requirements.GetUser(context);

            repository.Delete(user.Id);

            context.AddContext(ContextKeys.DeletedUserId, user.Id);

            return Task.FromResult<ErrorOr<Unit>>(Unit.Value);
        }
    }
}
