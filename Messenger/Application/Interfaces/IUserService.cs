using Messenger.Shared;

namespace Messenger.Application.Interfaces;

public interface IUserService
{
    Task<UserDto> CreateUserAsync(CreateUserDto createUserDto);
    Task<UserDto> UpdateUserAsync(Guid userId, UpdateUserDto updateUserDto);
    Task<UserDto?> GetUserAsync(Guid userId);
    Task<UserDto?> GetUserByUsernameAsync(string username);
    Task<IEnumerable<UserDto>> GetOnlineUsersAsync();
    Task UpdateOnlineStatusAsync(Guid userId, bool isOnline);
    Task<string> AuthenticateUserAsync(string username, string password);
    Task<IEnumerable<UserDto>> SearchUsersAsync(string query);
} 