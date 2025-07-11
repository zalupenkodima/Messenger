using Microsoft.EntityFrameworkCore;
using Messenger.Domain.Entities;
using Messenger.Domain.Interfaces;
using Messenger.Infrastructure.Data;

namespace Messenger.Infrastructure.Repositories;

public class UserRepository(MessengerDbContext context) : BaseRepository<User>(context), IUserRepository
{
    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<IEnumerable<User>> GetOnlineUsersAsync()
    {
        return await _dbSet.Where(u => u.IsOnline).ToListAsync();
    }

    public async Task UpdateLastSeenAsync(Guid userId, DateTime lastSeen)
    {
        var user = await _dbSet.FindAsync(userId);
        if (user != null)
        {
            user.LastSeen = lastSeen;
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateOnlineStatusAsync(Guid userId, bool isOnline)
    {
        var user = await _dbSet.FindAsync(userId);
        if (user != null)
        {
            user.IsOnline = isOnline;
            user.LastSeen = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<User>> SearchUsersAsync(string query)
    {
        Console.WriteLine($"UserRepository.SearchUsersAsync: Searching for '{query}'");
        
        var users = await _dbSet
            .Where(u => u.Username.Contains(query) || u.Email.Contains(query))
            .ToListAsync();
            
        Console.WriteLine($"UserRepository.SearchUsersAsync: Found {users.Count()} users");
        foreach (var user in users)
        {
            Console.WriteLine($"UserRepository.SearchUsersAsync: User - {user.Username} ({user.Email})");
        }
        
        return users;
    }
} 