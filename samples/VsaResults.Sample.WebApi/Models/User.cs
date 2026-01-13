namespace VsaResults.Sample.WebApi.Models;

public record User(
    Guid Id,
    string Email,
    string Name,
    UserRole Role,
    DateTime CreatedAt);

public enum UserRole
{
    User,
    Admin,
    SuperAdmin,
}

public record CreateUserRequest(string Email, string Name, UserRole Role = UserRole.User);

public record UpdateUserRequest(string? Name, UserRole? Role);
