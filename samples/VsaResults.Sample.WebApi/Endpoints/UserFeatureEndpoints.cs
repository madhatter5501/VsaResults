using VsaResults.Sample.WebApi.Features.Users;
using VsaResults.Sample.WebApi.Models;

namespace VsaResults.Sample.WebApi.Endpoints;

/// <summary>
/// User endpoints using FeatureHandler for clean Minimal API integration.
/// Demonstrates how FeatureHandler wires up DI-resolved features automatically.
/// </summary>
public static class UserFeatureEndpoints
{
    public static void MapUserFeatureEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/features/users")
            .WithTags("Users (Feature-based)");

        // Simple query - no validation, just returns all users
        group.MapGet("/", FeatureHandler.QueryOk<GetAllUsers.Request, GetAllUsers.Response>());

        // Query with validation - validates the ID before fetching
        group.MapGet("/{id:guid}", FeatureHandler.Query<GetUserById.Request, User>(ApiResults.Ok));

        // Mutation with Created response - uses location selector for the Location header
        group.MapPost("/", FeatureHandler.MutationCreated<CreateUser.Request, User>(user => $"/api/features/users/{user.Id}"));

        // Mutation with NoContent response - for delete operations
        group.MapDelete("/{id:guid}", FeatureHandler.MutationNoContent<DeleteUser.Request>());
    }
}
