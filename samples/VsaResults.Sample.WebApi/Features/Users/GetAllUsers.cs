using VsaResults.Sample.WebApi.Models;

namespace VsaResults.Sample.WebApi.Features.Users;

/// <summary>
/// Simple query feature: Returns all users.
/// Demonstrates basic IQueryFeature with no validation.
/// </summary>
public static class GetAllUsers
{
    public record Request;

    public record Response(List<User> Users);

    public class Feature(IFeatureQuery<Request, Response> query) : IQueryFeature<Request, Response>
    {
        public IFeatureQuery<Request, Response> Query => query;
    }

    public class Query(IUserRepository repository) : IFeatureQuery<Request, Response>
    {
        public Task<ErrorOr<Response>> ExecuteAsync(Request request, CancellationToken ct = default) =>
            Task.FromResult<ErrorOr<Response>>(new Response(repository.GetAll()));
    }
}
