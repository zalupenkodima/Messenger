using Messenger.Domain.Entities;

namespace Messenger.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetOnlineUsersAsync();
    Task UpdateLastSeenAsync(Guid userId, DateTime lastSeen);
    Task UpdateOnlineStatusAsync(Guid userId, bool isOnline);
    Task<IEnumerable<User>> SearchUsersAsync(string query);
} 