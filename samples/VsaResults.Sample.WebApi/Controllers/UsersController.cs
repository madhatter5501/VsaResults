using VsaResults.Sample.WebApi.Models;
using VsaResults.Sample.WebApi.Services;

using Microsoft.AspNetCore.Mvc;

namespace VsaResults.Sample.WebApi.Controllers;

/// <summary>
/// Demonstrates simple to medium complexity ErrorOr usage in MVC Controllers.
/// Uses the built-in ToOkResult() and ToCreatedResult() extension methods.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UsersController(IUserService userService) : ControllerBase
{
    /// <summary>
    /// Simple example: Get all users.
    /// ToOkResult() automatically returns 200 OK with the value.
    /// </summary>
    [HttpGet]
    public ActionResult<List<User>> GetAll() =>
        userService.GetAll().ToOkResult();

    /// <summary>
    /// Simple example: Get user by ID.
    /// ToOkResult() returns 200 OK for success, or ProblemDetails for errors.
    /// A NotFound error automatically maps to 404.
    /// </summary>
    [HttpGet("{id:guid}")]
    public ActionResult<User> GetById(Guid id) =>
        userService.GetById(id).ToOkResult();

    /// <summary>
    /// Medium example: Create user with validation.
    /// ToCreatedResult() returns 201 Created with location header.
    /// Validation errors automatically map to 400 with ValidationProblemDetails.
    /// Conflict errors (duplicate email) map to 409.
    /// </summary>
    [HttpPost]
    public ActionResult<User> Create([FromBody] CreateUserRequest request) =>
        userService.Create(request).ToCreatedResult(user => $"/api/users/{user.Id}");

    /// <summary>
    /// Medium example: Update user.
    /// Demonstrates chained operations in the service.
    /// </summary>
    [HttpPut("{id:guid}")]
    public ActionResult<User> Update(Guid id, [FromBody] UpdateUserRequest request) =>
        userService.Update(id, request).ToOkResult();

    /// <summary>
    /// Medium example: Delete user with business rule validation.
    /// Uses Match since Delete returns Deleted, not Success.
    /// Forbidden errors (can't delete admin) map to 403.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id) =>
        userService.Delete(id).Match<IActionResult>(
            _ => NoContent(),
            errors => errors.ToProblemDetailsResult());

    /// <summary>
    /// Alternative pattern: Manual Match for custom response handling.
    /// Use this when you need more control over the response format.
    /// </summary>
    [HttpGet("{id:guid}/profile")]
    public IActionResult GetProfile(Guid id) =>
        userService.GetById(id).Match<IActionResult>(
            user => Ok(new
            {
                user.Id,
                user.Name,
                user.Email,
                MemberSince = user.CreatedAt.ToString("MMMM yyyy"),
                IsAdmin = user.Role >= UserRole.Admin,
            }),
            errors => errors.ToProblemDetailsResult());
}
