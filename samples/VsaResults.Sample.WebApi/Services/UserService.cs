using System.Text.RegularExpressions;

using VsaResults.Sample.WebApi.Models;

namespace VsaResults.Sample.WebApi.Services;

public interface IUserService
{
    ErrorOr<User> GetById(Guid id);
    ErrorOr<List<User>> GetAll();
    ErrorOr<User> Create(CreateUserRequest request);
    ErrorOr<User> Update(Guid id, UpdateUserRequest request);
    ErrorOr<Deleted> Delete(Guid id);
}

/// <summary>
/// Demonstrates simple ErrorOr patterns for CRUD operations.
/// </summary>
public partial class UserService : IUserService
{
    private static readonly Dictionary<Guid, User> _users = new()
    {
        [Guid.Parse("11111111-1111-1111-1111-111111111111")] = new User(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "admin@example.com",
            "Admin User",
            UserRole.Admin,
            DateTime.UtcNow.AddDays(-30)),
        [Guid.Parse("22222222-2222-2222-2222-222222222222")] = new User(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "user@example.com",
            "Regular User",
            UserRole.User,
            DateTime.UtcNow.AddDays(-10)),
    };

    /// <summary>
    /// Simple example: Return value or NotFound error.
    /// </summary>
    public ErrorOr<User> GetById(Guid id) =>
        _users.TryGetValue(id, out var user)
            ? user
            : DomainErrors.User.NotFound(id);

    /// <summary>
    /// Simple example: Always returns success with a list.
    /// </summary>
    public ErrorOr<List<User>> GetAll() =>
        _users.Values.ToList();

    /// <summary>
    /// Medium complexity: Validates input and checks for conflicts.
    /// Demonstrates collecting multiple validation errors.
    /// </summary>
    public ErrorOr<User> Create(CreateUserRequest request)
    {
        // Collect all validation errors instead of failing fast
        var errors = new List<Error>();

        if (!EmailRegex().IsMatch(request.Email))
        {
            errors.Add(DomainErrors.User.InvalidEmail(request.Email));
        }

        if (request.Name.Length < 2)
        {
            errors.Add(DomainErrors.User.NameTooShort);
        }

        if (request.Name.Length > 100)
        {
            errors.Add(DomainErrors.User.NameTooLong);
        }

        // Return all validation errors at once
        if (errors.Count > 0)
        {
            return errors;
        }

        // Check for duplicate email (business rule)
        if (_users.Values.Any(u => u.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase)))
        {
            return DomainErrors.User.DuplicateEmail(request.Email);
        }

        var user = new User(
            Guid.NewGuid(),
            request.Email,
            request.Name,
            request.Role,
            DateTime.UtcNow);

        _users[user.Id] = user;

        return user;
    }

    /// <summary>
    /// Medium complexity: Chain operations with Then.
    /// </summary>
    public ErrorOr<User> Update(Guid id, UpdateUserRequest request) =>
        GetById(id)
            .Then(existingUser =>
            {
                var updatedUser = existingUser with
                {
                    Name = request.Name ?? existingUser.Name,
                    Role = request.Role ?? existingUser.Role,
                };

                _users[id] = updatedUser;
                return updatedUser;
            });

    /// <summary>
    /// Medium complexity: Business rule validation before deletion.
    /// </summary>
    public ErrorOr<Deleted> Delete(Guid id) =>
        GetById(id)
            .Then<Deleted>(user =>
            {
                if (user.Role == UserRole.Admin)
                {
                    return DomainErrors.User.CannotDeleteAdmin;
                }

                _users.Remove(id);
                return Result.Deleted;
            });

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
}
