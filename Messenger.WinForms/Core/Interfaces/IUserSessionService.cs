using Messenger.Shared;

namespace Messenger.WinForms.Core.Interfaces;

public interface IUserSessionService
{
    UserDto? CurrentUser { get; }
    string? CurrentToken { get; }
    bool IsAuthenticated { get; }
    
    void SetUserSession(UserDto user, string token);
    void ClearSession();
    Task<bool> ValidateSessionAsync();
} 