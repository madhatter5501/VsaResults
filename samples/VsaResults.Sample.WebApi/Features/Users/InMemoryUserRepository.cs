using VsaResults.Sample.WebApi.Models;

namespace VsaResults.Sample.WebApi.Features.Users;

/// <summary>
/// In-memory implementation of IUserRepository for demo purposes.
/// </summary>
public class InMemoryUserRepository : IUserRepository
{
    private readonly Dictionary<Guid, User> _users = new()
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

    public List<User> GetAll() =>
        _users.Values.ToList();

    public ErrorOr<User> GetById(Guid id) =>
        _users.TryGetValue(id, out var user)
            ? user
            : DomainErrors.User.NotFound(id);

    public bool ExistsByEmail(string email) =>
        _users.Values.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

    public void Add(User user) =>
        _users[user.Id] = user;

    public void Delete(Guid id) =>
        _users.Remove(id);
}
