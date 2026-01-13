using VsaResults.Sample.WebApi.Models;
using VsaResults.Sample.WebApi.Services;

namespace VsaResults.Sample.WebApi.Endpoints;

/// <summary>
/// Demonstrates medium complexity ErrorOr patterns with Minimal APIs.
/// Includes validation and CRUD operations.
/// </summary>
public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/minimal/users")
            .WithTags("Users (Minimal API)");

        group.MapGet("/", GetAll);
        group.MapGet("/{id:guid}", GetById);
        group.MapPost("/", Create);
        group.MapPut("/{id:guid}", Update);
        group.MapDelete("/{id:guid}", Delete);
    }

    private static IResult GetAll(IUserService userService) =>
        userService.GetAll().Match(Results.Ok, errors => errors.ToResults());

    private static IResult GetById(Guid id, IUserService userService) =>
        userService.GetById(id).Match(Results.Ok, errors => errors.ToResults());

    private static IResult Create(CreateUserRequest request, IUserService userService) =>
        userService.Create(request).Match(
            user => Results.Created($"/api/minimal/users/{user.Id}", user),
            errors => errors.ToResults());

    private static IResult Update(Guid id, UpdateUserRequest request, IUserService userService) =>
        userService.Update(id, request).Match(Results.Ok, errors => errors.ToResults());

    private static IResult Delete(Guid id, IUserService userService) =>
        userService.Delete(id).Match(_ => Results.NoContent(), errors => errors.ToResults());
}
