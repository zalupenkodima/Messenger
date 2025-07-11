using Messenger.Shared;
using Messenger.WinForms.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Messenger.WinForms.Infrastructure.Services;

public class UserSessionService(IMessengerService messengerService, ILogger<UserSessionService> logger) : IUserSessionService
{
    private readonly IMessengerService _messengerService = messengerService;
    private readonly ILogger<UserSessionService> _logger = logger;
    
    private UserDto? _currentUser;
    private string? _currentToken;

    public UserDto? CurrentUser => _currentUser;
    public string? CurrentToken => _currentToken;
    public bool IsAuthenticated => _currentUser != null && !string.IsNullOrEmpty(_currentToken);

    public void SetUserSession(UserDto user, string token)
    {
        _currentUser = user;
        _currentToken = token;
        _messengerService.SetToken(token);
        _logger.LogInformation("User session set for: {Username} with ID: {UserId}", user.Username, user.Id);
        _logger.LogInformation("User session - ID type: {UserIdType}", user.Id.GetType().Name);
    }

    public void ClearSession()
    {
        _currentUser = null;
        _currentToken = null;
        _logger.LogInformation("User session cleared");
    }

    public async Task<bool> ValidateSessionAsync()
    {
        if (!IsAuthenticated)
        {
            return false;
        }

        try
        {
            if (_currentUser != null)
            {
                var user = await _messengerService.GetUserAsync(_currentUser.Id);
                if (user != null)
                {
                    _currentUser = user;
                    return true;
                }
            }
            
            _logger.LogWarning("Session validation failed - user not found");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Session validation failed");
            return false;
        }
    }
} 