using VsaResults.Sample.WebApi.Models;

namespace VsaResults.Sample.WebApi.Features.Users;

/// <summary>
/// Repository interface for User entity.
/// Returns ErrorOr from methods that can fail (like GetById).
/// </summary>
public interface IUserRepository
{
    List<User> GetAll();

    ErrorOr<User> GetById(Guid id);

    bool ExistsByEmail(string email);

    void Add(User user);

    void Delete(Guid id);
}
