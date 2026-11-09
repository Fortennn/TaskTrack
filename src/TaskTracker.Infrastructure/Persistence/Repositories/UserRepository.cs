using Microsoft.EntityFrameworkCore;
using TaskTracker.Application.Common.Interfaces;
using TaskTracker.Domain.Entities;
using TaskTracker.Infrastructure.Persistence;

namespace TaskTracker.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users.AnyAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetFirstAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users.AsNoTracking().ToListAsync(cancellationToken);
    }
}
