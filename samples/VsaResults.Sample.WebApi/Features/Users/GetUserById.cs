using VsaResults.Sample.WebApi.Models;

namespace VsaResults.Sample.WebApi.Features.Users;

/// <summary>
/// Query feature with validation: Returns a user by ID.
/// Demonstrates IQueryFeature with input validation.
/// </summary>
public static class GetUserById
{
    private static class ErrorCodes
    {
        public const string InvalidId = "User.InvalidId";
    }

    private static class ErrorMessages
    {
        public const string IdCannotBeEmpty = "User ID cannot be empty.";
    }

    public record Request(Guid Id);

    public class Feature(
        IFeatureValidator<Request> validator,
        IFeatureQuery<Request, User> query)
        : IQueryFeature<Request, User>
    {
        public IFeatureValidator<Request> Validator => validator;

        public IFeatureQuery<Request, User> Query => query;
    }

    public class Validator : IFeatureValidator<Request>
    {
        public Task<ErrorOr<Request>> ValidateAsync(Request request, CancellationToken ct = default) =>
            request.Id == Guid.Empty
                ? Task.FromResult<ErrorOr<Request>>(Error.Validation(ErrorCodes.InvalidId, ErrorMessages.IdCannotBeEmpty))
                : Task.FromResult<ErrorOr<Request>>(request);
    }

    public class Query(IUserRepository repository) : IFeatureQuery<Request, User>
    {
        public Task<ErrorOr<User>> ExecuteAsync(Request request, CancellationToken ct = default) =>
            Task.FromResult(repository.GetById(request.Id));
    }
}
