namespace Messenger.WinForms.Core.Interfaces;

public interface INavigationService
{
    void ShowLoginForm();
    void ShowMainForm();
    void ShowChatForm(Guid chatId);
    void ShowUserSearchForm();
    void ShowCreateChatForm();
    void CloseApplication();
} 