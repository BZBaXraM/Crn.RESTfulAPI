using Domain.Entities;

namespace Application.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default);
    Task<bool> UserNameExistsAsync(string userName, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);
}
