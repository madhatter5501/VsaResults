using VsaResults.Sample.WebApi.Features.Users;
using VsaResults.Sample.WebApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace VsaResults.Sample.WebApi.Controllers;

/// <summary>
/// Controller using FeatureController base class for clean feature execution.
/// Demonstrates QueryOk, MutationCreated, and MutationNoContent patterns.
/// </summary>
[ApiController]
[Route("api/controller-features/users")]
public class UserFeaturesController : FeatureController
{
    [HttpGet]
    public Task<ActionResult<GetAllUsers.Response>> GetAll(CancellationToken ct) =>
        QueryOk<GetAllUsers.Request, GetAllUsers.Response>(new GetAllUsers.Request(), ct);

    [HttpGet("{id:guid}")]
    public Task<ActionResult<User>> GetById(Guid id, CancellationToken ct) =>
        QueryOk<GetUserById.Request, User>(new GetUserById.Request(id), ct);

    [HttpPost]
    public Task<ActionResult<User>> Create(CreateUser.Request request, CancellationToken ct) =>
        MutationCreated<CreateUser.Request, User>(request, user => $"/api/controller-features/users/{user.Id}", ct);

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        MutationNoContent<DeleteUser.Request>(new DeleteUser.Request(id), ct);
}
