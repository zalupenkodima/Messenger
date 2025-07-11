namespace Messenger.WinForms.Core.Interfaces;

public interface ILocalAuthStorageService
{
    void SaveToken(string token);
    string? LoadToken();
    void ClearToken();
} 