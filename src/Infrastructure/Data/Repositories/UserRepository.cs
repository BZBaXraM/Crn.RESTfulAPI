using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories;

public sealed class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<User?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default)
        => Set.FirstOrDefaultAsync(u => u.UserName == userName, cancellationToken);

    public Task<bool> UserNameExistsAsync(string userName, CancellationToken cancellationToken = default)
        => Set.AsNoTracking().AnyAsync(u => u.UserName == userName, cancellationToken);

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
        => Set.AsNoTracking().AnyAsync(u => u.Email == email, cancellationToken);

    public Task<bool> AnyAsync(CancellationToken cancellationToken = default)
        => Set.AsNoTracking().AnyAsync(cancellationToken);
}
