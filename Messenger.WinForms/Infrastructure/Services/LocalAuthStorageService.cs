using Messenger.WinForms.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Messenger.WinForms.Infrastructure.Services;

public class LocalAuthStorageService(ILogger<LocalAuthStorageService> logger) : ILocalAuthStorageService
{
    private const string TokenFile = "user.token";
    private readonly ILogger<LocalAuthStorageService> _logger = logger;

    public void SaveToken(string token)
    {
        try
        {
            File.WriteAllText(TokenFile, token);
            _logger.LogInformation("Token saved to file: {TokenFile}", TokenFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save token to file: {TokenFile}", TokenFile);
        }
    }

    public string? LoadToken()
    {
        try
        {
            if (File.Exists(TokenFile))
            {
                var token = File.ReadAllText(TokenFile);
                _logger.LogInformation("Token loaded from file: {TokenFile}", TokenFile);
                return token;
            }
            else
            {
                _logger.LogInformation("Token file not found: {TokenFile}", TokenFile);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load token from file: {TokenFile}", TokenFile);
            return null;
        }
    }

    public void ClearToken()
    {
        try
        {
            if (File.Exists(TokenFile))
            {
                File.Delete(TokenFile);
                _logger.LogInformation("Token file deleted: {TokenFile}", TokenFile);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete token file: {TokenFile}", TokenFile);
        }
    }
} 