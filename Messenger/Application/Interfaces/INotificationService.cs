namespace Messenger.Application.Interfaces;

public interface INotificationService
{
    Task SendUnreadMessageNotificationAsync(string userEmail, string username, string senderName, string chatName, string messageContent, DateTime messageTime);
    Task<bool> IsNotificationSentAsync(Guid messageId, Guid userId);
    Task MarkNotificationAsSentAsync(Guid messageId, Guid userId);
} 